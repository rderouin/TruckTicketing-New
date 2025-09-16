using System;
using System.Threading;

namespace SE.Shared.Domain;

public interface IFacilityQueryFilterContextAccessor
{
    FacilityQueryFilterContext FacilityQueryFilterContext { get; set; }
}

public class FacilityQueryFilterContext
{
    public Guid[] AllowedFacilityIds { get; set; } = Array.Empty<Guid>();
}

public class FacilityQueryFilterContextAccessor : IFacilityQueryFilterContextAccessor
{
    private static readonly AsyncLocal<FacilityQueryFilterContextRedirect> CurrentContext = new();

    public virtual FacilityQueryFilterContext FacilityQueryFilterContext
    {
        get
        {
            var context = CurrentContext.Value?.HeldContext;
            return context;
        }

        set
        {
            var holder = CurrentContext.Value;
            if (holder != null)
            {
                // Clear current context trapped in the AsyncLocals, as its done.
                holder.HeldContext = null;
            }

            if (value != null)
            {
                // Use an object indirection to hold the context in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                CurrentContext.Value = new() { HeldContext = value };
            }
        }
    }

    private class FacilityQueryFilterContextRedirect
    {
        public FacilityQueryFilterContext HeldContext;
    }
}
