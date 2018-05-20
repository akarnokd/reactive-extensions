using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the success value of the upstream
    /// maybe source onto an observable sequence
    /// and emits the items of this sequence.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="R">The element type of the observable sequence.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeFlatMapObservable<T, R> : IObservable<R>
    {
        readonly IMaybeSource<T> source;

        readonly Func<T, IObservable<R>> mapper;

        public MaybeFlatMapObservable(IMaybeSource<T> source, Func<T, IObservable<R>> mapper)
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

        sealed class FlatMapEnumerableObserver : IMaybeObserver<T>, IObserver<R>, IDisposable
        {
            readonly IObserver<R> downstream;

            readonly Func<T, IObservable<R>> mapper;

            IDisposable upstream;

            public FlatMapEnumerableObserver(IObserver<R> downstream, Func<T, IObservable<R>> mapper)
            {
                this.downstream = downstream;
                this.mapper = mapper;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                DisposableHelper.WeakDispose(ref upstream);
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                DisposableHelper.WeakDispose(ref upstream);
                downstream.OnError(error);
            }

            public void OnNext(R item)
            {
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnSuccess(T item)
            {
                var src = default(IObservable<R>);

                try
                {
                    src = RequireNonNullRef(mapper(item), "The mapper returned a null IObservable");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return;
                }

                var d = src.Subscribe(this);
                DisposableHelper.Replace(ref upstream, d);
            }
        }
    }
}
