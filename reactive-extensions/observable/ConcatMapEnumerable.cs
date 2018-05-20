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
    internal sealed class ConcatMapEnumerable<T, R> : BaseObservable<T, R>
    {
        readonly Func<T, IEnumerable<R>> mapper;

        internal ConcatMapEnumerable(IObservable<T> source, Func<T, IEnumerable<R>> mapper) : base(source)
        {
            this.mapper = mapper;
        }

        protected override BaseObserver<T, R> CreateObserver(IObserver<R> observer)
        {
            return new ConcatMapEnumerableObserver(observer, mapper);
        }

        sealed class ConcatMapEnumerableObserver : BaseObserver<T, R>
        {
            readonly Func<T, IEnumerable<R>> mapper;

            internal ConcatMapEnumerableObserver(IObserver<R> downstream, Func<T, IEnumerable<R>> mapper) : base(downstream)
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
