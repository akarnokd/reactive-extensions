using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Bridges the callback-style world with the reactive world
    /// through a single.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    internal sealed class SingleCreate<T> : ISingleSource<T>
    {
        readonly Action<ISingleEmitter<T>> onSubscribe;

        public SingleCreate(Action<ISingleEmitter<T>> onSubscribe)
        {
            this.onSubscribe = onSubscribe;
        }

        public void Subscribe(ISingleObserver<T> observer)
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

        internal sealed class CreateDisposableEmitter : ISingleEmitter<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            IDisposable resource;

            public CreateDisposableEmitter(ISingleObserver<T> downstream)
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
