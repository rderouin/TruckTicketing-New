namespace SE.TridentContrib.Extensions.Security;

public class UserContextAccessor : IUserContextAccessor
{
    private static readonly AsyncLocal<UserContextRedirect> CurrentContext = new();

    public virtual UserContext UserContext
    {
        get => CurrentContext.Value?.HeldContext;

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

    private class UserContextRedirect
    {
        public UserContext HeldContext;
    }
}
