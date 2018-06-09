using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Repeatedly re-subscribes to the source observable if the predicate
    /// function returns true upon the completion of the previous
    /// subscription.
    /// </summary>
    /// <typeparam name="T">The value type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal sealed class ObservableSourceRepeatPredicate<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<bool> predicate;

        public ObservableSourceRepeatPredicate(IObservableSource<T> source, Func<bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new RepeatPredicteObserver(observer, source, predicate);
            observer.OnSubscribe(parent);
            parent.Next();
        }

        sealed class RepeatPredicteObserver : RedoSignalObserver<T>
        {
            readonly Func<bool> predicate;


            internal RepeatPredicteObserver(ISignalObserver<T> downstream, IObservableSource<T> source, Func<bool> predicate) : base(downstream, source)
            {
                this.predicate = predicate;
            }

            public override void OnCompleted()
            {
                var again = false;

                try
                {
                    again = predicate();
                }
                catch (Exception ex)
                {
                    downstream.OnError(ex);
                    Dispose();
                    return;
                }
                if (again)
                {
                    Next();
                }
                else
                {
                    downstream.OnCompleted();
                }
            }

            public override void OnError(Exception error)
            {
                downstream.OnError(error);
            }
        }
    }

    /// <summary>
    /// Repeatedly re-subscribes to the source observable if the predicate
    /// function returns true upon the completion of the previous
    /// subscription.
    /// </summary>
    /// <typeparam name="T">The value type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal sealed class ObservableSourceRepeatPredicateCount<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<long, bool> predicate;

        public ObservableSourceRepeatPredicateCount(IObservableSource<T> source, Func<long, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new RepeatPredicteObserver(observer, source, predicate);
            observer.OnSubscribe(parent);
            parent.Next();
        }

        sealed class RepeatPredicteObserver : RedoSignalObserver<T>
        {
            readonly Func<long, bool> predicate;

            long count;

            internal RepeatPredicteObserver(ISignalObserver<T> downstream, IObservableSource<T> source, Func<long, bool> predicate) : base(downstream, source)
            {
                this.predicate = predicate;
            }

            public override void OnCompleted()
            {
                var again = false;

                try
                {
                    again = predicate(++count);
                }
                catch (Exception ex)
                {
                    downstream.OnError(ex);
                    Dispose();
                    return;
                }
                if (again)
                {
                    Next();
                }
                else
                {
                    downstream.OnCompleted();
                }
            }

            public override void OnError(Exception error)
            {
                downstream.OnError(error);
            }
        }
    }


    /// <summary>
    /// Repeatedly re-subscribes to the source at most the given number
    /// of times.
    /// </summary>
    /// <typeparam name="T">The value type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal sealed class ObservableSourceRepeatCount<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly long times;

        public ObservableSourceRepeatCount(IObservableSource<T> source, long times)
        {
            this.source = source;
            this.times = times;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            if (times == 0)
            {
                source.Subscribe(new ObservableSourceTake<T>.TakeObserver(observer, 0));
                return;
            }
            var parent = new RepeatPredicteObserver(observer, source, times);
            observer.OnSubscribe(parent);
            parent.Next();
        }

        sealed class RepeatPredicteObserver : RedoSignalObserver<T>
        {
            readonly long times;

            long count;

            internal RepeatPredicteObserver(ISignalObserver<T> downstream, IObservableSource<T> source, long times) : base(downstream, source)
            {
                this.times = times;
            }

            public override void OnCompleted()
            {
                var again = ++count < times;

                if (again)
                {
                    Next();
                }
                else
                {
                    downstream.OnCompleted();
                }
            }

            public override void OnError(Exception error)
            {
                downstream.OnError(error);
            }
        }
    }
}
