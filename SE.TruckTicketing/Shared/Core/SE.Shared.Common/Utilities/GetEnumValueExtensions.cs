using System;
using System.ComponentModel;
using System.Linq;

using Humanizer;

namespace SE.Shared.Common.Utilities;

public static class GetEnumValueExtensions
{
    public static string GetEnumValue<TEnum>(this string description)
        where TEnum : struct, Enum
    {
        return DataDictionary.For<TEnum>().Where(x => x.Value == description).First().Key.ToString();
    }

    public static string GetEnumDescription<TEnum>(this string value)
        where TEnum : struct, Enum
    {
        return Enum.Parse<TEnum>(value).Humanize();
    }

    public static string GetCategory<T>(this T enumValue)
        where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
        {
            return null;
        }

        var category = enumValue.ToString();
        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

        if (fieldInfo != null)
        {
            var attrs = fieldInfo.GetCustomAttributes(typeof(CategoryAttribute), true);
            if (attrs != null && attrs.Length > 0)
            {
                category = ((CategoryAttribute)attrs[0]).Category;
            }
        }

        return category;
    }
}
