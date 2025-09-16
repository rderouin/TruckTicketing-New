using System;

namespace Trident.SourceGeneration.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UseHttpFunctionAttribute : Attribute
    {
        public UseHttpFunctionAttribute(HttpFunctionApiMethod method)
        {
            Method = method;
        }

        public HttpFunctionApiMethod Method { get; }

        public string Route { get; set; }

        public string ClaimsAuthorizeResource { get; set; }

        public string ClaimsAuthorizeOperation { get; set; }

        public Type AuthorizeFacilityAccessWith { get; set; }
    }

    public enum HttpFunctionApiMethod
    {
        GetById,

        Search,

        Create,

        Update,

        Delete,

        Patch,
    }
}
