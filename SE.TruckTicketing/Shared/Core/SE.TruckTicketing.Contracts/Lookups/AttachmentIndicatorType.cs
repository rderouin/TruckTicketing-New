namespace SE.TruckTicketing.Contracts.Lookups;

/// <summary>
/// used to color code sales line list to indicate attachment type
/// </summary>
public enum AttachmentIndicatorType
{
    None, // no attachments
    Internal, // only an internal attachment
    External, // only an external attachment
    InternalExternal, // both an internal and external attachment
    Neither, // has attachments, but no internal or external attachment
}
