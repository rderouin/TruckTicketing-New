using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

using Trident.Azure.Security;
using Trident.Contracts.Configuration;
using Trident.IoC;
using Trident.Logging;
using Trident.Security;

namespace Trident.Azure.Functions
{
    public class FunctionsSecurityMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IAuthorizationService _authorizationService;

        private readonly IFunctionControllerFactory _controllerFactory;

        private readonly IIoCProvider _iocProvider;

        protected readonly ILog Logger;

        public FunctionsSecurityMiddleware(
            ILog appLoger,
            IFunctionControllerFactory controllerFactory,
            IAuthorizationService authorizationService,
            IIoCProvider iocProvider
        )
        {
            Logger = appLoger;
            _controllerFactory = controllerFactory;
            _authorizationService = authorizationService;
            _iocProvider = iocProvider;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var settings = context.InstanceServices.GetService(typeof(IAppSettings)) as IAppSettings;
            bool.TryParse(settings["AppSettings:EnableSecurityExceptions"], out var enableSecurityExceptions);
            bool.TryParse(settings["AppSettings:SkipClaimsCheck"], out var skipClaimsCheck);

            try
            {
                var log = context.GetLogger<FunctionsSecurityMiddleware>();

                var funcClassType = GetFunctionClassType(context.FunctionDefinition);

                if (funcClassType != null)
                {
                    var classLevelClaimsAuthAttrs = funcClassType.GetCustomAttributes<ClaimsAuthorizeAttribute>();
                    var methodLevelClaimsAuthAttrs = funcClassType.GetMethod(context.FunctionDefinition.Name)
                                                                 ?.GetCustomAttributes<ClaimsAuthorizeAttribute>();

                    var claims =
                        new List<ClaimsAuthorizeAttribute.ClaimEntity>(classLevelClaimsAuthAttrs.Select(x => x.Claim));

                    claims.AddRange(methodLevelClaimsAuthAttrs.Select(x => x.Claim));
                    var authorized = true;

                    if (claims.Any())
                    {
                        var headers =
                            JsonConvert.DeserializeObject<Headers>(context.BindingContext.BindingData[nameof(Headers)]
                                                                          .ToString());

                        var token = headers.Authorization
                                           .Replace("bearer", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                                           .Trim();

                        var principal = await _authorizationService.ValidateToken(token);

                        if (principal != null)
                        {
                            var securityToken = ReadJwtToken(token);

                            authorized &= await IsAuthorizedPrecheck(context, securityToken, principal);

                            if (!skipClaimsCheck)
                                foreach (var claim in claims)
                                    if (!_authorizationService.HasPermission(principal, claim.Type, claim.Value))
                                    {
                                        authorized = false;
                                        var secMsg =
                                            $"{context.FunctionDefinition.EntryPoint} | User: {securityToken.Subject} doesn't have authorization claims for {claim.Type} with value {claim.Value}";

                                        var msg = enableSecurityExceptions
                                                      ? secMsg
                                                      : "User is unauthorized.";

                                        Logger.Information<FunctionsSecurityMiddleware>(messageTemplate: secMsg);
                                        context.OverwriteResponseStream(msg, HttpStatusCode.Unauthorized);
                                        break;
                                    }

                            authorized &= await IsAuthorizedPostcheck(context, securityToken, principal, token);
                        }
                        else
                        {
                            authorized = false;
                            Logger.Information<FunctionsSecurityMiddleware>(messageTemplate:
                                                                            "Token deserialization didn't result into a Claims Principal.");

                            context.OverwriteResponseStream("Unauthorized Access", HttpStatusCode.Unauthorized);
                        }
                    }

                    if (authorized)
                        await next(context);
                }
                else
                {
                    Logger.Information(messageTemplate:
                                       $"{context.FunctionDefinition.EntryPoint} is not registered for security evaluation. Doesn't implement IFunctionController interface.");

                    await next(context);
                }
            }
            catch (SecurityTokenException stv)
            {
                Logger.Error<FunctionsSecurityMiddleware>(stv,
                                                          $"{context.FunctionDefinition.EntryPoint} Access Denied.");

                var msg = enableSecurityExceptions ? stv.ToString() : "Unauthorized Access";
                context.OverwriteResponseStream(msg, HttpStatusCode.Unauthorized);
            }
            catch (Exception ex)
            {
                Logger.Error<FunctionsSecurityMiddleware>(ex,
                                                          $"{context.FunctionDefinition.EntryPoint} threw an exception.");

                context.OverwriteResponseStream("Internal Server Error", HttpStatusCode.InternalServerError);
            }
        }

        protected virtual Task<bool> IsAuthorizedPrecheck(FunctionContext context,
                                                          JwtSecurityToken token,
                                                          ClaimsPrincipal principal)
        {
            return Task.FromResult(true);
        }

        protected virtual Task<bool> IsAuthorizedPostcheck(FunctionContext context,
                                                           JwtSecurityToken token,
                                                           ClaimsPrincipal principal,
                                                           string originalToken)
        {
            return Task.FromResult(true);
        }

        private JwtSecurityToken ReadJwtToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken;
        }

        private Type GetFunctionClassType(FunctionDefinition definition)
        {
            var pos = definition.EntryPoint.LastIndexOf('.');
            var fqcn = definition.EntryPoint.Substring(0, pos);
            return _controllerFactory.GetControllerType(fqcn);
        }

        private class Headers
        {
            public string Authorization { get; set; }
        }
    }
}
