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

    /// <summary>
    /// Maps the upstream success item into a single source,
    /// subscribes to it and relays its success or terminal signals
    /// to the downstream.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The value type of the inner single source.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleFlatMapSingle<T, R> : ISingleSource<R>
    {
        readonly ISingleSource<T> source;

        readonly Func<T, ISingleSource<R>> mapper;

        public SingleFlatMapSingle(ISingleSource<T> source, Func<T, ISingleSource<R>> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(ISingleObserver<R> observer)
        {
            source.Subscribe(new FlatMapObserver(observer, mapper));
        }

        sealed class FlatMapObserver : SingleFlatMapObserver<T, R>
        {

            readonly Func<T, ISingleSource<R>> mapper;

            public FlatMapObserver(ISingleObserver<R> downstream, Func<T, ISingleSource<R>> mapper) : base(downstream)
            {
                this.mapper = mapper;
            }

            public override void OnSuccess(T item)
            {
                DisposableHelper.WeakDispose(ref upstream);

                var source = default(ISingleSource<R>);

                try
                {
                    source = RequireNonNullRef(mapper(item), "The mapper returned a null ISingleSource");
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

    /// <summary>
    /// Relays the events of single or single observer
    /// to a single observer.
    /// </summary>
    /// <typeparam name="R">The success element type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleFlatMapInnerObserver<R> : ISingleObserver<R>, IDisposable
    {
        readonly ISingleObserver<R> downstream;

        IDisposable upstream;

        public SingleFlatMapInnerObserver(ISingleObserver<R> downstream)
        {
            this.downstream = downstream;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnError(Exception error)
        {
            downstream.OnError(error);
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public void OnSuccess(R item)
        {
            downstream.OnSuccess(item);
        }
    }

    /// <summary>
    /// Can be subscribed to a single or single source and
    /// manages an internal observer that hosts a
    /// single observer.
    /// </summary>
    /// <typeparam name="T">The upstream success type.</typeparam>
    /// <typeparam name="R">The downstream success type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal abstract class SingleFlatMapObserver<T, R> : ISingleObserver<T>, IDisposable
    {

        protected readonly ISingleObserver<R> downstream;

        protected readonly SingleFlatMapInnerObserver<R> inner;

        protected IDisposable upstream;

        public SingleFlatMapObserver(ISingleObserver<R> downstream)
        {
            this.downstream = downstream;
            this.inner = new SingleFlatMapInnerObserver<R>(downstream);
        }

        public virtual void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            inner.Dispose();
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

        public abstract void OnSuccess(T item);
    }

}
