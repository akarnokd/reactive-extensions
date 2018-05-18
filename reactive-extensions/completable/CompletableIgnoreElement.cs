using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Ingores the success element of the source maybe and completes
    /// the completable observer.
    /// </summary>
    /// <typeparam name="T">The success type of the source maybe.</typeparam>
    /// <since>0.0.9</since>
    internal sealed class CompletableIgnoreElementMaybe<T> : ICompletableSource
    {
        readonly IMaybeSource<T> source;

        public CompletableIgnoreElementMaybe(IMaybeSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new IgnoreMultiObserver<T>(observer));
        }
    }

    /// <summary>
    /// Ingores the success element of the source single and completes
    /// the completable observer.
    /// </summary>
    /// <typeparam name="T">The success type of the source maybe.</typeparam>
    /// <since>0.0.9</since>
    internal sealed class CompletableIgnoreElementSingle<T> : ICompletableSource
    {
        readonly ISingleSource<T> source;

        public CompletableIgnoreElementSingle(ISingleSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new IgnoreMultiObserver<T>(observer));
        }
    }

    /// <summary>
    /// A single- and maybe observer that turns the success
    /// signal into a completed signal.
    /// </summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <since>0.0.9</since>
    internal sealed class IgnoreMultiObserver<T> : IMaybeObserver<T>, ISingleObserver<T>, IDisposable
    {
        readonly ICompletableObserver downstream;

        IDisposable upstream;

        public IgnoreMultiObserver(ICompletableObserver downstream)
        {
            this.downstream = downstream;
        }

        public void Dispose()
        {
            upstream?.Dispose();
            upstream = null;
        }

        public void OnCompleted()
        {
            upstream = null;
            downstream.OnCompleted();
        }

        public void OnError(Exception error)
        {
            upstream = null;
            downstream.OnError(error);
        }

        public void OnSubscribe(IDisposable d)
        {
            upstream = d;
            downstream.OnSubscribe(this);
        }

        public void OnSuccess(T item)
        {
            upstream = null;
            downstream.OnCompleted();
        }
    }
}
