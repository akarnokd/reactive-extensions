using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Hides the identity of the upstream source and disposable.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class MaybeHide<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        public MaybeHide(IMaybeSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new HideObserver(observer));
        }

        sealed class HideObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IDisposable upstream;

            public HideObserver(IMaybeObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }
}
