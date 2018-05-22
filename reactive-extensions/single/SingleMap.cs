using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the success value of the upstream single source
    /// into another value.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The result value type</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleMap<T, R> : ISingleSource<R>
    {
        readonly ISingleSource<T> source;

        readonly Func<T, R> mapper;

        public SingleMap(ISingleSource<T> source, Func<T, R> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(ISingleObserver<R> observer)
        {
            source.Subscribe(new MapObserver(observer, mapper));
        }

        sealed class MapObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<R> downstream;

            readonly Func<T, R> mapper;

            IDisposable upstream;

            public MapObserver(ISingleObserver<R> downstream, Func<T, R> mapper)
            {
                this.downstream = downstream;
                this.mapper = mapper;
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
