using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Repeatedly subscribes to the completable source after the
    /// previous subscription fails.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableRetry : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly long times;

        public CompletableRetry(ICompletableSource source, long times)
        {
            this.source = source;
            this.times = times;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new RepeatObserver(observer, source, times);
            observer.OnSubscribe(parent);
            parent.Drain();
        }

        sealed class RepeatObserver : CompletableRedoObserver
        {
            long times;

            public RepeatObserver(ICompletableObserver downstream, ICompletableSource source, long times) : base(downstream, source)
            {
                this.times = times;
            }

            public override void OnError(Exception error)
            {
                var t = times;

                if (t == 0)
                {
                    downstream.OnError(error);
                }
                else
                {
                    if (t != long.MaxValue)
                    {
                        times = t - 1;
                    }
                    Drain();
                }
            }
        }
    }

    /// <summary>
    /// Repeatedly subscribes to the completable source after the
    /// previous subscription fails and the predicate returns true.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableRetryPredicate : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly Func<Exception, long, bool> predicate;

        public CompletableRetryPredicate(ICompletableSource source, Func<Exception, long, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new RepeatObserver(observer, source, predicate);
            observer.OnSubscribe(parent);
            parent.Drain();
        }

        sealed class RepeatObserver : CompletableRedoObserver
        {
            readonly Func<Exception, long, bool> predicate;

            long times;

            public RepeatObserver(ICompletableObserver downstream, ICompletableSource source, Func<Exception, long, bool> predicate) : base(downstream, source)
            {
                this.predicate = predicate;
            }

            public override void OnError(Exception error)
            {
                var t = ++times;

                var repeat = false;

                try
                {
                    repeat = predicate(error, t);
                }
                catch (Exception ex)
                {
                    base.OnError(new AggregateException(error, ex));
                    return;
                }

                if (repeat)
                {
                    Drain();
                }
                else
                {
                    downstream.OnError(error);
                }
            }
        }
    }
}
