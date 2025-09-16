using System;

namespace SE.Shared.Common.Extensions;

public static class DateTimeExtensions
{
    private static readonly TimeSpan AlbertaOffset = TimeSpan.FromHours(-7);
    private static readonly TimeSpan DstToAlbertaOffset = TimeSpan.FromHours(-6);

    public static DateTimeOffset ToAlbertaOffset(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToOffset(AlbertaOffset);
    }

    public static DateTimeOffset ToAlbertaOffsetBasedonDST(this DateTimeOffset dateTimeOffset)
    {
        TimeZoneInfo mountainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
        
        return mountainTimeZone.IsDaylightSavingTime(dateTimeOffset) ?
                   dateTimeOffset.ToOffset(DstToAlbertaOffset) : dateTimeOffset.ToOffset(AlbertaOffset);      
    }
}
