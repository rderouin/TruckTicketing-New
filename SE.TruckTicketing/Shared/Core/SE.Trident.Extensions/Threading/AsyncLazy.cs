using System;
using System.Threading.Tasks;

namespace SE.TridentContrib.Extensions.Threading;

public class AsyncLazy<T> : Lazy<Task<T>>
{
    public AsyncLazy(Func<Task<T>> taskFactory) : base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
    {
    }
}
