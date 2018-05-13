using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Checks a predicate after an item has been emitted and completes
    /// the sequence if it returns false.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence</typeparam>
    /// <remarks>Since 0.0.3</remarks>
    internal sealed class TakeUntilPredicate<T> : BaseObservable<T, T>
    {
        readonly Func<T, bool> stopPredicate;

        internal TakeUntilPredicate(IObservable<T> source, Func<T, bool> stopPredicate) : base(source)
        {
            this.stopPredicate = stopPredicate;
        }

        protected override BaseObserver<T, T> CreateObserver(IObserver<T> observer)
        {
            return new TakeUntilObserver(observer, stopPredicate);
        }

        sealed class TakeUntilObserver : BaseObserver<T, T>
        {
            readonly Func<T, bool> stopPredicate;

            bool done;

            internal TakeUntilObserver(IObserver<T> downstream, Func<T, bool> stopPredicate) : base(downstream)
            {
                this.stopPredicate = stopPredicate;
            }

            public override void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                downstream.OnCompleted();
                Dispose();
            }

            public override void OnError(Exception error)
            {
                if (done)
                {
                    return;
                }
                downstream.OnError(error);
                Dispose();
            }

            public override void OnNext(T value)
            {
                if (done)
                {
                    return;
                }

                downstream.OnNext(value);

                var stop = false;
                try
                {
                    stop = stopPredicate(value);
                }
                catch (Exception ex)
                {
                    done = true;
                    downstream.OnError(ex);
                    Dispose();
                    return;
                }

                if (stop)
                {
                    done = true;
                    downstream.OnCompleted();
                    Dispose();
                }
            }
        }
    }
}
