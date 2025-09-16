namespace SE.TruckTicketing.Contracts.Lookups;

/// <summary>
/// allows front-end filter based on sales line attachments
/// </summary>
public enum HasAttachments
{
    Any, // sales line has either an internal or an external or another type of attachment
    None, // sales line has no attachments
    Both, // sales line has internal and external attachments
}
