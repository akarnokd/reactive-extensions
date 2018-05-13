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
    /// <remarks>Since 0.0.3</remarks>
    internal sealed class RetryPredicate<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Func<Exception, int, bool> predicate;

        public RetryPredicate(IObservable<T> source, Func<Exception, int, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new RetryPredicteObserver(observer, source, predicate);
            parent.Next();
            return parent;
        }

        sealed class RetryPredicteObserver : RedoObserver<T>
        {
            readonly Func<Exception, int, bool> predicate;

            int count;

            internal RetryPredicteObserver(IObserver<T> downstream, IObservable<T> source, Func<Exception, int, bool> predicate) : base(downstream, source)
            {
                this.predicate = predicate;
            }

            public override void OnCompleted()
            {
                downstream.OnCompleted();
                Dispose();
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
                    var d = Volatile.Read(ref upstream);
                    if (d != DisposableHelper.DISPOSED)
                    {
                        if (Interlocked.CompareExchange(ref upstream, null, d) == d)
                        {
                            d?.Dispose();
                            Next();
                        }
                    }
                }
                else
                {
                    downstream.OnError(error);
                    Dispose();
                }
            }
        }
    }
}
