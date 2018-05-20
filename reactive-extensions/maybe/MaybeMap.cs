using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the success value of the upstream maybe source
    /// into another value.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The result value type</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeMap<T, R> : IMaybeSource<R>
    {
        readonly IMaybeSource<T> source;

        readonly Func<T, R> mapper;

        public MaybeMap(IMaybeSource<T> source, Func<T, R> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(IMaybeObserver<R> observer)
        {
            source.Subscribe(new MapObserver(observer, mapper));
        }

        sealed class MapObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<R> downstream;

            readonly Func<T, R> mapper;

            IDisposable upstream;

            public MapObserver(IMaybeObserver<R> downstream, Func<T, R> mapper)
            {
                this.downstream = downstream;
                this.mapper = mapper;
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
                var v = default(R);

                try
                {
                    v = mapper(item);
                }
                catch (Exception ex)
                {
                    downstream.OnError(ex);
                    return;
                }

                downstream.OnSuccess(v);
            }
        }
    }
}
