using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the success value of the upstream
    /// single source onto an enumerable sequence
    /// and emits the items of this sequence.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="R">The element type of the enumerable sequence.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleFlatMapEnumerable<T, R> : IObservable<R>
    {
        readonly ISingleSource<T> source;

        readonly Func<T, IEnumerable<R>> mapper;

        public SingleFlatMapEnumerable(ISingleSource<T> source, Func<T, IEnumerable<R>> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            var parent = new FlatMapEnumerableObserver(observer, mapper);
            source.Subscribe(parent);
            return parent;
        }

        sealed class FlatMapEnumerableObserver : ISingleObserver<T>, IDisposable
        {
            readonly IObserver<R> downstream;

            readonly Func<T, IEnumerable<R>> mapper;

            IDisposable upstream;

            public FlatMapEnumerableObserver(IObserver<R> downstream, Func<T, IEnumerable<R>> mapper)
            {
                this.downstream = downstream;
                this.mapper = mapper;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnError(Exception error)
            {
                DisposableHelper.WeakDispose(ref upstream);
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnSuccess(T item)
            {
                var en = default(IEnumerator<R>);

                try
                {
                    en = RequireNonNullRef(mapper(item).GetEnumerator(), "The GetEnumerator returned a null IEnumerator");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return;
                }

                for (; ; )
                {
                    if (DisposableHelper.IsDisposed(ref upstream))
                    {
                        en.Dispose();
                        break;
                    }

                    var v = default(R);
                    var b = false;
                    try
                    {
                        b = en.MoveNext();
                        if (b)
                        {
                            v = en.Current;
                        }
                    }
                    catch (Exception ex)
                    {
                        en.Dispose();
                        OnError(ex);
                        break;
                    }

                    if (b)
                    {
                        downstream.OnNext(v);
                    }
                    else
                    {
                        downstream.OnCompleted();
                        break;
                    }
                }
            }
        }
    }
}
