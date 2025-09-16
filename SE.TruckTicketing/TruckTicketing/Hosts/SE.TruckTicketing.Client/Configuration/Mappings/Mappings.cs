using AutoMapper;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Navigation;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Operation = SE.TruckTicketing.UI.ViewModels.Operation;

namespace SE.TruckTicketing.Client.Configuration.Mappings;

public class Mappings : Profile
{
    public Mappings()
    {
        CreateMap<Operation, Contracts.Models.Accounts.Operation>().ReverseMap();
        CreateMap<Permission, PermissionLookup>().ReverseMap();
        CreateMap<PermissionLookup, PermissionViewModel>().ReverseMap();
        CreateMap<Permission, PermissionViewModel>()
           .ForMember(x => x.Flattened, cfg => cfg.Ignore())
           .ReverseMap();

        CreateMap<RoleViewModel, Role>()
           .ReverseMap();

        CreateMap<Role, UserProfileRole>()
           .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.Id))
           .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Name))
           .ReverseMap();

        CreateMap<NavigationMenuItem, NavigationItemModel>()
           .ForMember(dest => dest.RelativeUrl, opt => opt.MapFrom(src => src.Path))
           .ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimName))
           .ForMember(dest => dest.NavigationItems, opt => opt.MapFrom(src => src.SubMenus))
           .ReverseMap();

        CreateMap<NoteViewModel, Note>()
           .ReverseMap();

        CreateMap<EDIFieldValue, EDIValueViewModel>()
           .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
           .ForMember(dest => dest.EDIFieldValueContent, opt => opt.MapFrom(src => src.EDIFieldValueContent))
           .ForMember(dest => dest.IsNew, opt => opt.MapFrom(src => src.IsNew))
           .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.DefaultValue));

        CreateMap<NewAccountModel, NewAccountViewModel>()
           .ReverseMap();
    }
}
