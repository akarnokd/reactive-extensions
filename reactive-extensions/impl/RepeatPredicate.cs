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
    /// <remarks>Since 0.0.3</remarks>
    internal sealed class RepeatPredicate<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Func<bool> predicate;

        public RepeatPredicate(IObservable<T> source, Func<bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new RepeatPredicteObserver(observer, source, predicate);
            parent.Next();
            return parent;
        }

        sealed class RepeatPredicteObserver : RedoObserver<T>
        {
            readonly Func<bool> predicate;


            internal RepeatPredicteObserver(IObserver<T> downstream, IObservable<T> source, Func<bool> predicate) : base(downstream, source)
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
                    downstream.OnCompleted();
                    Dispose();
                }
            }

            public override void OnError(Exception error)
            {
                downstream.OnError(error);
                Dispose();
            }
        }
    }
}
