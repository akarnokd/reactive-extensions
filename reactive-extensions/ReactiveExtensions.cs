using System;
using System.Reactive;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension methods for IObservables.
    /// </summary>
    public static class ReactiveExtensions
    {
        /// <summary>
        /// Emits the upstream item on the specified <paramref name="scheduler"/>
        /// and optionally delays an upstream error.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream source observable.</param>
        /// <param name="scheduler">The IScheduler to use.</param>
        /// <param name="delayError">If true, an upstream error is emitted last. If false, an error may cut ahead of other values.</param>
        /// <returns>The new IObservable instance.</returns>
        public static IObservable<T> ObserveOn<T>(
            this IObservable<T> source, 
            IScheduler scheduler, 
            bool delayError)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }

            return new ObserveOn<T>(source, scheduler, delayError);
        }

        public static IObservable<R> ConcatMap<T, R>(
            this IObservable<T> source,
            Func<T, IObservable<R>> mapper
            )
        {
            return null;
        }

        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> observable.
        /// </summary>
        /// <typeparam name="T">The value type of the source observable.</typeparam>
        /// <param name="source">The source observable to test.</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this IObservable<T> source)
        {
            var to = new TestObserver<T>();
            to.OnSubscribe(source.Subscribe(to));
            return to;
        }
    }
}
