using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Bridges the callback-style world with the reactive world
    /// through a valueless completable.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    internal sealed class CompletableCreate : ICompletableSource
    {
        readonly Action<ICompletableEmitter> onSubscribe;

        public CompletableCreate(Action<ICompletableEmitter> onSubscribe)
        {
            this.onSubscribe = onSubscribe;
        }

        public void Subscribe(ICompletableObserver observer)
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

        internal sealed class CreateDisposableEmitter : ICompletableEmitter, IDisposable
        {
            readonly ICompletableObserver downstream;

            IDisposable resource;

            public CreateDisposableEmitter(ICompletableObserver downstream)
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

            public void SetResource(IDisposable d)
            {
                DisposableHelper.Set(ref resource, d);
            }
        }
    }
}
