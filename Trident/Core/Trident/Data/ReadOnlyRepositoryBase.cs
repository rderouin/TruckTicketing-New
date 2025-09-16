using System;
using System.Collections.Concurrent;

using Trident.Data.Contracts;

namespace Trident.Data
{
    /// <summary>
    ///     Provieds an implemenation of a read-only repositry.
    ///     Implements the <see cref="TridentOptionsBuilder.Data.Contracts.IReadOnlyResponsitory{TEntity}" />
    /// </summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <seealso cref="TridentOptionsBuilder.Data.Contracts.IReadOnlyResponsitory{TEntity}" />
    public abstract class ReadOnlyRepositoryBase<TEntity> : RepositoryBase, IReadOnlyRepository<TEntity>
        where TEntity : class
    {
        /// <summary>
        ///     The lazy context
        /// </summary>
        private readonly Lazy<IContext> _lazyContext;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RepositoryBase{TEntity}" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        protected ReadOnlyRepositoryBase(Lazy<IContext> context)
        {
            _lazyContext = context;
        }

        /// <summary>
        ///     Gets the context.
        /// </summary>
        /// <value>The context.</value>
        protected IContext Context => _lazyContext.Value;
    }

    public abstract class RepositoryBase : IRepository
    {
        public const string DisablePartitionKeyInferenceOption = "DisablePartitionKeyInference";

        public const string DisablePartitionKeyInferenceSetting = "AppSettings:" + DisablePartitionKeyInferenceOption;

        public static ConcurrentDictionary<string, object> Options { get; } = new ConcurrentDictionary<string, object>();
    }
}
