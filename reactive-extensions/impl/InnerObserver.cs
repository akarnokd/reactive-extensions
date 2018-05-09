using System;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Common inner observer that notifies a parent object of the signals
    /// it receives.
    /// </summary>
    /// <typeparam name="T">The value type this inner observer receives.</typeparam>
    internal sealed class InnerObserver<T> : IObserver<T>, IDisposable
    {
        readonly IInnerObserverSupport<T> parent;

        IDisposable upstream;

        SpscLinkedArrayQueue<T> queue;

        bool done;

        internal InnerObserver(IInnerObserverSupport<T> parent)
        {
            this.parent = parent;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public void OnCompleted()
        {
            parent.InnerComplete(this);
        }

        public void OnError(Exception error)
        {
            parent.InnerError(this, error);
        }

        public void OnNext(T value)
        {
            parent.InnerNext(this, value);
        }

        internal SpscLinkedArrayQueue<T> GetQueue()
        {
            return Volatile.Read(ref queue);
        }

        internal SpscLinkedArrayQueue<T> GetOrCreateQueue(int capacityHint)
        {
            return GetQueue() ?? CreateQueue(capacityHint);
        }

        internal SpscLinkedArrayQueue<T> CreateQueue(int capacityHint)
        {
            var q = new SpscLinkedArrayQueue<T>(capacityHint);
            Interlocked.Exchange(ref queue, q);
            return q;
        }

        internal bool IsDone()
        {
            return Volatile.Read(ref done);
        }

        internal void SetDone()
        {
            Volatile.Write(ref done, true);
        }
    }
}
