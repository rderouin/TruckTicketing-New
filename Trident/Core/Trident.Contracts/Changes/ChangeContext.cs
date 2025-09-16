using System;
using System.Threading;

namespace Trident.Contracts.Changes;

public class ChangeContext
{
    public Guid CorrelationId { get; set; }

    public static ChangeContext FromThreadContext()
    {
        return Thread.GetData(GetThreadDataStoreSlot()) as ChangeContext;
    }

    public void ToThreadContext()
    {
        Thread.SetData(GetThreadDataStoreSlot(), this);
    }

    private static LocalDataStoreSlot GetThreadDataStoreSlot()
    {
        return Thread.GetNamedDataSlot(nameof(ChangeContext));
    }
}
