using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Humanizer;

namespace SE.Shared.Common.Utilities;

public class DataDictionary
{
    public static IReadOnlyDictionary<TEnum, string> For<TEnum>(bool ignoreDefault = true, bool excludeObsolete = false)
        where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();

        if (excludeObsolete)
        {
            var excludedValues = new List<TEnum>();
            foreach (var value in values)
            {
                var memberInfo = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
                var hasObsoletes = memberInfo.GetCustomAttributes(typeof(ObsoleteAttribute), false);
                if (hasObsoletes.Any())
                {
                    excludedValues.Add(value);
                }
            }

            values = values.Except(excludedValues).ToArray();
        }

        var filteredValues = ignoreDefault ? values.Where(value => !value.Equals(default(TEnum))) : values;
        return filteredValues.ToDictionary(value => value, value => value.Humanize());
    }

    public static IReadOnlyDictionary<bool, string> ForBooleanOnlyDictionary()
    {
        var values = new[] { true, false };
        return values.ToDictionary(value => value, value => value.ToString());
    }

    public static IReadOnlyDictionary<TEnum, string> ForSelectedValues<TEnum>(List<string> includedValues, bool ignoreDefault = true, bool humanize = true)
        where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        var filteredValues = ignoreDefault && includedValues.Any() ? values.Where(value => !value.Equals(default(TEnum)) && includedValues.Contains(value.ToString())) :
                             includedValues.Any() ? values.Where(value => includedValues.Contains(value.ToString())) :
                             ignoreDefault ? values.Where(value => !value.Equals(default(TEnum))) : values;

        return filteredValues.ToDictionary(value => value, value => humanize ? value.Humanize() : value.ToString());
    }

    public static IReadOnlyDictionary<TEnum, string> ForSelectedCategory<TEnum>(string category, bool ignoreDefault = true, bool humanize = true)
        where TEnum : struct, Enum
    {
        var enumValuesByCategory = GetDataDictionaryByCategory<TEnum>();

        return enumValuesByCategory[category];
    }

    public static IDictionary<string, Dictionary<TEnum, string>> GetDataDictionaryByCategory<TEnum>(bool ignoreDefault = true)
        where TEnum : struct, Enum
    {
        if (!typeof(TEnum).IsEnum)
        {
            return null;
        }

        var category = string.Empty;
        IDictionary<string, Dictionary<TEnum, string>> enumDataByCategory = new Dictionary<string, Dictionary<TEnum, string>>();
        var enumValues = Enum.GetValues<TEnum>();
        var filteredValues = ignoreDefault ? enumValues.Where(value => !value.Equals(default(TEnum))) : enumValues;

        foreach (var enumValue in filteredValues)
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            if (fieldInfo != null)
            {
                var attrs = fieldInfo.GetCustomAttributes(typeof(CategoryAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    category = ((CategoryAttribute)attrs[0]).Category;
                    enumDataByCategory.TryAdd(category, new());
                    enumDataByCategory[category].TryAdd(enumValue, enumValue.Humanize());
                }
                else
                {
                    enumDataByCategory.TryAdd("All", new());
                    enumDataByCategory["All"].TryAdd(enumValue, enumValue.Humanize());
                }
            }
        }

        return enumDataByCategory;
    }
}
