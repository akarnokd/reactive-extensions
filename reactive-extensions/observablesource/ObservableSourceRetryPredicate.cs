using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Repeatedly re-subscribes to the source observable if the predicate
    /// function returns true upon the failure of the previous
    /// subscription.
    /// </summary>
    /// <typeparam name="T">The value type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal sealed class ObservableSourceRetryPredicate<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<Exception, long, bool> predicate;

        public ObservableSourceRetryPredicate(IObservableSource<T> source, Func<Exception, long, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new RetryPredicteObserver(observer, source, predicate);
            observer.OnSubscribe(parent);
            parent.Next();
        }

        sealed class RetryPredicteObserver : RedoSignalObserver<T>
        {
            readonly Func<Exception, long, bool> predicate;

            long count;

            internal RetryPredicteObserver(ISignalObserver<T> downstream, IObservableSource<T> source, Func<Exception, long, bool> predicate) : base(downstream, source)
            {
                this.predicate = predicate;
            }

            public override void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public override void OnError(Exception error)
            {
                var again = false;

                try
                {
                    again = predicate(error, ++count);
                }
                catch (Exception ex)
                {
                    downstream.OnError(new AggregateException(error, ex));
                    Dispose();
                    return;
                }
                if (again)
                {
                    Next();
                }
                else
                {
                    downstream.OnError(error);
                }
            }
        }
    }

    /// <summary>
    /// Repeatedly re-subscribes to the source observable source
    /// at most the specified number of times if the source failed.
    /// </summary>
    /// <typeparam name="T">The value type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal sealed class ObservableSourceRetryCount<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly long times;

        public ObservableSourceRetryCount(IObservableSource<T> source, long times)
        {
            this.source = source;
            this.times = times;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new RetryPredicteObserver(observer, source, times);
            observer.OnSubscribe(parent);
            parent.Next();
        }

        sealed class RetryPredicteObserver : RedoSignalObserver<T>
        {
            readonly long times;

            int count;

            internal RetryPredicteObserver(ISignalObserver<T> downstream, IObservableSource<T> source, long times) : base(downstream, source)
            {
                this.times = times;
            }

            public override void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public override void OnError(Exception error)
            {
                var again = count++ < times;

                if (again)
                {
                    Next();
                }
                else
                {
                    downstream.OnError(error);
                }
            }
        }
    }
}
