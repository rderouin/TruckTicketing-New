using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum StateProvince
{
    Unspecified = default,

    // US States

    [Category(nameof(CountryCode.US))]
    AL,

    [Category(nameof(CountryCode.US))]
    AK,

    [Category(nameof(CountryCode.US))]
    AZ,

    [Category(nameof(CountryCode.US))]
    AR,

    [Category(nameof(CountryCode.US))]
    CA,

    [Category(nameof(CountryCode.US))]
    CO,

    [Category(nameof(CountryCode.US))]
    CT,

    [Category(nameof(CountryCode.US))]
    DE,

    [Category(nameof(CountryCode.US))]
    DC,

    [Category(nameof(CountryCode.US))]
    FL,

    [Category(nameof(CountryCode.US))]
    GA,

    [Category(nameof(CountryCode.US))]
    HI,

    [Category(nameof(CountryCode.US))]
    ID,

    [Category(nameof(CountryCode.US))]
    IL,

    [Category(nameof(CountryCode.US))]
    IN,

    [Category(nameof(CountryCode.US))]
    IA,

    [Category(nameof(CountryCode.US))]
    KS,

    [Category(nameof(CountryCode.US))]
    KY,

    [Category(nameof(CountryCode.US))]
    LA,

    [Category(nameof(CountryCode.US))]
    ME,

    [Category(nameof(CountryCode.US))]
    MD,

    [Category(nameof(CountryCode.US))]
    MA,

    [Category(nameof(CountryCode.US))]
    MI,

    [Category(nameof(CountryCode.US))]
    MN,

    [Category(nameof(CountryCode.US))]
    MS,

    [Category(nameof(CountryCode.US))]
    MO,

    [Category(nameof(CountryCode.US))]
    MT,

    [Category(nameof(CountryCode.US))]
    NE,

    [Category(nameof(CountryCode.US))]
    NV,

    [Category(nameof(CountryCode.US))]
    NH,

    [Category(nameof(CountryCode.US))]
    NJ,

    [Category(nameof(CountryCode.US))]
    NM,

    [Category(nameof(CountryCode.US))]
    NY,

    [Category(nameof(CountryCode.US))]
    NC,

    [Category(nameof(CountryCode.US))]
    ND,

    [Category(nameof(CountryCode.US))]
    OH,

    [Category(nameof(CountryCode.US))]
    OK,

    [Category(nameof(CountryCode.US))]
    OR,

    [Category(nameof(CountryCode.US))]
    PA,

    [Category(nameof(CountryCode.US))]
    RI,

    [Category(nameof(CountryCode.US))]
    SC,

    [Category(nameof(CountryCode.US))]
    SD,

    [Category(nameof(CountryCode.US))]
    TN,

    [Category(nameof(CountryCode.US))]
    TX,

    [Category(nameof(CountryCode.US))]
    UT,

    [Category(nameof(CountryCode.US))]
    VT,

    [Category(nameof(CountryCode.US))]
    VA,

    [Category(nameof(CountryCode.US))]
    WA,

    [Category(nameof(CountryCode.US))]
    WV,

    [Category(nameof(CountryCode.US))]
    WI,

    [Category(nameof(CountryCode.US))]
    WY,

    // Canadian Provinces

    [Category(nameof(CountryCode.CA))]
    AB,

    [Category(nameof(CountryCode.CA))]
    BC,

    [Category(nameof(CountryCode.CA))]
    MB,

    [Category(nameof(CountryCode.CA))]
    NB,

    [Category(nameof(CountryCode.CA))]
    NL,

    [Category(nameof(CountryCode.CA))]
    NT,

    [Category(nameof(CountryCode.CA))]
    NS,

    [Category(nameof(CountryCode.CA))]
    NU,

    [Category(nameof(CountryCode.CA))]
    ON,

    [Category(nameof(CountryCode.CA))]
    PE,

    [Category(nameof(CountryCode.CA))]
    QC,

    [Category(nameof(CountryCode.CA))]
    SK,

    [Category(nameof(CountryCode.CA))]
    YT,
}
