using System;
using System.Reactive.Subjects;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Automatically connect the upstream IConnectableObservable once the
    /// specified number of IObservers have subscribed to this IObservable.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal sealed class ObservableSourceAutoConnect<T> : IObservableSource<T>
    {
        readonly IConnectableObservableSource<T> source;

        readonly int minObservers;

        readonly Action<IDisposable> onConnect;

        int count;

        internal ObservableSourceAutoConnect(IConnectableObservableSource<T> source, int minObservers, Action<IDisposable> onConnect)
        {
            this.source = source;
            this.minObservers = minObservers;
            this.onConnect = onConnect;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(observer);

            if (Volatile.Read(ref count) < minObservers)
            {
                if (Interlocked.Increment(ref count) == minObservers)
                {
                    var c = source.Connect();
                    onConnect?.Invoke(c);
                }
            }
        }
    }
}