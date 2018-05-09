using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class DoOnDispose<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Action handler;

        internal DoOnDispose(IObservable<T> source, Action handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return new DoOnDisposeDisposable(source.Subscribe(observer), handler);
        }

        internal sealed class DoOnDisposeDisposable : IDisposable
        {
            IDisposable disposable;

            Action handler;

            public DoOnDisposeDisposable(IDisposable disposable, Action handler)
            {
                this.disposable = disposable;
                Volatile.Write(ref this.handler, handler);
            }

            public void Dispose()
            {
                try
                {
                    Interlocked.Exchange(ref handler, null)?.Invoke();
                }
                finally
                {
                    Interlocked.Exchange(ref disposable, null)?.Dispose();
                }
            }
        }
    }
}
