using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Hides the identity of the upstream source and disposable.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class SingleHide<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        public SingleHide(ISingleSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new HideObserver(observer));
        }

        sealed class HideObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            IDisposable upstream;

            public HideObserver(ISingleObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
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
