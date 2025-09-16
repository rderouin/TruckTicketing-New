namespace SE.Shared.Domain.Rules;

public class ValidationResultState<TErrorCode> where TErrorCode : struct
{
    public ValidationResultState(TErrorCode? errorCode = null, params string[] propertyNames)
    {
        ErrorCode = errorCode;
        PropertyNames = propertyNames;
    }

    public string[] PropertyNames { get; set; }

    public TErrorCode? ErrorCode { get; set; }
}
