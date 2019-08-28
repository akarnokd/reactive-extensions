using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#if DEBUG
[assembly: InternalsVisibleTo("reactive-extensions-test")]
[assembly: InternalsVisibleTo("reactive-extensions-benchmarks")]
#else
[assembly: InternalsVisibleTo("reactive-extensions-test,PublicKey=" +
    "002400000480000094000000060200000024000052534131000400000100010021abffd06f6b96" +
    "c263f931fc995e76f6de4c41063813f875b1e6eef6e207000c19d27e576d13c1865418b6158859" +
    "c8b9f59037e9d3e1b855ed7a51d99369f64e7a09cd32ba4d3cc97f71546b02e3e542fba8c9de6d" +
    "cd9d27c393ae27be179dd4053f6bf8b85e5a4e4792f6606e69a1d4a09a480a81a78107e722b569" +
    "bfcca5ba"
)]
[assembly: InternalsVisibleTo("reactive-extensions-benchmarks,PublicKey=" +
    "002400000480000094000000060200000024000052534131000400000100010021abffd06f6b96" +
    "c263f931fc995e76f6de4c41063813f875b1e6eef6e207000c19d27e576d13c1865418b6158859" +
    "c8b9f59037e9d3e1b855ed7a51d99369f64e7a09cd32ba4d3cc97f71546b02e3e542fba8c9de6d" +
    "cd9d27c393ae27be179dd4053f6bf8b85e5a4e4792f6606e69a1d4a09a480a81a78107e722b569" +
    "bfcca5ba")]
#endif

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Test helper scheduler that executes actions on the current thread
    /// sleeping if necessary.
    /// </summary>
    internal sealed class ImmediateScheduler : IScheduler
    {
        public DateTimeOffset Now => DateTimeOffset.Now;

        internal static readonly IScheduler INSTANCE = new ImmediateScheduler();

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            return action(this, state);
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            Task.Delay(dueTime).Wait();
            return action(this, state);
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var diff = dueTime - Now;
            Task.Delay(diff).Wait();
            return action(this, state);
        }
    }
}
