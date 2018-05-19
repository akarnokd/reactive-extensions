using System;
using System.Collections.Generic;
using System.Text;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the success value of the upstream single source
    /// into a completable source and signals its terminal
    /// events to the downstream.
    /// </summary>
    /// <typeparam name="T">The element type of the single source.</typeparam>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableFlatMapSingle<T> : ICompletableSource
    {
        readonly ISingleSource<T> source;

        readonly Func<T, ICompletableSource> mapper;

        public CompletableFlatMapSingle(ISingleSource<T> source, Func<T, ICompletableSource> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new CompletableFlatMapSingleObserver<T>(observer, mapper);
            observer.OnSubscribe(parent);

            source.Subscribe(parent);
        }
    }

    /// <summary>
    /// Consumes a maybe or single source and turns the
    /// success value into a completable source to subscribe to.
    /// </summary>
    /// <typeparam name="T">The type of the success value</typeparam>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableFlatMapSingleObserver<T> : ISingleObserver<T>, IMaybeObserver<T>, ICompletableObserver, IDisposable
    {
        readonly ICompletableObserver downstream;

        readonly Func<T, ICompletableSource> mapper;

        IDisposable upstream;

        public CompletableFlatMapSingleObserver(ICompletableObserver downstream, Func<T, ICompletableSource> mapper)
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
            downstream.OnCompleted();
        }

        public void OnError(Exception error)
        {
            downstream.OnError(error);
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.Replace(ref upstream, d);
        }

        public void OnSuccess(T item)
        {
            var c = default(ICompletableSource);

            try
            {
                c = RequireNonNullRef(mapper(item), "The mapper returned a null ICompletableSource");
            }
            catch (Exception ex)
            {
                downstream.OnError(ex);
                return;
            }

            c.Subscribe(this);
        }

    }

    /// <summary>
    /// Maps the success value of the upstream maybe source
    /// into a completable source and signals its terminal
    /// events to the downstream.
    /// </summary>
    /// <typeparam name="T">The element type of the maybe source.</typeparam>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableFlatMapMaybe<T> : ICompletableSource
    {
        readonly IMaybeSource<T> source;

        readonly Func<T, ICompletableSource> mapper;

        public CompletableFlatMapMaybe(IMaybeSource<T> source, Func<T, ICompletableSource> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new CompletableFlatMapSingleObserver<T>(observer, mapper);
            observer.OnSubscribe(parent);

            source.Subscribe(parent);
        }
    }
}
