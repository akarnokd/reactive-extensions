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
        /// Concatenates a sequence of observables eagerly by running some
        /// or all of them at once and emitting their items in order.
        /// </summary>
        /// <typeparam name="T">The value type of the inner observables.</typeparam>
        /// <param name="sources">The sequence of observables to concatenate eagerly.</param>
        /// <param name="maxConcurrency">The maximum number of observables to run at a time.</param>
        /// <param name="capacityHint">The number of items expected from each observable.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> ConcatEager<T>(this IObservable<IObservable<T>> sources, int maxConcurrency = int.MaxValue, int capacityHint = 128)
        {
            return sources.ConcatMapEager(v => v, maxConcurrency, capacityHint);
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

        /// <summary>
        /// Collects upstream items into a per-observer collection object created
        /// via a function and added via a collector action and emits this collection
        /// when the upstream completes.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="C">The type of the collection.</typeparam>
        /// <param name="source">The source sequence to collect up.</param>
        /// <param name="collectionSupplier">The function creating the collection per-observer.</param>
        /// <param name="collector">The action that receives the collection and the current upstream item.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.3</remarks>
        public static IObservable<C> Collect<T, C>(this IObservable<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(collectionSupplier, nameof(collectionSupplier));
            RequireNonNull(collector, nameof(collector));

            return new Collect<T, C>(source, collectionSupplier, collector);
        }

        /// <summary>
        /// Applies a function to the source at assembly-time and returns the
        /// observable returned by this function.
        /// This allows creating reusable set of operators to be applied on observables.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The downstream value type</typeparam>
        /// <param name="source">The upstream sequence.</param>
        /// <param name="composer">The function called immediately on <paramref name="source"/>
        /// and should return another observable.</param>
        /// <returns>The observable returned by the <paramref name="composer"/> function.</returns>
        public static IObservable<R> Compose<T, R>(this IObservable<T> source, Func<IObservable<T>, IObservable<R>> composer)
        {
            return composer(source);
        }

        /// <summary>
        /// Repeatedly re-subscribes to the source observable if the predicate
        /// function returns true upon the completion of the previous
        /// subscription.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to repeat.</param>
        /// <param name="predicate">Function to determine whether to repeat the <paramref name="source"/> or not.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.3</remarks>
        public static IObservable<T> Repeat<T>(this IObservable<T> source, Func<bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new RepeatPredicate<T>(source, predicate);
        }

        /// <summary>
        /// Repeatedly re-subscribes to the source observable if the predicate
        /// function returns true upon the failure of the previous
        /// subscription.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to repeat.</param>
        /// <param name="predicate">Function to determine whether to retry the <paramref name="source"/> or not,
        /// given the last Exception and the run count so far.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.3</remarks>
        public static IObservable<T> Retry<T>(this IObservable<T> source, Func<Exception, int, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new RetryPredicate<T>(source, predicate);
        }

        /// <summary>
        /// Checks a predicate after an item has been emitted and completes
        /// the sequence if it returns true.
        /// </summary>
        /// <param name="source">The upstream observable limit conditionally.</param>
        /// <param name="stopPredicate">The function called with the current item after it
        /// has been emitted to the downstream and should return true to stop and dispose
        /// the upstream and complete the downstream.</param>
        /// <typeparam name="T">The element type of the sequence</typeparam>
        /// <remarks>Since 0.0.3</remarks>
        public static IObservable<T> TakeUntil<T>(this IObservable<T> source, Func<T, bool> stopPredicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(stopPredicate, nameof(stopPredicate));

            return new TakeUntilPredicate<T>(source, stopPredicate);
        }

        /// <summary>
        /// Switches to the fallback observables if the main source
        /// or a previous fallback is empty.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The main source sequence.</param>
        /// <param name="fallbacks">The fallback sequences if the <paramref name="source"/>
        /// turns out to be empty. Should have at least one observable.</param>
        /// <remarks>Since 0.0.3</remarks>
        public static IObservable<T> SwitchIfEmpty<T>(this IObservable<T> source, params IObservable<T>[] fallbacks)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(fallbacks, nameof(fallbacks));
            RequirePositive(fallbacks.Length, nameof(fallbacks) + ".Length");

            return new SwitchIfEmpty<T>(source, fallbacks);
        }

        /// <summary>
        /// Caches all upstream events and relays/replays it to current or
        /// late observers.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source"></param>
        /// <param name="capacityHint">The items are stored internally in a linked-array structure for 
        /// which this is the capacity hint for sizing those arrays: a tradeoff between memory usage and
        /// locality of memory.</param>
        /// <param name="cancel">Called with the disposable when the source is subscribed on the first observer.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<T> Cache<T>(this IObservable<T> source, int capacityHint = 16, Action<IDisposable> cancel = null)
        {
            RequireNonNull(source, nameof(source));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new Cache<T>(source, cancel, capacityHint);
        }

        /// <summary>
        /// Switches to a new observable mapped via a function in response to
        /// an new upstream item, disposing the previous active observable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="source">The source sequence to turn into the inner observables.</param>
        /// <param name="mapper">The function that given an upstream item returns the inner observable to continue
        /// relaying events from.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all observables have terminated or got disposed.</param>
        /// <param name="capacityHint">The expected number of items to buffer per inner observable</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<R> SwitchMap<T, R>(this IObservable<T> source, Func<T, IObservable<R>> mapper, bool delayErrors = false, int capacityHint = 128)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new SwitchMap<T, R>(source, mapper, delayErrors, capacityHint);
        }

        /// <summary>
        /// Switches to a new inner observable when the upstream emits it,
        /// disposing the previous active observable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources">The sequence of observables to switch between.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all observables have terminated or got disposed.</param>
        /// <param name="capacityHint">The expected number of items to buffer per inner observable</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<T> SwitchMany<T>(this IObservable<IObservable<T>> sources, bool delayErrors = false, int capacityHint = 128)
        {
            return SwitchMap(sources, v => v, delayErrors, capacityHint);
        }

        /// <summary>
        /// Combines the latest values of multiple alternate observables with 
        /// the value of the main observable sequence through a function.
        /// </summary>
        /// <typeparam name="T">The element type of the main source.</typeparam>
        /// <typeparam name="U">The common type of the alternate observables.</typeparam>
        /// <typeparam name="R">The result type of the flow.</typeparam>
        /// <param name="source">The main source observable of items.</param>
        /// <param name="mapper">The function that takes the upstream item and the
        /// latest values from each <paramref name="others"/> observable if any.
        /// if not all other observable has a latest value, the mapper is not invoked.</param>
        /// <param name="others">The params array of alternate observables to use the latest values of.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<R> WithLatestFrom<T, U, R>(
            this IObservable<T> source,
            Func<T, U[], R> mapper,
            params IObservable<U>[] others)
        {
            return WithLatestFrom(source, mapper, false, false, others);
        }

        /// <summary>
        /// Combines the latest values of multiple alternate observables with 
        /// the value of the main observable sequence through a function.
        /// </summary>
        /// <typeparam name="T">The element type of the main source.</typeparam>
        /// <typeparam name="U">The common type of the alternate observables.</typeparam>
        /// <typeparam name="R">The result type of the flow.</typeparam>
        /// <param name="source">The main source observable of items.</param>
        /// <param name="mapper">The function that takes the upstream item and the
        /// latest values from each <paramref name="others"/> observable if any.
        /// if not all other observable has a latest value, the mapper is not invoked.</param>
        /// <param name="delayErrors">If true, errors from the <paramref name="others"/> will be delayed until the main sequence terminates.</param>
        /// <param name="others">The params array of alternate observables to use the latest values of.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<R> WithLatestFrom<T, U, R>(
            this IObservable<T> source,
            Func<T, U[], R> mapper,
            bool delayErrors,
            params IObservable<U>[] others)
        {
            return WithLatestFrom(source, mapper, delayErrors, false, others);
        }

        /// <summary>
        /// Combines the latest values of multiple alternate observables with 
        /// the value of the main observable sequence through a function.
        /// </summary>
        /// <typeparam name="T">The element type of the main source.</typeparam>
        /// <typeparam name="U">The common type of the alternate observables.</typeparam>
        /// <typeparam name="R">The result type of the flow.</typeparam>
        /// <param name="source">The main source observable of items.</param>
        /// <param name="mapper">The function that takes the upstream item and the
        /// latest values from each <paramref name="others"/> observable if any.
        /// if not all other observable has a latest value, the mapper is not invoked.</param>
        /// <param name="delayErrors">If true, errors from the <paramref name="others"/> will be delayed until the main sequence terminates.</param>
        /// <param name="sourceFirst">If true, the <paramref name="source"/> is subscribed first,
        /// if false, the <paramref name="others"/> are subscribed to first.</param>
        /// <param name="others">The params array of alternate observables to use the latest values of.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<R> WithLatestFrom<T, U, R>(
            this IObservable<T> source, 
            Func<T, U[], R> mapper, 
            bool delayErrors, 
            bool sourceFirst,
            params IObservable<U>[] others)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequireNonNull(others, nameof(others));
            RequirePositive(others.Length, nameof(others) + ".Length");

            return new WithLatestFrom<T, U, R>(source, others, mapper, delayErrors, sourceFirst);
        }

        /// <summary>
        /// Retries (resubscribes to) the source observable after a failure and when the observable
        /// returned by a handler produces an arbitrary item.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="U">The arbitrary element type signaled by the handler observable.</typeparam>
        /// <param name="source">Observable sequence to repeat until it successfully terminates.</param>
        /// <param name="handler">The function that is called for each observer and takes an observable sequence of
        /// errors. It should return an observable of arbitrary items that should signal that arbitrary item in
        /// response to receiving the failure Exception from the source observable. If this observable signals
        /// a terminal event, the sequence is terminated with that signal instead.</param>
        /// <returns>An observable sequence producing the elements of the given sequence repeatedly until it terminates successfully.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is null.</exception>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<T> RetryWhen<T, U>(this IObservable<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new RetryWhen<T, U>(source, handler);
        }

        /// <summary>
        /// Repeats (resubscribes to) the source observable after a completion and when the observable
        /// returned by a handler produces an arbitrary item.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="U">The arbitrary element type signaled by the handler observable.</typeparam>
        /// <param name="source">Observable sequence to repeat while it successfully terminates.</param>
        /// <param name="handler">The function that is called for each observer and takes an observable sequence of
        /// errors. It should return an observable of arbitrary items that should signal that arbitrary item in
        /// response to receiving the completion signal from the source observable. If this observable signals
        /// a terminal event, the sequence is terminated with that signal instead.</param>
        /// <returns>An observable sequence producing the elements of the given sequence repeatedly while it terminates successfully.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is null.</exception>
        /// <remarks>Since 0.0.4</remarks>
        public static IObservable<T> RepeatWhen<T, U>(this IObservable<T> source, Func<IObservable<object>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new RepeatWhen<T, U>(source, handler);
        }

    }
}
