using System;
using System.Collections.Generic;
using System.Text;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the upstream success item into a maybe source,
    /// subscribes to it and relays its success or terminal signals
    /// to the downstream.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The value type of the inner maybe source.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleFlatMapMaybe<T, R> : IMaybeSource<R>
    {
        readonly ISingleSource<T> source;

        readonly Func<T, IMaybeSource<R>> mapper;

        public SingleFlatMapMaybe(ISingleSource<T> source, Func<T, IMaybeSource<R>> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(IMaybeObserver<R> observer)
        {
            source.Subscribe(new FlatMapObserver(observer, mapper));
        }

        sealed class FlatMapObserver : MaybeFlatMapObserver<T, R>
        {

            readonly Func<T, IMaybeSource<R>> mapper;

            public FlatMapObserver(IMaybeObserver<R> downstream, Func<T, IMaybeSource<R>> mapper) : base(downstream)
            {
                this.mapper = mapper;
            }

            public override void OnSuccess(T item)
            {
                DisposableHelper.WeakDispose(ref upstream);

                var source = default(IMaybeSource<R>);

                try
                {
                    source = RequireNonNullRef(mapper(item), "The mapper returned a null IMaybeSource");
                }
                catch (Exception ex)
                {
                    downstream.OnError(ex);
                    return;
                }

                source.Subscribe(inner);
            }
        }
    }

}
