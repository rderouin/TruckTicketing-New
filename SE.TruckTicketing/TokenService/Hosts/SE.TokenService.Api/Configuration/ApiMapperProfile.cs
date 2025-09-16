using System;
using System.Linq;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.UserProfile;
using SE.TokenService.Contracts.Api.Models.Accounts;

namespace SE.TokenService.Api.Configuration;

public class ApiMapperProfile : BaseMapperProfile
{
    public ApiMapperProfile()
    {
        CreateMap<B2CApiConnectorRequest, UserProfileEntity>()
           .IgnoreTTEntityBaseMembers()
           .ForMember(userProfile => userProfile.CreatedBy, opt => opt.MapFrom((_, _, _) => "Azure AD B2C"))
           .ForMember(userProfile => userProfile.CreatedById, opt => opt.MapFrom((_, _, _) => "Azure AD B2C"))
           .ForMember(userProfile => userProfile.CreatedAt, opt => opt.MapFrom((_, _, _) => DateTimeOffset.UtcNow))
           .ForMember(userProfile => userProfile.UpdatedBy, opt => opt.MapFrom((_, _, _) => "Azure AD B2C"))
           .ForMember(userProfile => userProfile.UpdatedById, opt => opt.MapFrom((_, _, _) => "Azure AD B2C"))
           .ForMember(userProfile => userProfile.UpdatedAt, opt => opt.MapFrom((_, _, _) => DateTimeOffset.UtcNow))
           .ForMember(userProfile => userProfile.LocalAuthId, options => options.MapFrom(request => request.ObjectId))
           .ForMember(userProfile => userProfile.ExternalAuthId, options => options.MapFrom(request => request.Identities.FirstOrDefault().IssuerAssignedId))
           .ForMember(userProfile => userProfile.Roles, options => options.Ignore())
           .ForMember(userProfile => userProfile.AllFacilitiesAccessLevel, options => options.Ignore())
           .ForMember(userProfile => userProfile.FirstName, options => options.MapFrom(request => request.GivenName))
           .ForMember(userProfile => userProfile.LastName, options => options.MapFrom(request => request.Surname))
           .ForMember(userProfile => userProfile.SpecificFacilityAccessAssignments, options => options.Ignore())
           .ForMember(userProfile => userProfile.EnforceSpecificFacilityAccessLevels, options => options.Ignore())
           .ReverseMap();
    }
}
