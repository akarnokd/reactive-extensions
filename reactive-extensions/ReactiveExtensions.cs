using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension methods for IObservables.
    /// </summary>
    public static class ReactiveExtensions
    {

        /// <summary>
        /// Perform a null-check on an argument and throw an ArgumentNullException.
        /// </summary>
        /// <typeparam name="X">Nullable classes only.</typeparam>
        /// <param name="reference">The target reference to check.</param>
        /// <param name="paramName">The name of the parameter in the original method</param>
        /// <exception cref="ArgumentNullException">If <paramref name="reference"/> is null.</exception>
        /// <remarks>Since 0.0.2</remarks>
        internal static void RequireNonNull<X>(X reference, string paramName) where X : class
        {
            if (reference == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Verify the <paramref name="value"/> is positive.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name of the parameter in the original method</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is non-positive.</exception>
        /// <remarks>Since 0.0.2</remarks>
        internal static void RequirePositive(int value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Positive value required: " + value);
            }
        }

        /// <summary>
        /// Verify the <paramref name="value"/> is positive.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name of the parameter in the original method</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is non-positive.</exception>
        internal static void RequirePositive(long value, string paramName)
        {
            if (value <= 0L)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Positive value required: " + value);
            }
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
            RequireNonNull(source, nameof(source));
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
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

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
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

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
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

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
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

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
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

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
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new DoFinally<T>(source, handler);
        }

        /// <summary>
        /// Wraps the given <paramref name="observer"/> so that concurrent
        /// calls to the returned observer's OnXXX methods are serialized.
        /// </summary>
        /// <typeparam name="T">The element type of the flow</typeparam>
        /// <param name="observer">The observer to wrap and serialize signals for.</param>
        /// <returns>The serialized observer instance.</returns>
        public static IObserver<T> ToSerialized<T>(this IObserver<T> observer)
        {
            RequireNonNull(observer, nameof(observer));

            if (observer is SerializedObserver<T> o)
            {
                return o;
            }

            return new SerializedObserver<T>(observer);
        }

        /// <summary>
        /// Wraps the given <paramref name="subject"/> so that concurrent
        /// calls to the returned subject's OnXXX methods are serialized.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The subject's output value type.</typeparam>
        /// <param name="subject">The subject to wrap and serialize signals for.</param>
        /// <returns>The serialized observer instance.</returns>
        public static ISubject<T, R> ToSerialized<T, R>(this ISubject<T, R> subject)
        {
            RequireNonNull(subject, nameof(subject));

            if (subject is SerializedSubject<T, R> o)
            {
                return o;
            }

            return new SerializedSubject<T, R>(subject);
        }

        /// <summary>
        /// Wraps the given <paramref name="subject"/> so that concurrent
        /// calls to the returned subject's OnXXX methods are serialized.
        /// </summary>
        /// <typeparam name="T">The value type of the flow.</typeparam>
        /// <param name="subject">The subject to wrap and serialize signals for.</param>
        /// <returns>The serialized observer instance.</returns>
        public static ISubject<T> ToSerialized<T>(this ISubject<T> subject)
        {
            RequireNonNull(subject, nameof(subject));

            if (subject is SerializedSubject<T> || subject is SerializedSubject<T, T>)
            {
                return subject;
            }

            return new SerializedSubject<T>(subject);
        }

        /// <summary>
        /// Maps the upstream items into observables, runs some or all of them at once, emits items from one
        /// of the observables until it completes, then switches to the next observable.
        /// </summary>
        /// <typeparam name="T">The value type of the upstream.</typeparam>
        /// <typeparam name="R">The output value type.</typeparam>
        /// <param name="source">The source observable to be mapper and concatenated eagerly.</param>
        /// <param name="mapper">The function that returns an observable for an upstream item.</param>
        /// <param name="maxConcurrency">The maximum number of observables to run at a time.</param>
        /// <param name="capacityHint">The number of items expected from each observable.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<R> ConcatMapEager<T, R>(this IObservable<T> source, Func<T, IObservable<R>> mapper, int maxConcurrency = int.MaxValue, int capacityHint = 128)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequirePositive(maxConcurrency, nameof(maxConcurrency));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ConcatMapEager<T, R>(source, mapper, maxConcurrency, capacityHint);
        }

        /// <summary>
        /// Maps the upstream items to enumerables and emits their items in order.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The result value type</typeparam>
        /// <param name="source">The source observable.</param>
        /// <param name="mapper">The function that turns an upstream item into an enumerable sequence.</param>
        /// <returns>The new observable instance</returns>
        /// <remarks>Since 0.0.2</remarks>
        public static IObservable<R> ConcatMap<T, R>(this IObservable<T> source, Func<T, IEnumerable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new ConcatMapEnumerable<T, R>(source, mapper);
        }

        /// <summary>
        /// Concatenates the sequence of inner observables into one observable sequence
        /// while preserving their order.
        /// </summary>
        /// <typeparam name="T">The result and inner observable element type.</typeparam>
        /// <param name="sources">The sequence of inner observable sequences</param>
        /// <returns>The new observable instance</returns>
        /// <remarks>Since 0.0.2</remarks>
        public static IObservable<T> ConcatMany<T>(this IObservable<IObservable<T>> sources)
        {
            RequireNonNull(sources, nameof(sources));

            return new ConcatMany<T>(sources);
        }

        /// <summary>
        /// Merge some or all observables provided by the outer observable.
        /// </summary>
        /// <typeparam name="T">The result and inner observable element type.</typeparam>
        /// <param name="sources">The sequence of inner observable sequences</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <param name="maxConcurrency">The maximum number of sources to run at once.</param>
        /// <param name="capacityHint">The expected number of items from the inner sources that will have to wait in a buffer.</param>
        /// <returns>The new observable instance</returns>
        /// <remarks>Since 0.0.2</remarks>
        public static IObservable<T> MergeMany<T>(this IObservable<IObservable<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue, int capacityHint = 128)
        {
            RequireNonNull(sources, nameof(sources));
            RequirePositive(maxConcurrency, nameof(maxConcurrency));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new MergeMany<T>(sources, delayErrors, maxConcurrency, capacityHint);
        }
    }
}
