using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Bridges the callback-style world with the reactive world
    /// through a optionally valued maybe.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    internal sealed class MaybeCreate<T> : IMaybeSource<T>
    {
        readonly Action<IMaybeEmitter<T>> onSubscribe;

        public MaybeCreate(Action<IMaybeEmitter<T>> onSubscribe)
        {
            this.onSubscribe = onSubscribe;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var parent = new CreateDisposableEmitter(observer);
            observer.OnSubscribe(parent);
            try
            {
                onSubscribe(parent);
            }
            catch (Exception ex)
            {
                parent.OnError(ex);
            }
        }

        internal sealed class CreateDisposableEmitter : IMaybeEmitter<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IDisposable resource;

            public CreateDisposableEmitter(IMaybeObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref resource);
            }

            public bool IsDisposed()
            {
                return DisposableHelper.IsDisposed(ref resource);
            }

            public void OnCompleted()
            {
                var d = Interlocked.Exchange(ref resource, DisposableHelper.DISPOSED);
                if (d != DisposableHelper.DISPOSED)
                {
                    downstream.OnCompleted();
                    d?.Dispose();
                }
            }

            public void OnError(Exception error)
            {
                var d = Interlocked.Exchange(ref resource, DisposableHelper.DISPOSED);
                if (d != DisposableHelper.DISPOSED)
                {
                    downstream.OnError(error);
                    d?.Dispose();
                }
            }

            public void OnSuccess(T item)
            {
                var d = Interlocked.Exchange(ref resource, DisposableHelper.DISPOSED);
                if (d != DisposableHelper.DISPOSED)
                {
                    downstream.OnSuccess(item);
                    d?.Dispose();
                }
            }

            public void SetResource(IDisposable d)
            {
                DisposableHelper.Set(ref resource, d);
            }
        }
    }
}
