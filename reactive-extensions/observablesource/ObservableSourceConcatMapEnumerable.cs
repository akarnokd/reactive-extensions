using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the upstream items to <see cref="IEnumerable{T}"/>s and emits their items in order.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The result value type</typeparam>
    /// <remarks>Since 0.0.2</remarks>
    internal sealed class ObservableSourceConcatMapEnumerable<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, IEnumerable<R>> mapper;

        internal ObservableSourceConcatMapEnumerable(IObservableSource<T> source, Func<T, IEnumerable<R>> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            source.Subscribe(new ConcatMapEnumerableObserver(observer, mapper));
        }

        sealed class ConcatMapEnumerableObserver : BaseSignalObserver<T, R>
        {
            readonly Func<T, IEnumerable<R>> mapper;

            internal ConcatMapEnumerableObserver(ISignalObserver<R> downstream, Func<T, IEnumerable<R>> mapper) : base(downstream)
            {
                this.mapper = mapper;
            }

            public override void OnCompleted()
            {
                if (IsDisposed())
                {
                    return;
                }
                downstream.OnCompleted();
            }

            public override void OnError(Exception error)
            {
                if (IsDisposed())
                {
                    return;
                }
                downstream.OnError(error);
            }

            public override void OnNext(T value)
            {
                if (IsDisposed())
                {
                    return;
                }

                var enumerator = default(IEnumerator<R>);

                try
                {
                    var enumerable = mapper(value);
                    enumerator = enumerable.GetEnumerator();
                }
                catch (Exception ex)
                {
                    downstream.OnError(ex);
                    Dispose();
                    return;
                }

                for (; ; )
                {
                    if (IsDisposed())
                    {
                        enumerator.Dispose();
                        return;
                    }

                    var hasValue = false;
                    var v = default(R);

                    try
                    {
                        hasValue = enumerator.MoveNext();
                        if (hasValue)
                        {
                            v = enumerator.Current;
                        }
                    }
                    catch (Exception ex)
                    {
                        downstream.OnError(ex);
                        enumerator.Dispose();
                        Dispose();
                        return;
                    }

                    if (IsDisposed())
                    {
                        enumerator.Dispose();
                        return;
                    }

                    if (!hasValue)
                    {
                        enumerator.Dispose();
                        break;
                    }
                    downstream.OnNext(v);
                }
            }

        }
    }
}
