using System;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Common inner observer that notifies a parent object of the signals
    /// it receives.
    /// </summary>
    /// <typeparam name="T">The value type this inner observer receives.</typeparam>
    internal sealed class InnerSignalObserver<T> : ISignalObserver<T>, IDisposable
    {
        readonly IInnerSignalObserverSupport<T> parent;

        IDisposable upstream;

        ISimpleQueue<T> queue;

        bool done;

        int fusionMode;

        internal InnerSignalObserver(IInnerSignalObserverSupport<T> parent)
        {
            this.parent = parent;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnSubscribe(IDisposable d)
        {
            if (DisposableHelper.SetOnce(ref upstream, d))
            {
                if (d is IFuseableDisposable<T> fd)
                {
                    var m = fd.RequestFusion(FusionSupport.AnyBoundary);

                    if (m == FusionSupport.Sync)
                    {
                        fusionMode = m;
                        Volatile.Write(ref queue, fd);
                        SetDone();
                        parent.Drain();
                        return;
                    }

                    if (m == FusionSupport.Async)
                    {
                        fusionMode = m;
                        Volatile.Write(ref queue, fd);
                    }
                }
            }
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
            if (fusionMode == FusionSupport.None)
            {
                parent.InnerNext(this, value);
            }
            else
            {
                parent.Drain();
            }
        }

        internal ISimpleQueue<T> GetQueue()
        {
            return Volatile.Read(ref queue);
        }

        internal ISimpleQueue<T> GetOrCreateQueue(int capacityHint)
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
