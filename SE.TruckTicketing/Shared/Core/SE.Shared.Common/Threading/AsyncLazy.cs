using System;
using System.Threading.Tasks;

namespace SE.Shared.Common.Threading;

public class AsyncLazy<T> : Lazy<Task<T>>
{
    public AsyncLazy(Func<Task<T>> taskFactory) : base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
    {
    }
}
