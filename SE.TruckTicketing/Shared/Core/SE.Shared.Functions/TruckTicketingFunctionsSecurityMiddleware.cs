using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Permission;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Caching;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.IoC;
using Trident.Logging;

namespace SE.Shared.Functions;

public class TruckTicketingFunctionSecurityMiddleware : FunctionsSecurityMiddleware
{
    private readonly ITruckTicketingAuthorizationService _authorizationService;

    private readonly ICachingManager _cachingManager;

    private readonly IFunctionControllerFactory _controllerFactory;

    private readonly IFacilityQueryFilterContextAccessor _facilityQueryFilterContextAccessor;

    private readonly IProvider<Guid, PermissionEntity> _permissionsProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    public TruckTicketingFunctionSecurityMiddleware(ILog appLoger,
                                                    IFunctionControllerFactory controllerFactory,
                                                    ITruckTicketingAuthorizationService authorizationService,
                                                    IIoCProvider iocProvider,
                                                    ICachingManager cachingManager,
                                                    IProvider<Guid, PermissionEntity> permissionsProvider,
                                                    IUserContextAccessor userContextAccessor,
                                                    IFacilityQueryFilterContextAccessor facilityQueryFilterContextAccessor) : base(appLoger, controllerFactory, authorizationService, iocProvider)
    {
        _controllerFactory = controllerFactory;
        _authorizationService = authorizationService;
        _cachingManager = cachingManager;
        _permissionsProvider = permissionsProvider;
        _userContextAccessor = userContextAccessor;
        _facilityQueryFilterContextAccessor = facilityQueryFilterContextAccessor;
    }

    protected override async Task<bool> IsAuthorizedPrecheck(FunctionContext context, JwtSecurityToken token, ClaimsPrincipal principal)
    {
        try
        {
            var permissionsClaimValue = _authorizationService.GetClaim(principal, TruckTicketingClaimTypes.Permissions);

            var facilityId = await ReadFacilityId(context);

            if (facilityId.HasText())
            {
                var facilityAccessLevel = _authorizationService.GetFacilityAccessLevel(principal, facilityId);

                permissionsClaimValue = facilityAccessLevel switch
                                        {
                                            FacilityAccessLevels.Admin => await GetAdminAccessLevelPermissionsClaim(),
                                            FacilityAccessLevels.ReadOnly => await GetReadOnlyAccessLevelPermissionsClaim(),
                                            FacilityAccessLevels.InheritedRoles => _authorizationService.GetClaim(principal, TruckTicketingClaimTypes.Permissions),
                                            _ => await GetReadOnlyAccessLevelPermissionsClaim(),
                                        };
            }

            _authorizationService.SetTruckTicketingIdentityClaim(principal, new(ClaimConstants.Permissions, permissionsClaimValue));

            return true;
        }
        catch (JsonReaderException ex)
        {
            context.OverwriteResponseStream("Internal Server Error", HttpStatusCode.InternalServerError);
            Logger.Error<FunctionsSecurityMiddleware>(ex, $"Failed to parse JSON body for {context.FunctionDefinition.EntryPoint}.");
            return false;
        }
        catch (Exception ex)
        {
            context.OverwriteResponseStream("Internal Server Error", HttpStatusCode.InternalServerError);
            Logger.Error<FunctionsSecurityMiddleware>(ex, $"{context.FunctionDefinition.EntryPoint} threw an exception.");
            return false;
        }
    }

    protected override Task<bool> IsAuthorizedPostcheck(FunctionContext context, JwtSecurityToken token, ClaimsPrincipal principal, string originalToken)
    {
        var settings = context.InstanceServices.GetService(typeof(IAppSettings)) as IAppSettings;
        bool.TryParse(settings["AppSettings:SkipClaimsCheck"], out var skipClaimsCheck);

        var userContext = new UserContext
        {
            Principal = principal,
            DisplayName = _authorizationService.GetClaim(principal, ClaimConstants.Name),
            ObjectId = _authorizationService.GetClaim(principal, ClaimConstants.ObjectId),
            OriginalToken = originalToken
        };

        var accessibleFacilityIds = _authorizationService.GetAccessibleFacilityIds(principal).ToArray();
        if (accessibleFacilityIds.Length > 0 && accessibleFacilityIds.All(id => id != FacilityAccessConstants.AllFacilityAccessFacilityId) && !skipClaimsCheck)
        {
            _facilityQueryFilterContextAccessor.FacilityQueryFilterContext = new()
            {
                AllowedFacilityIds = accessibleFacilityIds.Select(id =>
                                                                  {
                                                                      var isValidGuid = Guid.TryParse(id, out var parsedId);
                                                                      return (isValidGuid, parsedId);
                                                                  })
                                                          .Where(id => id.isValidGuid)
                                                          .Select(id => id.parsedId)
                                                          .ToArray(),
            };
        }

        try
        {
            userContext.EmailAddress = _authorizationService.GetClaim(principal, ClaimConstants.Emails);
        }
        catch
        {
            // Ignore email missing email claim
        }
        finally
        {
            _userContextAccessor.UserContext = userContext;
        }

        return Task.FromResult(true);
    }

    private async Task<string> ReadFacilityId(FunctionContext context)
    {
        var funcClassType = GetFunctionClassType(context.FunctionDefinition);

        // Retrieve IFacilityRelatedModel implementation type provided to FacilityClaimsAuthorizeAttribute
        var facilityClaimsAuthorizeModelType = funcClassType?
           .GetMethod(context.FunctionDefinition.Name)?
           .GetCustomAttribute<AuthorizeFacilityAccessWithAttribute>()?
           .Type;

        // If we couldn't retrieve an iFacilityRelatedModel type from the attribute, then assume
        // we are going to authorize for All Facilities
        if (facilityClaimsAuthorizeModelType == null)
        {
            return String.Empty;
        }

        // If we did retrieve a type but it wasn't an iFacilityRelatedModel, then assume normal
        // execution Unfortunately, generic attributes are still in preview so we have to rely
        // on this to get some semblance of type safety.
        if (!facilityClaimsAuthorizeModelType.IsAssignableTo(typeof(IFacilityRelatedModel)))
        {
            Logger.Warning<FunctionsSecurityMiddleware>(messageTemplate:
                                                        $"EnforceFacilityAccess was registered with a type {facilityClaimsAuthorizeModelType.FullName} that is not assignable to IFacilityRelatedModel for {context.FunctionDefinition.EntryPoint}.");

            return String.Empty;
        }

        // We are now safe to read request data
        var request = context.GetHttpRequestData();
        var json = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;
        var model = JsonConvert.DeserializeObject(json, facilityClaimsAuthorizeModelType) as IFacilityRelatedModel;

        var facilityId = model?.FacilityId ?? Guid.Empty;
        return facilityId == Guid.Empty ? string.Empty : facilityId.ToString();
    }

    private async Task<List<PermissionEntity>> LoadPermissions()
    {
        var cacheKey = nameof(TruckTicketingFunctionSecurityMiddleware) + nameof(LoadPermissions);
        var permissions = _cachingManager.Get<List<PermissionEntity>>(cacheKey);
        if (permissions == null)
        {
            permissions = (await _permissionsProvider.Get()).ToList();
            _cachingManager.Set(cacheKey, permissions);
        }

        return permissions;
    }

    private async Task<string> GetAdminAccessLevelPermissionsClaim()
    {
        var cacheKey = nameof(TruckTicketingFunctionSecurityMiddleware) + nameof(GetAdminAccessLevelPermissionsClaim);
        var claim = _cachingManager.Get<string>(cacheKey);
        if (string.IsNullOrEmpty(claim))
        {
            var permissions = await LoadPermissions();
            var permissionsLookup = permissions.GroupBy(permission => permission.Code)
                                               .ToDictionary(group => group.Key,
                                                             group => string.Concat(group.SelectMany(permission => permission.AllowedOperations)
                                                                                         .Select(operation => operation.Value)
                                                                                         .Distinct()));

            claim = permissionsLookup.ToJson().ToBase64();
        }

        return claim;
    }

    private async Task<string> GetReadOnlyAccessLevelPermissionsClaim()
    {
        var cacheKey = nameof(TruckTicketingFunctionSecurityMiddleware) + nameof(GetReadOnlyAccessLevelPermissionsClaim);
        var claim = _cachingManager.Get<string>(cacheKey);
        if (string.IsNullOrEmpty(claim))
        {
            var permissions = await LoadPermissions();
            var permissionsLookup = permissions.GroupBy(permission => permission.Code)
                                               .ToDictionary(group => group.Key,
                                                             group => string.Concat(group.SelectMany(permission => permission.AllowedOperations)
                                                                                         .Select(operation => operation.Value)
                                                                                         .Where(value => value == Permissions.Operations.Read)
                                                                                         .Distinct()));

            claim = permissionsLookup.ToJson().ToBase64();
        }

        return claim;
    }

    private Type GetFunctionClassType(FunctionDefinition definition)
    {
        var pos = definition.EntryPoint.LastIndexOf('.');
        var fqcn = definition.EntryPoint.Substring(0, pos);
        return _controllerFactory.GetControllerType(fqcn);
    }
}
