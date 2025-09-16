using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Cosmos.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using Trident.Contracts.Configuration;
using Trident.Data;
using Trident.EFCore.Changes;
using Trident.EFCore.Contracts;
using Trident.Extensions;
using Trident.IoC;
using Trident.Logging;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

#pragma warning disable EF1001

namespace Trident.EFCore
{
    public class EFCoreCosmosDataContext : EFCoreDataContext
    {
        private const string LoggerCategory = "Trident.EFCore.Diagnostics";

        private readonly IAppSettings _appSettings;

        private readonly Lazy<ILogger> _lazyLogger;

        public EFCoreCosmosDataContext(IEFCoreModelBuilderFactory modelBuilderFactory,
                                       IEntityMapFactory mapFactory,
                                       string dataSource,
                                       DbContextOptions options,
                                       ILog log,
                                       ILoggerFactory loggerFactory,
                                       IAppSettings appSettings,
                                       IIoCServiceLocator ioCServiceLocator,
                                       IChangeObserver changeObserver)
            : base(modelBuilderFactory, mapFactory, dataSource, options, log, loggerFactory, appSettings, ioCServiceLocator, changeObserver)
        {
            _appSettings = appSettings;
            _lazyLogger = new Lazy<ILogger>(() => loggerFactory.CreateLogger(LoggerCategory));
        }

        private ILogger Logger => _lazyLogger.Value;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            if (bool.Parse(_appSettings.GetKeyOrDefault("EFPerfLoggingEnabled", "false")))
            {
                optionsBuilder.LogTo((eventId, logLevel) => eventId == CosmosEventId.ExecutedReadItem ||
                                                            eventId == CosmosEventId.ExecutedReadNext,
                                     eventData =>
                                     {
                                         if (eventData is CosmosQueryExecutedEventData ed)
                                         {
                                             var message =
                                                 $@"Executed Query: T={ed.Elapsed.TotalMilliseconds:F3}ms, RU={ed.RequestCharge:F3}, C={ed.ContainerId}, PK={ed.PartitionKey}, Q=""{ed.QuerySql.Replace(Environment.NewLine, " ")}""";

                                             Logger.Log(LogLevel.Information,
                                                        ed.EventId,
                                                        new
                                                        {
                                                            ElapsedTime = ed.Elapsed,
                                                            RequestUnits = ed.RequestCharge,
                                                            Container = ed.ContainerId,
                                                            Partition = ed.PartitionKey,
                                                            Query = ed.QuerySql,
                                                            Message = message,
                                                        },
                                                        null,
                                                        (s, x) => s.ToJson());
                                         }
                                     });
            }
        }

        public override async Task<IEnumerable<TEntity>> ExecuteQueryAsync<TEntity>(string command, IDictionary<string, object> parameters = null) where TEntity : class
        {
            var client = GetDbClient<TEntity>();
            var dbInfo = GetDataSourceInfo<TEntity>();
            var results = (await client.ExecuteQueryAsync(command, parameters)).ToList();
            results.ForEach(x => x["id"] = x["id"].ToString().Replace($"{dbInfo.DiscriminatorValue}|", ""));
            return JsonConvert.DeserializeObject<IEnumerable<TEntity>>(JsonConvert.SerializeObject(results));
        }

        public override async Task<int> SaveChangesAsync<TEntity>(CancellationToken cancellationToken = default)
        {
            IEnumerable<EntityEntry<TEntity>> itemsToUpdate = null;

            _log.Debug<EFCoreCosmosDataContext>(messageTemplate: $"Calling {nameof(SaveChangesAsync)}<TEntity> on context:" +
                                                                 $" {ContextId} with provider: {Database.ProviderName}");

            if (ChangeTracker.HasChanges())
            {
                _log.Debug<EFCoreCosmosDataContext>(messageTemplate: "HasChanges");

                var properties = typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                                .Select(x => new
                                                 {
                                                     PropertyInfo = x,
                                                     Attr = x.GetCustomAttribute<ManuallyTackedAttribute>(),
                                                 })
                                                .Where(x => x.Attr != null);

                if (properties.Any())
                {
                    itemsToUpdate = ChangeTracker.Entries<TEntity>()
                                                 .Where(x => x.State == EntityState.Modified || x.State == EntityState.Added)
                                                 .ToList();
                }

                var result = await base.SaveChangesAsync(cancellationToken);
                _log.Debug<EFCoreCosmosDataContext>(messageTemplate: $"Primary SaveChangesResult: {result}");
                if (itemsToUpdate != null)
                {
                    foreach (var entry in itemsToUpdate)
                    {
                        var jsonProperty = entry.Property<JObject>("__jObject");
                        if (jsonProperty.CurrentValue == null)
                        {
                            continue;
                        }

                        foreach (var item in properties)
                        {
                            var memberKey = !string.IsNullOrWhiteSpace(item.Attr.Name)
                                                ? item.Attr.Name
                                                : item.PropertyInfo.Name;

                            var value = item.PropertyInfo.GetValue(entry.Entity);
                            var expConverter = new ExpandoObjectConverter();
                            var jsonStr = JsonConvert.SerializeObject(value, expConverter);
                            jsonProperty.CurrentValue[memberKey] = JToken.Parse(jsonStr);
                        }

                        entry.State = EntityState.Modified;
                    }

                    result = await base.SaveChangesAsync(cancellationToken);
                    _log.Debug<EFCoreCosmosDataContext>(messageTemplate: $"Dynamic Object update SaveChangesResult: {result}");
                }

                return result;
            }

            return 0;
        }

        public override void MapDynamicObjects<TEntity>(TEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            var entry = Entry(entity);
            var jsonProperty = entry.Property<JObject>("__jObject");

            var properties = typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                            .Select(x => new
                                             {
                                                 PropertyInfo = x,
                                                 Attr = x.GetCustomAttribute<ManuallyTackedAttribute>(),
                                             })
                                            .Where(x => x.Attr != null);

            foreach (var item in properties)
            {
                var memberKey = !string.IsNullOrWhiteSpace(item.Attr.Name)
                                    ? item.Attr.Name
                                    : item.PropertyInfo.Name;

                if (jsonProperty.CurrentValue.ContainsKey(memberKey))
                {
                    var childMemberVal = jsonProperty.CurrentValue[memberKey];
                    var expConverter = new ExpandoObjectConverter();
                    dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(childMemberVal.ToString(), expConverter);
                    item.PropertyInfo.SetValue(entity, obj);
                }
            }
        }

        public override IDbClient<T> GetDbClient<T>()
        {
            var dbInfo = GetDataSourceInfo<T>();
            var cosmosClient = Database.GetCosmosClient();
            var database = cosmosClient.GetDatabase(dbInfo.DatabaseName);
            var container = database.GetContainer(dbInfo.Container);

            return new CosmosDbClient<T>(container, dbInfo);
        }

        private DataSourceInfo GetDataSourceInfo<T>()
        {
            var type = typeof(T);
            var containerAttr = type.GetCustomAttribute<ContainerAttribute>();
            var discriminatorAttr = type.GetCustomAttribute<DiscriminatorAttribute>();
            return new DataSourceInfo
            {
                DatabaseName = GetDatabaseName(),
                Container = containerAttr?.Name,
                PartitionKey = containerAttr?.PartitionKey,
                DiscriminatorProperty = discriminatorAttr.Property,
                DiscriminatorValue = discriminatorAttr.Value,
                TargetEntityType = typeof(T),
            };
        }

        private string GetDatabaseName()
        {
            var optionExtension = (CosmosOptionsExtension)Options.Extensions.FirstOrDefault(x => x.GetType() == typeof(CosmosOptionsExtension));
            return optionExtension.DatabaseName;
        }
    }
}
