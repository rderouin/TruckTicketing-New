using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

using Newtonsoft.Json;

using Trident.Contracts;
using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;
using Trident.Data;
using Trident.Extensions;

namespace Trident.Domain
{
    /// <summary>
    ///     Supply an abstract implementation for all entities in using the Reference Architecture
    /// </summary>
    public abstract class Entity
    {
        protected object id;

        /// <summary>
        ///     Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [Key]
        [JsonProperty("Id")]
        public object Id
        {
            get => id;
            set => id = value;
        }
    }

    /// <summary>
    ///     Class EntityBase.
    ///     Implements the <see cref="TridentOptionsBuilder.Domain.Entity" />
    ///     Implements the <see cref="TridentOptionsBuilder.Contracts.IHaveId{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="TridentOptionsBuilder.Domain.Entity" />
    /// <seealso cref="TridentOptionsBuilder.Contracts.IHaveId{T}" />
    public abstract class EntityBase<T> : Entity, IHaveId<T>
    {
        public EntityBase()
        {
            id = default(T);
        }

        /// <summary>
        ///     Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [Key]
        [JsonProperty("Id")]
        public new T Id
        {
            get => base.Id != null ? (T)base.Id : default;
            set => base.Id = value;
        }

        /// <summary>
        ///     Gets the primary identifier for this instance.
        /// </summary>
        /// <returns>T.</returns>
        T IHaveId<T>.GetId()
        {
            return Id;
        }

        /// <summary>
        ///     Gets the primary identifier for this instance.
        /// </summary>
        /// <returns>T.</returns>
        public T GetId()
        {
            return Id;
        }
    }

    /// <summary>
    ///     Class EntityGuidBase.
    ///     Implements the <see cref="TridentOptionsBuilder.Domain.EntityBase{System.Guid}" />
    /// </summary>
    /// <seealso cref="TridentOptionsBuilder.Domain.EntityBase{System.Guid}" />
    public abstract class EntityGuidBase : EntityBase<Guid>
    {
        /// <summary>
        ///     Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [Key]
        public new Guid Id
        {
            get => base.Id != default ? base.Id : base.Id = Guid.NewGuid();
            set => base.Id = value;
        }
    }

    /// <summary>
    ///     Class EntityIntBase.
    ///     Implements the <see cref="TridentOptionsBuilder.Domain.EntityBase{System.Int32}" />
    /// </summary>
    /// <seealso cref="TridentOptionsBuilder.Domain.EntityBase{System.Int32}" />
    public abstract class EntityIntBase : EntityBase<int>
    {
    }

    public abstract class EntityLongBase : EntityBase<long>
    {
    }

    public abstract class TridentDualIdenityEntityBase<T> : EntityBase<T>
    {
        public Guid Identifier { get; set; }
    }

    public abstract class DocumentDbEntityBase<T> : EntityBase<T>, IHaveCompositeKey<T>
    {
        protected DocumentDbEntityBase()
        {
            var thisType = GetType();
            var containerAttr = thisType.GetCustomAttribute<ContainerAttribute>();
            var discriminatorAttr = thisType.GetCustomAttribute<DiscriminatorAttribute>();

            containerAttr.GuardIsNotNull(nameof(containerAttr), "Container Attribute is Require for entities stored in Cosmos DB");
            discriminatorAttr.GuardIsNotNull(nameof(discriminatorAttr), "Discriminator Attribute is Require for entities stored in Cosmos DB");

            if (containerAttr != null)
            {
                containerAttr.Name.GuardIsNotNullOrWhitespace($"{nameof(containerAttr)}.{nameof(containerAttr.Name)}");
                containerAttr.PartitionKey.GuardIsNotNullOrWhitespace($"{nameof(containerAttr)}.{nameof(containerAttr.PartitionKey)}");
                containerAttr.PartitionKeyValue.GuardIsNotNullOrWhitespace($"{nameof(containerAttr)}.{nameof(containerAttr.PartitionKeyValue)}");
                containerAttr.PartitionKeyType.GuardNotUndefined($"{nameof(containerAttr)}.{nameof(containerAttr.PartitionKeyType)}");
                DocumentType = containerAttr.PartitionKeyType == PartitionKeyType.WellKnown ? containerAttr.PartitionKeyValue : null;
            }

            if (discriminatorAttr != null)
            {
                discriminatorAttr.Property.GuardIsNotNullOrWhitespace($"{nameof(discriminatorAttr)}.{nameof(discriminatorAttr.Property)}");
                discriminatorAttr.Value.GuardIsNotNullOrWhitespace($"{nameof(discriminatorAttr)}.{nameof(discriminatorAttr.Value)}");
                EntityType = discriminatorAttr.Value;
            }
        }

        public string EntityType { get; set; }

        public string DocumentType { get; set; }

        [JsonIgnore]
        [NotMapped]
        public CompositeKey<T> Key => new CompositeKey<T>(Id, DocumentType);
    }
}
