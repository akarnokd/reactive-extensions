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

        /// <summary>
        /// Calls the specified <paramref name="handler"/> when an observer
        /// subscribes to the source observable.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect on various terminal cases.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoOnSubscribe<T>(source, handler);
        }

        /// <summary>
        /// Calls the specified <paramref name="handler"/> when the downstream
        /// disposes the sequence.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect when the downstream disposes.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoOnDispose<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoOnDispose<T>(source, handler);
        }

        /// <summary>
        /// Calls the specified <paramref name="handler"/> after the current
        /// OnNext item has been emitted to the downstream but before the
        /// next item or terminal signal.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect after each upstream item.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoAfterNext<T>(this IObservable<T> source, Action<T> handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoAfterNext<T>(source, handler);
        }

        /// <summary>
        /// Call the specified <paramref name="handler"/> after the upstream completed
        /// normally or with an error.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect the termination of.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoAfterTerminate<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoAfterTerminate<T>(source, handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> exactly once when the source
        /// completes normally, with an error or the downstream disposes the stream.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect on various terminal cases.</param>
        /// <param name="handler">The action to call when the upstream terminates or the downstream disposes.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoFinally<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoFinally<T>(source, handler);
        }
    }
}
