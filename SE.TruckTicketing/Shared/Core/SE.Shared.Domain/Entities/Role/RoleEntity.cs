using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using SE.Shared.Domain.Entities.Permission;
using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Role;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Accounts, nameof(DocumentType), Containers.Partitions.Accounts, PartitionKeyType.WellKnown)]
[Discriminator(Containers.Discriminators.Role, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
public class RoleEntity : TTEntityBase
{
    public string Name { get; set; }

    public bool Deleted { get; set; }

    public List<PermissionLookupEntity> Permissions { get; set; } = new();

    public string PermissionDisplay
    {
        get
        {
            IEnumerable<string> GetPermissionDisplayEntries()
            {
                if (Permissions != null)
                {
                    foreach (var perm in Permissions)
                    {
                        foreach (var ops in perm.AssignedOperations)
                        {
                            yield return $"{ops.Display} {perm.Display}";
                        }
                    }
                }
            }

            var entries = GetPermissionDisplayEntries();
            var result = string.Join(", ", entries);
            return result;
        }
        set { }
    }
}

public class PermissionLookupEntity : OwnedLookupEntityBase<Guid>
{
    public string Code { get; set; }

    public string Display { get; set; }

    [NotMapped]
    public string Value => string.Join("", AssignedOperations.OrderBy(x => x.Value).Select(x => x.Value).ToList());

    public List<OperationEntity> AssignedOperations { get; set; } = new();
}
