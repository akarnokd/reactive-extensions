using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal abstract class DeferredScalarDisposable<T> : IFuseableDisposable<T>
    {
        protected readonly ISignalObserver<T> downstream;

        int state;

        T value;

        static readonly int Empty = 0;
        static readonly int FusedEmpty = 4;
        static readonly int FusedReady = 5;
        static readonly int Disposed = 6;

        protected DeferredScalarDisposable(ISignalObserver<T> downstream)
        {
            this.downstream = downstream;
        }

        public virtual void Clear()
        {
            value = default(T);
            Interlocked.Exchange(ref state, Disposed);
        }

        public virtual void Dispose()
        {
            Interlocked.Exchange(ref state, Disposed);
        }

        public bool IsEmpty()
        {
            var s = Volatile.Read(ref state);
            return s != FusedReady;
        }

        public int RequestFusion(int mode)
        {
            if ((mode & FusionSupport.Async) != 0)
            {
                Volatile.Write(ref state, FusedEmpty);
                return FusionSupport.Async;
            }
            return FusionSupport.None;
        }

        public bool TryOffer(T item)
        {
            throw new InvalidOperationException("Should not be called");
        }

        public T TryPoll(out bool success)
        {
            if (Volatile.Read(ref state) == FusedReady)
            {
                success = true;
                var v = value;
                value = default(T);
                Volatile.Write(ref state, Disposed);
                return v;
            }
            success = false;
            return default(T);
        }

        public void Complete(T result)
        {
            var s = Volatile.Read(ref state);
            if (s == Empty)
            {
                downstream.OnNext(result);
                if (Volatile.Read(ref state) != Disposed)
                {
                    downstream.OnCompleted();
                }
            }
            else
            if (s == FusedEmpty)
            {
                value = result;
                Volatile.Write(ref state, FusedReady);
                downstream.OnNext(default(T));
                downstream.OnCompleted();
            }

        }

        public void Complete()
        {
            if (Volatile.Read(ref state) != Disposed)
            {
                Volatile.Write(ref state, Disposed);
                downstream.OnCompleted();
            }
        }

        public void Error(Exception error)
        {
            if (Volatile.Read(ref state) != Disposed)
            {
                Volatile.Write(ref state, Disposed);
                downstream.OnError(error);
            }
        }

        public bool IsDisposed()
        {
            return Volatile.Read(ref state) == Disposed;
        }
    }
}
