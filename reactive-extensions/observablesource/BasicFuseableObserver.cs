using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Base class for intermediate operators that want to support
    /// transitive fusion.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The downstream value type.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    internal abstract class BasicFuseableObserver<T, R> : ISignalObserver<T>, IFuseableDisposable<R>
    {
        protected readonly ISignalObserver<R> downstream;

        protected IDisposable upstream;

        protected bool done;

        protected int fusionMode;

        /// <summary>
        /// If not null, the upstream supports fusion.
        /// </summary>
        protected IFuseableDisposable<T> queue;

        public BasicFuseableObserver(ISignalObserver<R> downstream)
        {
            this.downstream = downstream;
        }

        public virtual void OnSubscribe(IDisposable d)
        {
            this.upstream = d;
            if (d is IFuseableDisposable<T> f)
            {
                this.queue = f;
            }
            downstream.OnSubscribe(this);
        }

        public virtual void OnError(Exception error)
        {
            if (done)
            {
                return;
            }
            done = true;
            downstream.OnError(error);
        }

        public virtual void OnCompleted()
        {
            if (done)
            {
                return;
            }
            downstream.OnCompleted();
        }

        public void Clear()
        {
            queue.Clear();
        }

        public virtual void Dispose()
        {
            upstream.Dispose();
        }

        public bool IsEmpty()
        {
            return queue.IsEmpty();
        }

        public abstract int RequestFusion(int mode);

        public abstract R TryPoll(out bool success);

        public abstract void OnNext(T item);

        protected int RequestBoundaryFusion(int mode)
        {
            var q = queue;
            if (q != null) {
                if ((mode & FusionSupport.Boundary) == 0)
                {
                    var m = q.RequestFusion(mode);
                    fusionMode = m;
                    return m;
                }
            }
            return FusionSupport.None;
        }

        public bool TryOffer(R item)
        {
            throw new InvalidOperationException("Should not be called!");
        }
    }
}
