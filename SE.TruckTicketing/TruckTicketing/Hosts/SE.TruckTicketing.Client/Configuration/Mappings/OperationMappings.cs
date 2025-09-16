using AutoMapper;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.UI.ViewModels;

using Operation = SE.TruckTicketing.UI.ViewModels.Operation;

namespace SE.TruckTicketing.Client.Configuration.Mappings;

public class OperationMappings : Profile
{
    public OperationMappings()
    {
        CreateMap<Operation, Contracts.Models.Accounts.Operation>().ReverseMap();
        CreateMap<Permission, PermissionLookup>().ReverseMap();
        CreateMap<PermissionLookup, PermissionViewModel>().ReverseMap();
        CreateMap<Permission, PermissionViewModel>()
           .ForMember(x => x.Flattened, cfg => cfg.Ignore())
           .ReverseMap();

        CreateMap<RoleViewModel, Role>()
            //.ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
           .ReverseMap();
    }
}
