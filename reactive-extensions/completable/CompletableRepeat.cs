using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Repeatedly subscribes to the completable source after the
    /// previous subscription completes.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableRepeat : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly long times;

        public CompletableRepeat(ICompletableSource source, long times)
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

            public override void OnCompleted()
            {
                var t = times;

                if (t == 0)
                {
                    downstream.OnCompleted();
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
    /// previous subscription completes and the predicate returns true.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableRepeatPredicate : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly Func<long, bool> predicate;

        public CompletableRepeatPredicate(ICompletableSource source, Func<long, bool> predicate)
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
            readonly Func<long, bool> predicate;

            long times;

            public RepeatObserver(ICompletableObserver downstream, ICompletableSource source, Func<long, bool> predicate) : base(downstream, source)
            {
                this.predicate = predicate;
            }

            public override void OnCompleted()
            {
                var t = ++times;

                var repeat = false;

                try
                {
                    repeat = predicate(t);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return;
                }

                if (repeat)
                {
                    Drain();
                }
                else
                {
                    downstream.OnCompleted();
                }
            }
        }
    }
}
