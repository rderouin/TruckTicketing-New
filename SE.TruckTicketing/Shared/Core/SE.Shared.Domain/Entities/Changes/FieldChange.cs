using System.Linq;

using Newtonsoft.Json.Linq;

using SE.Shared.Common.Lookups;

namespace SE.Shared.Domain.Entities.Changes;

/// <summary>
///     A data class for storing object comparison results
/// </summary>
public class FieldChange
{
    private readonly JValue _source;

    private readonly JValue _target;

    public FieldChange(JValue source, JValue target)
    {
        _source = source;
        _target = target;
    }

    /// <summary>
    ///     The location of the field in the entity within the entire hierarchy: Signatories[0]
    /// </summary>
    public string FieldLocation { get; set; }

    /// <summary>
    ///     The name of the field that has been changed: FacilityServiceName
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    ///     The original value of the field.
    /// </summary>
    public string ValueBefore { get; set; }

    /// <summary>
    ///     The new value of the field.
    /// </summary>
    public string ValueAfter { get; set; }

    /// <summary>
    ///     The operation on the object/field such as added/updated/deleted.
    /// </summary>
    public ChangeOperation Operation { get; set; }

    /// <summary>
    ///     ID of the object where the change occurred: D6486D57-22E7-4BC5-98E1-E486F69089F4
    /// </summary>
    public string ObjectId { get; set; }

    public string GetReferenceFieldValue(string fieldNameExpression)
    {
        var (fieldName, hierarchicalSkips) = ParseFieldName(fieldNameExpression);
        return FetchNearestReferenceFieldValue(_target ?? _source, fieldName, hierarchicalSkips);

        static string FetchNearestReferenceFieldValue(JToken token, string name, uint skips)
        {
            // starting position = self
            var pointer = token;

            // iterate over the hierarchy
            while (pointer != null)
            {
                // within the hierarchy, there should be a JObject that holds the property
                if (pointer is JObject jObject && jObject.TryGetValue(name, out var value))
                {
                    if (skips > 0)
                    {
                        skips--;
                    }
                    else
                    {
                        // convert the token into a string
                        return value.ToObject<string>();
                    }
                }

                // traverse to the root
                pointer = pointer.Parent;
            }

            return null;
        }

        static (string fieldName, uint hierarchicalSkips) ParseFieldName(string expression)
        {
            // default number of skips
            uint hierarchicalSkips = 0;

            // on blank, return blank
            if (string.IsNullOrEmpty(expression))
            {
                return (expression, hierarchicalSkips);
            }

            // split the string
            var elements = expression.Split("|");

            // parse elements
            var fieldName = elements[0];
            if (elements.Length > 1)
            {
                uint.TryParse(elements[1], out hierarchicalSkips);
            }

            return (fieldName, hierarchicalSkips);
        }
    }

    public override string ToString()
    {
        var fieldPath = string.Join(".", new[] { FieldLocation, FieldName }.Where(e => !string.IsNullOrEmpty(e)));
        return $"{Operation.ToString()} | {fieldPath}: '{ValueBefore}' => '{ValueAfter}'";
    }
}
