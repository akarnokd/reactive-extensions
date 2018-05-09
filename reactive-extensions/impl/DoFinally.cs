using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class DoFinally<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Action handler;

        public DoFinally(IObservable<T> source, Action handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new DoFinallyObserver(observer, handler);
            var d = source.Subscribe(parent);
            parent.OnSubscribe(d);
            return parent;
        }

        sealed class DoFinallyObserver : BaseObserver<T, T>
        {
            Action handler;

            internal DoFinallyObserver(IObserver<T> downstream, Action handler) : base(downstream)
            {
                Volatile.Write(ref this.handler, handler);
            }

            void HandleFinally()
            {
                var h = Volatile.Read(ref handler);
                if (h != null)
                {
                    Interlocked.Exchange(ref handler, null)?.Invoke();
                }
            }

            public override void OnCompleted()
            {
                try
                {
                    downstream.OnCompleted();
                }
                finally
                {
                    HandleFinally();
                }
            }

            public override void OnError(Exception error)
            {
                try
                {
                    downstream.OnError(error);
                }
                finally
                {
                    HandleFinally();
                }
            }

            public override void OnNext(T value)
            {
                downstream.OnNext(value);
            }

            public override void Dispose()
            {
                HandleFinally();
                base.Dispose();
            }
        }
    }
}
