using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Tests the upstream's success value via a predicate
    /// and relays it if the predicate returns false,
    /// completes the downstream otherwise.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeFilter<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly Func<T, bool> predicate;

        public MaybeFilter(IMaybeSource<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new FilterObserver<T>(observer, predicate));
        }
    }

    /// <summary>
    /// Tests the upstream success value with a predicate
    /// and relays it if it passes this predicate, calls
    /// OnCompleted otherwise.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class FilterObserver<T> : ISingleObserver<T>, IMaybeObserver<T>, IDisposable
    {
        readonly IMaybeObserver<T> downstream;

        readonly Func<T, bool> predicate;

        IDisposable upstream;

        public FilterObserver(IMaybeObserver<T> downstream, Func<T, bool> predicate)
        {
            this.downstream = downstream;
            this.predicate = predicate;
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
            var v = false;

            try
            {
                v = predicate(item);
            }
            catch (Exception ex)
            {
                downstream.OnError(ex);
                return;
            }

            if (v)
            {
                downstream.OnSuccess(item);
            }
            else
            {
                downstream.OnCompleted();
            }
        }
    }

}
