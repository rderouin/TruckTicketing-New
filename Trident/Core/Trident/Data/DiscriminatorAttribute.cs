using System;

namespace Trident.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DiscriminatorAttribute : Attribute
    {
        public DiscriminatorAttribute(string property, string value)
        {
            Property = property;
            Value = value;
        }

        public DiscriminatorAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        public string Property { get; set; }
    }
}