using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension and static factory methods for supporting
    /// <see cref="IObservableSource{T}"/>-based flows.
    /// </summary>
    /// <remarks>Since 0.0.17</remarks>
    public static class ObservableSource
    {
        // --------------------------------------------------------------
        // Factory methods
        // --------------------------------------------------------------

        /// <summary>
        /// Signals a given single constant value and completes.
        /// </summary>
        /// <typeparam name="T">The value type to be emitted.</typeparam>
        /// <param name="item">The value to emit to each individual observer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> Just<T>(T item)
        {
            return new ObservableSourceJust<T>(item);
        }

        /// <summary>
        /// Completes the downstream immediately.
        /// </summary>
        /// <typeparam name="T">The target value type of the empty sequence.</typeparam>
        /// <returns>The shared empty observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> Empty<T>()
        {
            return ObservableSourceEmpty<T>.Instance;
        }

        /// <summary>
        /// Never signals any OnNext, OnError or OnCompleted signal after
        /// subscription.
        /// </summary>
        /// <typeparam name="T">The target element type of the sequence.</typeparam>
        /// <returns>The shared never signaling observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> Never<T>()
        {
            return ObservableSourceNever<T>.Instance;
        }

        /// <summary>
        /// Signals an OnError with the given constant exception to
        /// each individual observer.
        /// </summary>
        /// <typeparam name="T">The target element type of the sequence.</typeparam>
        /// <param name="error">The exception to signal.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new ObservableSourceError<T>(error);
        }

        /// <summary>
        /// Defers the creation of the actual observable source
        /// by calling the given supplier function for each individual
        /// observer to generate the observable source to consume.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="supplier">The function called for each individual
        /// observer and should return an observable source to consume by
        /// the observer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> Defer<T>(Func<IObservableSource<T>> supplier)
        {
            RequireNonNull(supplier, nameof(supplier));

            return new ObservableSourceDefer<T>(supplier);
        }

        /// <summary>
        /// Generates a range of integer values from the given
        /// starting value and up to the specified number of
        /// subsequent values.
        /// </summary>
        /// <param name="start">The starting value of the sequence.</param>
        /// <param name="count">The number of integers to produce.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<int> Range(int start, int count)
        {
            RequireNonNegative(count, nameof(count));
            // TODO handle corner cases of count == 0 and count == 1
            return new ObservableSourceRange(start, start + count);
        }

        /// <summary>
        /// Generates a range of long values from the given
        /// starting value and up to the specified number of
        /// subsequent values.
        /// </summary>
        /// <param name="start">The starting value of the sequence.</param>
        /// <param name="count">The number of integers to produce.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<long> RangeLong(long start, long count)
        {
            RequireNonNegative(count, nameof(count));
            // TODO handle corner cases of count == 0 and count == 1
            return new ObservableSourceRangeLong(start, start + count);
        }

        /// <summary>
        /// Emits the elements of the array to observers.
        /// </summary>
        /// <typeparam name="T">The element type of the source array.</typeparam>
        /// <param name="array">The array whose elements to emit.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> FromArray<T>(params T[] array)
        {
            RequireNonNull(array, nameof(array));
            // TODO handle corner cases of Length == 0 and Length == 1

            return new ObservableSourceArray<T>(array);
        }

        /// <summary>
        /// Concatenates a sequence of observable sources, in order, one
        /// after the other and optionally delays errors until all of
        /// them terminate.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The source sequence to map into observables.</param>
        /// <param name="delayErrors">If true, errors are delayed until all sequences terminate. If false, error is signaled once the current inner source terminates.</param>
        /// <param name="capacityHint">The number of upstream items expected to be cached until
        /// the current observer terminates.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> Concat<T>(this IObservableSource<IObservableSource<T>> source, bool delayErrors = false, int capacityHint = 32)
        {
            return ConcatMap(source, v => v, delayErrors, capacityHint);
        }

        /// <summary>
        /// Convert an enumerable sequence into an observable source.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The enumerable to convert into an observable source.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> FromEnumerable<T>(IEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceEnumerable<T>(source);
        }

        /// <summary>
        /// Emits the value returned by the supplier function
        /// or signals its exception if the supplier crashes.
        /// </summary>
        /// <typeparam name="T">The output value type.</typeparam>
        /// <param name="supplier">The function called for each
        /// individual observer to generate a single result value or
        /// an exception.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> FromFunc<T>(Func<T> supplier)
        {
            RequireNonNull(supplier, nameof(supplier));

            return new ObservableSourceFromFunc<T>(supplier);
        }

        /// <summary>
        /// Runs the action for each individual observer and
        /// signals OnCompleted or OnError based on the success
        /// or failure of the action.
        /// </summary>
        /// <typeparam name="T">The output value type.</typeparam>
        /// <param name="action">The action called for each
        /// individual observer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> FromAction<T>(Action action)
        {
            RequireNonNull(action, nameof(action));

            return new ObservableSourceFromAction<T>(action);
        }

        /// <summary>
        /// Signals a 0L after the specified delay and completes.
        /// </summary>
        /// <param name="delay">The time delay.</param>
        /// <param name="scheduler">The target scheduler where to emit 0L.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<long> Timer(TimeSpan delay, IScheduler scheduler)
        {
            RequireNonNull(scheduler, nameof(scheduler));

            return new ObservableSourceTimer(delay, scheduler);
        }

        /// <summary>
        /// Emits a long values, starting from 0, over time.
        /// </summary>
        /// <param name="period">The time between emitting subsequent values.</param>
        /// <param name="scheduler">The scheduler used for emission and timing.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<long> Interval(TimeSpan period, IScheduler scheduler)
        {
            return IntervalRange(0, long.MaxValue, period, period, scheduler);
        }

        /// <summary>
        /// Emits a long values, starting from 0, over time.
        /// </summary>
        /// <param name="initialDelay">The delay before signaling the start value.</param>
        /// <param name="period">The time between emitting subsequent values.</param>
        /// <param name="scheduler">The scheduler used for emission and timing.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<long> Interval(TimeSpan initialDelay, TimeSpan period, IScheduler scheduler)
        {
            return IntervalRange(0, long.MaxValue, initialDelay, period, scheduler);
        }

        /// <summary>
        /// Emits a range of long values over time.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="count">The number of items to emit.</param>
        /// <param name="period">The time between emitting subsequent values.</param>
        /// <param name="scheduler">The scheduler used for emission and timing.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<long> IntervalRange(long start, long count, TimeSpan period, IScheduler scheduler)
        {
            return IntervalRange(start, count, period, period, scheduler);
        }

        /// <summary>
        /// Emits a range of long values over time.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="count">The number of items to emit.</param>
        /// <param name="initialDelay">The delay before signaling the start value.</param>
        /// <param name="period">The time between emitting subsequent values.</param>
        /// <param name="scheduler">The scheduler used for emission and timing.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<long> IntervalRange(long start, long count, TimeSpan initialDelay, TimeSpan period, IScheduler scheduler)
        {
            RequireNonNull(scheduler, nameof(scheduler));

            return new ObservableSourceIntervalRange(start, start + count, initialDelay, period, scheduler);
        }

        /// <summary>
        /// Wraps a Task and signals its terminal event to observers.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="task">The task to wrap and signal terminal events of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<T> FromTask<T>(Task task)
        {
            RequireNonNull(task, nameof(task));

            return new ObservableSourceFromTask<T>(task);
        }

        /// <summary>
        /// Wraps a Task and signals its terminal value or error to observers.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="task">The task to wrap and signal terminal events of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<T> FromTask<T>(Task<T> task)
        {
            RequireNonNull(task, nameof(task));

            return new ObservableSourceFromTaskValue<T>(task);
        }

        /// <summary>
        /// Generates a resource for each individual observer, then
        /// creates an observable source sequence based on this resource
        /// and relays its signals to the same observer.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <typeparam name="S">The resource type.</typeparam>
        /// <param name="resourceSupplier">Called for each individual observer to create a resource.</param>
        /// <param name="sourceSelector">The function receiving the resource and should return
        /// an observable source sequence.</param>
        /// <param name="resourceCleanup">Called when the resource is no longer needed upon
        /// cancellation or when the sequence terminates.</param>
        /// <param name="eager">If true, the resourceCleanup will be called before signaling
        /// the terminal event.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<T> Using<T, S>(Func<S> resourceSupplier, Func<S, IObservableSource<T>> sourceSelector, Action<S> resourceCleanup = null, bool eager = true)
        {
            RequireNonNull(resourceSupplier, nameof(resourceSupplier));
            RequireNonNull(sourceSelector, nameof(sourceSelector));

            return new ObservableSourceUsing<T, S>(resourceSupplier, sourceSelector, resourceCleanup, eager);
        }

        /// <summary>
        /// Combines the latest items of each source observable through
        /// a mapper function.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="sources">The array of observables to combine.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> CombineLatest<T, R>(Func<T[], R> mapper, params IObservableSource<T>[] sources)
        {
            return CombineLatest(mapper, false, sources);
        }

        /// <summary>
        /// Combines the latest items of each source observable through
        /// a mapper function, optionally delaying errors until all
        /// of them terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="delayErrors">If true, errors from all sources are delayed until all sources terminate.</param>
        /// <param name="sources">The array of observables to combine.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> CombineLatest<T, R>(Func<T[], R> mapper, bool delayErrors, params IObservableSource<T>[] sources)
        {
            RequireNonNull(mapper, nameof(mapper));
            RequireNonNull(sources, nameof(sources));

            return new ObservableSourceCombineLatest<T, R>(sources, mapper, delayErrors);
        }

        /// <summary>
        /// Combines the latest items of each source observable through
        /// a mapper function, optionally delaying errors until all
        /// of them terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="sources">The array of observables to combine.</param>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="delayErrors">If true, errors from all sources are delayed until all sources terminate.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> CombineLatest<T, R>(this IObservableSource<T>[] sources, Func<T[], R> mapper, bool delayErrors = false)
        {
            return CombineLatest(mapper, delayErrors, sources);
        }

        /// <summary>
        /// Combines the next item of each source observable (a row of values) through
        /// a mapper function.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="sources">The array of observables to combine.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> Zip<T, R>(Func<T[], R> mapper, params IObservableSource<T>[] sources)
        {
            return Zip(sources, mapper, false);
        }

        /// <summary>
        /// Combines the next item of each source observable (a row of values) through
        /// a mapper function.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="capacityHint">The expected number of items to be buffered by each source.</param>
        /// <param name="sources">The array of observables to combine.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> Zip<T, R>(Func<T[], R> mapper, int capacityHint, params IObservableSource<T>[] sources)
        {
            return Zip(mapper, false, capacityHint, sources);
        }

        /// <summary>
        /// Combines the next item of each source observable (a row of values) through
        /// a mapper function, optionally delaying errors until no more rows
        /// can be consumed.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="delayErrors">If true, errors from all sources are delayed until all sources terminate.</param>
        /// <param name="sources">The array of observables to combine.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> Zip<T, R>(Func<T[], R> mapper, bool delayErrors, params IObservableSource<T>[] sources)
        {
            return Zip<T, R>(sources, mapper, delayErrors);
        }

        /// <summary>
        /// Combines the next item of each source observable (a row of values) through
        /// a mapper function, optionally delaying errors until no more rows
        /// can be consumed.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="delayErrors">If true, errors from all sources are delayed until all sources terminate.</param>
        /// <param name="capacityHint">The expected number of items to be buffered by each source.</param>
        /// <param name="sources">The array of observables to combine.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> Zip<T, R>(Func<T[], R> mapper, bool delayErrors, int capacityHint = 32, params IObservableSource<T>[] sources)
        {
            RequireNonNull(mapper, nameof(mapper));
            RequireNonNull(sources, nameof(sources));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ObservableSourceZip<T, R>(sources, mapper, delayErrors, capacityHint);
        }

        /// <summary>
        /// Combines the next item of each source observable (a row of values) through
        /// a mapper function, optionally delaying errors until no more rows
        /// can be consumed.
        /// </summary>
        /// <typeparam name="T">The element type of the observables.</typeparam>
        /// <typeparam name="R">The result type.</typeparam>
        /// <param name="sources">The array of observables to combine.</param>
        /// <param name="mapper">The function that receives the latest values and should return an item to be emitted.</param>
        /// <param name="delayErrors">If true, errors from all sources are delayed until all sources terminate.</param>
        /// <param name="capacityHint">The expected number of items to be buffered by each source.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> Zip<T, R>(this IObservableSource<T>[] sources, Func<T[], R> mapper, bool delayErrors = false, int capacityHint = 32)
        {
            return Zip(mapper, delayErrors, capacityHint, sources);
        }

        /// <summary>
        /// Concatenates a sequence of observables eagerly by running some
        /// or all of them at once and emitting their items in order.
        /// </summary>
        /// <typeparam name="T">The value type of the inner observables.</typeparam>
        /// <param name="sources">The sequence of observables to concatenate eagerly.</param>
        /// <param name="maxConcurrency">The maximum number of observables to run at a time.</param>
        /// <param name="capacityHint">The number of items expected from each observable.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<T> ConcatEager<T>(this IObservableSource<IObservableSource<T>> sources, int maxConcurrency = int.MaxValue, int capacityHint = 128)
        {
            return ConcatMapEager(sources, v => v, maxConcurrency, capacityHint);
        }

        /// <summary>
        /// Switches to a new inner observable when the upstream emits it,
        /// disposing the previous active observable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources">The sequence of observables to switch between.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all observables have terminated or got disposed.</param>
        /// <param name="capacityHint">The expected number of items to buffer per inner observable</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<T> Switch<T>(this IObservableSource<IObservableSource<T>> sources, bool delayErrors = false, int capacityHint = 128)
        {
            return SwitchMap(sources, v => v, delayErrors, capacityHint);
        }

        /// <summary>
        /// Creates an observable sequence by providing an emitter
        /// API to bridge the callback world with the reactive world.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="onSubscribe">The action called for each individual
        /// observer and receives an <see cref="ISignalEmitter{T}"/> that
        /// allows emitting events and registering a resource within the sequence
        /// to be automatically disposed upon termination or cancellation.</param>
        /// <param name="serialize">If true, the <see cref="ISignalEmitter{T}"/>'s
        /// OnNext, OnError and OnCompleted will be thread-safe to call from multiple
        /// threads.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Create<T>(Action<ISignalEmitter<T>> onSubscribe, bool serialize = false)
        {
            RequireNonNull(onSubscribe, nameof(onSubscribe));

            return new ObservableSourceCreate<T>(onSubscribe, serialize);
        }

        /// <summary>
        /// Merge some or all observables provided by the outer observable.
        /// </summary>
        /// <typeparam name="T">The result and inner observable element type.</typeparam>
        /// <param name="sources">The sequence of inner observable sequences</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <param name="maxConcurrency">The maximum number of sources to run at once.</param>
        /// <param name="capacityHint">The expected number of items from the inner sources that will have to wait in a buffer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Merge<T>(this IObservableSource<IObservableSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue, int capacityHint = 128)
        {
            RequireNonNull(sources, nameof(sources));
            RequirePositive(maxConcurrency, nameof(maxConcurrency));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ObservableSourceMergeMany<T>(sources, delayErrors, maxConcurrency, capacityHint);
        }

        /// <summary>
        /// Concatenates multiple sources in-order provided via
        /// an array of observable sources
        /// </summary>
        /// <typeparam name="T">The element type of the inner and result sequences.</typeparam>
        /// <param name="sources">The array of observable sources to concatenate.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Concat<T>(params IObservableSource<T>[] sources)
        {
            return Concat(false, sources);
        }

        /// <summary>
        /// Concatenates multiple sources in-order provided via
        /// an array of observable sources, optionally delaying
        /// errors until all sources terminate.
        /// </summary>
        /// <typeparam name="T">The element type of the inner and result sequences.</typeparam>
        /// <param name="delayErrors">If true, errors will be delayed until all sources terminate.</param>
        /// <param name="sources">The array of observable sources to concatenate.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Concat<T>(bool delayErrors, params IObservableSource<T>[] sources)
        {
            RequireNonNull(sources, nameof(sources));

            return new ObservableSourceConcatArray<T>(sources, delayErrors);
        }

        /// <summary>
        /// Concatenates multiple sources in-order provided via
        /// an array of observable sources, optionally delaying
        /// errors until all sources terminate.
        /// </summary>
        /// <typeparam name="T">The element type of the inner and result sequences.</typeparam>
        /// <param name="delayErrors">If true, errors will be delayed until all sources terminate.</param>
        /// <param name="sources">The array of observable sources to concatenate.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Concat<T>(this IObservableSource<T>[] sources, bool delayErrors = false)
        {
            RequireNonNull(sources, nameof(sources));

            return new ObservableSourceConcatArray<T>(sources, delayErrors);
        }

        /// <summary>
        /// Concatenates multiple sources in-order provided via
        /// an enumerable of observable sources, optionally delaying
        /// errors until all sources terminate.
        /// </summary>
        /// <typeparam name="T">The element type of the inner and result sequences.</typeparam>
        /// <param name="sources">The enumerable sequence of observable sources to concatenate.</param>
        /// <param name="delayErrors">If true, errors will be delayed until all sources terminate.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Concat<T>(this IEnumerable<IObservableSource<T>> sources, bool delayErrors = false)
        {
            RequireNonNull(sources, nameof(sources));

            return new ObservableSourceConcatEnumerable<T>(sources, delayErrors);
        }

        /// <summary>
        /// Subscribes to all sources and relays the signals
        /// of the fastest responding one.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="sources">The array of sources to pick the fastest responding of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Amb<T>(params IObservableSource<T>[] sources)
        {
            RequireNonNull(sources, nameof(sources));

            return new ObservableSourceAmbArray<T>(sources);
        }

        /// <summary>
        /// Subscribes to all sources and relays the signals
        /// of the fastest responding one.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="sources">The array of sources to pick the fastest responding of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Amb<T>(this IEnumerable<IObservableSource<T>> sources)
        {
            RequireNonNull(sources, nameof(sources));

            return new ObservableSourceAmbEnumerable<T>(sources);
        }

        // --------------------------------------------------------------
        // Instance methods
        // --------------------------------------------------------------

        /// <summary>
        /// Calls the specified function with the upstream source
        /// during assembly time and returns a possibly new
        /// observable source sequence with additional behavior applied
        /// to it.
        /// This allows applying the same operators to a sequence via
        /// a help of a function callback in-sequence.
        /// </summary>
        /// <typeparam name="T">The upstream element type.</typeparam>
        /// <typeparam name="R">The downstream element type.</typeparam>
        /// <param name="source">The source sequence to compose with.</param>
        /// <param name="composer">The function receiving the upstream
        /// source sequence and should return an observable sequence in return
        /// during assembly time.</param>
        /// <returns>The observable sequence returned by the <paramref name="composer"/> function.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<R> Compose<T, R>(this IObservableSource<T> source, Func<IObservableSource<T>, IObservableSource<R>> composer)
        {
            RequireNonNull(source, nameof(source));

            return composer(source);
        }

        /// <summary>
        /// Relays at most the given number of items from the upstream,
        /// then disposes the upstream and completes the downstream
        /// observer.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source observable to limit in number of elements.</param>
        /// <param name="n">The maximum number of elements to let through,
        /// non-positive values will immediately dispose the upstream thus
        /// some subscription side-effects would still happen depending
        /// on the source operator.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> Take<T>(this IObservableSource<T> source, long n)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceTake<T>(source, n);
        }

        /// <summary>
        /// Skips the given number of items from the beginning of
        /// the sequence and relays the rest if any.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to skip a number of elements.</param>
        /// <param name="n">The number of elements to skip. If the sequence is shorter than this or signals an error, completion or error is always relayed.</param>
        /// <returns></returns>
        public static IObservableSource<T> Skip<T>(this IObservableSource<T> source, long n)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceSkip<T>(source, Math.Max(0L, n));
        }

        /// <summary>
        /// If the source sequence doesn't produce the first or
        /// next item within the specified timeout window,
        /// signal an error or switch to the given fallback sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source to detect timeout on.</param>
        /// <param name="timeout">The timeout value.</param>
        /// <param name="scheduler">The scheduler whose notion of time
        /// to use and if <paramref name="fallback"/> is present,
        /// subscribe to it to resume.</param>
        /// <param name="fallback">Optional observable source sequence to switch
        /// to if the main source times out.</param>
        /// <returns>The new observable source sequence.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> Timeout<T>(this IObservableSource<T> source, TimeSpan timeout, IScheduler scheduler, IObservableSource<T> fallback = null)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new ObservableSourceTimeout<T>(source, timeout, scheduler, fallback);
        }

        /// <summary>
        /// Applies a function to each item of the upstream sequence
        /// and emits the resulting value to the downstream.
        /// </summary>
        /// <typeparam name="T">The source value type.</typeparam>
        /// <typeparam name="R">The result value type.</typeparam>
        /// <param name="source">The source to transform each items of.</param>
        /// <param name="mapper">The function that receives an upstream item
        /// and should return an item to be emitted to the downstream.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<R> Map<T, R>(this IObservableSource<T> source, Func<T, R> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new ObservableSourceMap<T, R>(source, mapper);
        }

        /// <summary>
        /// Calls a predicate with each upstream item and relays those
        /// which pass it.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The source sequence to filter.</param>
        /// <param name="predicate">The predicate receiving the upstream item
        /// and should return true if that item can be passed to the downstream
        /// or false if it should be ignored.</param>
        /// <returns></returns>
        public static IObservableSource<T> Filter<T>(this IObservableSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceFilter<T>(source, predicate);
        }


        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever the
        /// upstream observable <paramref name="source"/> signals an OnNext item.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoOnNext<T>(this IObservableSource<T> source, Action<T> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onNext: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> after the
        /// upstream observable <paramref name="source"/>'s signaled an
        /// OnNext.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoAfterNext<T>(this IObservableSource<T> source, Action<T> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onAfterNext: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever a
        /// signal observer subscribes to the observable <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoOnSubscribe<T>(this IObservableSource<T> source, Action<IDisposable> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onSubscribe: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever a
        /// signal observer disposes to the connection to
        /// the observable <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoOnDispose<T>(this IObservableSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onDispose: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// signal observer gets completed by
        /// the observable <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoOnCompleted<T>(this IObservableSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onCompleted: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// signal observer receives the error signal from
        /// the observable <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoOnError<T>(this IObservableSource<T> source, Action<Exception> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onError: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// signal observer gets terminated normally or with an error by
        /// the observable <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoOnTerminate<T>(this IObservableSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onTerminate: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> after a
        /// signal observer gets terminated normally or exceptionally by
        /// the observable <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoAfterTerminate<T>(this IObservableSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, onAfterTerminate: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> exactly once per observable
        /// observer and after the signal observer gets terminated normally
        /// or exceptionally or the observer disposes the connection to the
        /// the observable <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The observable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> DoFinally<T>(this IObservableSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return ObservableSourcePeek<T>.Create(source, doFinally: handler);
        }

        /// <summary>
        /// Maps the source observable sequence into inner observable
        /// sources and relays their items in order, one after the other
        /// until all sequences terminate.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="R">The element type of the inner and output sequence.</typeparam>
        /// <param name="source">The source sequence to map into observables.</param>
        /// <param name="mapper">The function taking the upstream item
        /// and should return an inner observable sequence to relay signals of.</param>
        /// <param name="delayErrors">If true, errors are delayed until all sequences terminate. If false, error is signaled once the current inner source terminates.</param>
        /// <param name="capacityHint">The number of upstream items expected to be cached until
        /// the current observer terminates.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<R> ConcatMap<T, R>(this IObservableSource<T> source, Func<T, IObservableSource<R>> mapper, bool delayErrors = false, int capacityHint = 32)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ObservableSourceConcatMap<T, R>(source, mapper, delayErrors, capacityHint);
        }

        /// <summary>
        /// Hides the identity of the source observable sequence, including
        /// its disposable connection as well as prevents identity-based
        /// optimizations (such as fusion).
        /// The returned observable sequence does not and should not
        /// support any optimizations.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to hide.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> Hide<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceHide<T>(source);
        }

        /// <summary>
        /// Relays signals from the main source until the
        /// other source signals an item or completes, completing
        /// the downstream immediately.
        /// </summary>
        /// <typeparam name="T">The main and result sequence type.</typeparam>
        /// <typeparam name="U">The element type of the other source sequence.</typeparam>
        /// <param name="source">The main source sequence.</param>
        /// <param name="other">The stop indicator sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> TakeUntil<T, U>(this IObservableSource<T> source, IObservableSource<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new ObservableSourceTakeUntil<T, U>(source, other);
        }

        /// <summary>
        /// Skips signals from the main source until the
        /// other source signals an item or completes, relaying
        /// items then on.
        /// </summary>
        /// <typeparam name="T">The main and result sequence type.</typeparam>
        /// <typeparam name="U">The element type of the other source sequence.</typeparam>
        /// <param name="source">The main source sequence.</param>
        /// <param name="other">The indicator sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> SkipUntil<T, U>(this IObservableSource<T> source, IObservableSource<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new ObservableSourceSkipUntil<T, U>(source, other);
        }

        /// <summary>
        /// Ignores OnNext items and only relays the terminal events.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to ignore elements of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> IgnoreElements<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceIgnoreElements<T>(source);
        }

        /// <summary>
        /// When the upstream source fails, the handler function
        /// is invoked to return an observable source to resume
        /// the flow.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The upstream source that can fail.</param>
        /// <param name="handler">The function receiving the upstream error
        /// and should return a fallback observable source to resume with.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> OnErrorResumeNext<T>(this IObservableSource<T> source, Func<Exception, IObservableSource<T>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new ObservableSourceOnErrorResumeNext<T>(source, handler);
        }

        /// <summary>
        /// Keeps switching to the next fallback source if the
        /// main source or the previous fallback source is empty.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The main source that may be empty.</param>
        /// <param name="fallbacks">The fallback sources to switch to if
        /// the main source or the previous fallback is empty.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> SwitchIfEmpty<T>(this IObservableSource<T> source, params IObservableSource<T>[] fallbacks)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(fallbacks, nameof(fallbacks));

            return new ObservableSourceSwitchIfEmpty<T>(source, fallbacks);
        }

        /// <summary>
        /// Subscribes to the source observable sequence on the
        /// given scheduler.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source to subscribe to on a given scheduler.</param>
        /// <param name="scheduler">The scheduler to use to subscribe to the <paramref name="source"/> sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> SubscribeOn<T>(this IObservableSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new ObservableSourceSubscribeOn<T>(source, scheduler);
        }

        /// <summary>
        /// When the downstream disposes the sequence
        /// the dispose signal is signaled towards the upstream
        /// from the given scheduler.
        /// Note that OnError and OnCompleted don't trigger unsubscription
        /// with observable sources.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The source observable to dispose on a scheduler.</param>
        /// <param name="scheduler">The scheduler to dispose the upstream connection on.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> UnsubscribeOn<T>(this IObservableSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new ObservableSourceUnsubscribeOn<T>(source, scheduler);
        }

        /// <summary>
        /// Takes signals from the upstream and re-emits them on
        /// the given scheduler.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source to observe signals of on a scheduler.</param>
        /// <param name="scheduler">The scheduler to use to re-emit signals from <paramref name="source"/>.</param>
        /// <param name="delayError">If true, errors are re-emitted after normal items. If false, errors may cut ahead of normal items.</param>
        /// <param name="capacityHint">The expected number of items to be temporarily cached until the scheduler thread can emit them.</param>
        /// <param name="fair">If true, the task to re-emit the upstream signal is renewed after each item emitted. If false, the scheduler will be used as long as there are items to re-emit at a time, potentially preventing other tasks on the same scheduler from executing for a longer period of time.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> ObserveOn<T>(this IObservableSource<T> source, IScheduler scheduler, bool delayError = true, int capacityHint = 128, bool fair = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ObservableSourceObserveOn<T>(source, scheduler, delayError, capacityHint, fair);
        }

        /// <summary>
        /// Collects items from the upstream via a collector action into
        /// a collection provided for each individual observer and signals
        /// this collection to the downstream.
        /// </summary>
        /// <typeparam name="T">The element type of the upstream sequence.</typeparam>
        /// <typeparam name="C">The collection type.</typeparam>
        /// <param name="source">The source sequence to collect.</param>
        /// <param name="collectionSupplier">The function providing a collection to each
        /// individual observer.</param>
        /// <param name="collector">The action receiving the collection and the current upstream
        /// item to be combined in some fashion.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<C> Collect<T, C>(this IObservableSource<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(collectionSupplier, nameof(collectionSupplier));
            RequireNonNull(collector, nameof(collector));

            return new ObservableSourceCollect<T, C>(source, collectionSupplier, collector);
        }

        /// <summary>
        /// Collects all upstream items into a list and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IList<T>> ToList<T>(this IObservableSource<T> source)
        {
            return Collect(source, () => new List<T>(), (a, b) => a.Add(b));
        }

        /// <summary>
        /// Collects all upstream items into a list and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <param name="capacityHint">The expected number of items to be stored in the resulting
        /// list.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IList<T>> ToList<T>(this IObservableSource<T> source, int capacityHint)
        {
            RequirePositive(capacityHint, nameof(capacityHint));

            return Collect(source, () => new List<T>(capacityHint), (a, b) => a.Add(b));
        }

        /// <summary>
        /// Collects all upstream items into a dictionary and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="K">The key type</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <param name="keySelector">The function receiving the upstream item and
        /// returning the key for the map.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IDictionary<K, T>> ToDictionary<T, K>(this IObservableSource<T> source, Func<T, K> keySelector)
        {
            return ToDictionary(source, keySelector, v => v, EqualityComparer<K>.Default);
        }

        /// <summary>
        /// Collects and projects all upstream items into a dictionary and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="K">The key type.</typeparam>
        /// <typeparam name="V">The value type of the dictionary.</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <param name="keySelector">The function receiving the upstream item and
        /// returns the key for the dictionary.</param>
        /// <param name="valueSelector">The function receiving the upstream item and
        /// returns the value for the dictionary.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IDictionary<K, V>> ToDictionary<T, K, V>(this IObservableSource<T> source, Func<T, K> keySelector, Func<T, V> valueSelector)
        {
            return ToDictionary(source, keySelector, valueSelector, EqualityComparer<K>.Default);
        }

        /// <summary>
        /// Collects and projects all upstream items into a dictionary and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="K">The key type.</typeparam>
        /// <typeparam name="V">The value type of the dictionary.</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <param name="keySelector">The function receiving the upstream item and
        /// returns the key for the dictionary.</param>
        /// <param name="valueSelector">The function receiving the upstream item and
        /// returns the value for the dictionary.</param>
        /// <param name="keyComparer">The custom comparer for the keys</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IDictionary<K, V>> ToDictionary<T, K, V>(this IObservableSource<T> source, Func<T, K> keySelector, Func<T, V> valueSelector, IEqualityComparer<K> keyComparer)
        {
            RequireNonNull(keySelector, nameof(keySelector));
            RequireNonNull(valueSelector, nameof(valueSelector));
            RequireNonNull(keyComparer, nameof(keyComparer));

            return Collect(source, () => new Dictionary<K, V>(keyComparer), (a, b) => a.Add(keySelector(b), valueSelector(b)));
        }

        /// <summary>
        /// Collects and projects all upstream items into a lookup (multimap) and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="K">The key type.</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <param name="keySelector">The function receiving the upstream item and
        /// returns the key for the dictionary.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<ILookup<K, T>> ToLookup<T, K>(this IObservableSource<T> source, Func<T, K> keySelector)
        {
            return ToLookup(source, keySelector, v => v, EqualityComparer<K>.Default);
        }

        /// <summary>
        /// Collects and projects all upstream items into a lookup (multimap) and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="K">The key type.</typeparam>
        /// <typeparam name="V">The value type of the dictionary.</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <param name="keySelector">The function receiving the upstream item and
        /// returns the key for the dictionary.</param>
        /// <param name="valueSelector">The function receiving the upstream item and
        /// returns the value for the dictionary.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<ILookup<K, V>> ToLookup<T, K, V>(this IObservableSource<T> source, Func<T, K> keySelector, Func<T, V> valueSelector)
        {
            return ToLookup(source, keySelector, valueSelector, EqualityComparer<K>.Default);
        }

        /// <summary>
        /// Collects and projects all upstream items into a lookup (multimap) and emits that upon
        /// completion.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="K">The key type.</typeparam>
        /// <typeparam name="V">The value type of the dictionary.</typeparam>
        /// <param name="source">The source of items to collect into a list.</param>
        /// <param name="keySelector">The function receiving the upstream item and
        /// returns the key for the dictionary.</param>
        /// <param name="valueSelector">The function receiving the upstream item and
        /// returns the value for the dictionary.</param>
        /// <param name="keyComparer">The custom comparer for the keys.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<ILookup<K, V>> ToLookup<T, K, V>(this IObservableSource<T> source, Func<T, K> keySelector, Func<T, V> valueSelector, IEqualityComparer<K> keyComparer)
        {
            RequireNonNull(keySelector, nameof(keySelector));
            RequireNonNull(valueSelector, nameof(valueSelector));
            RequireNonNull(keyComparer, nameof(keyComparer));

            return Collect(source, () => new Lookup<K, V>(keyComparer), (a, b) => a.Add(keySelector(b), valueSelector(b)));
        }

        /// <summary>
        /// Signals true if any of the upstream source item passes the predicate (short circuit),
        /// false otherwise.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The source to find an item passing the predicate.</param>
        /// <param name="predicate">The predicate receiving the upstream item and should
        /// return true to stop and indicate true as the outcome of the sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<bool> Any<T>(this IObservableSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceAny<T>(source, predicate);
        }

        /// <summary>
        /// Signals true if all of the upstream source item passes the predicate,
        /// false otherwise (short circuit).
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The source to find an item passing the predicate.</param>
        /// <param name="predicate">The predicate receiving the upstream item and should
        /// return false to stop and indicate false as the outcome of the sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<bool> All<T>(this IObservableSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceAll<T>(source, predicate);
        }

        /// <summary>
        /// Signals true if the source sequence has no items, false otherwise (short circuit).
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The source to find an item passing the predicate.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<bool> IsEmpty<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceIsEmpty<T>(source);
        }

        /// <summary>
        /// Reduce the upstream into a single value by applying a function to
        /// the initial or previous reduced value and the current upstream value
        /// to produce the next or last value to be finally emitted.
        /// </summary>
        /// <typeparam name="T">The element type of the upstream.</typeparam>
        /// <typeparam name="C">The result type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="initialSupplier">The function to generate an initial value for
        /// each individual observer.</param>
        /// <param name="reducer">The function called with the first or previous reduced
        /// value and should return a new reduced value.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<C> Reduce<T, C>(this IObservableSource<T> source, Func<C> initialSupplier, Func<C, T, C> reducer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(initialSupplier, nameof(initialSupplier));
            RequireNonNull(reducer, nameof(reducer));

            return new ObservableSourceReduce<T, C>(source, initialSupplier, reducer);
        }

        /// <summary>
        /// Reduce the upstream into a single value by applying a function to the 
        /// previously reduced and current item to produce the next reduced item
        /// and emit the last of such reduced item to the downstream.
        /// </summary>
        /// <typeparam name="T">The element and result type of the sequences.</typeparam>
        /// <param name="source">The source sequence to reduce into a single item.</param>
        /// <param name="reducer">The function that takes the previous reduced item, the
        /// current upstream item and should produce the next reduced item.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<T> Reduce<T>(this IObservableSource<T> source, Func<T, T, T> reducer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(reducer, nameof(reducer));

            return new ObservableSourceReducePlain<T>(source, reducer);
        }

        /// <summary>
        /// Buffer items into non-overlapping, non-skipping lists and emit those lists
        /// as they become full.
        /// </summary>
        /// <typeparam name="T">The upstream sequence type.</typeparam>
        /// <param name="source">The source sequence to buffer.</param>
        /// <param name="size">The number of items per buffer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IList<T>> Buffer<T>(this IObservableSource<T> source, int size)
        {
            RequireNonNull(source, nameof(source));
            RequirePositive(size, nameof(size));

            return new ObservableSourceBuffer<T, IList<T>>(source, () => new List<T>(), size, size);
        }

        /// <summary>
        /// Buffer items into lists, possibly overlapping (skip &lt; size),
        /// possibly skipping (skip &gt; size) or exact chunks (skip == size).
        /// </summary>
        /// <typeparam name="T">The upstream sequence type.</typeparam>
        /// <param name="source">The source sequence to buffer.</param>
        /// <param name="size">The number of items per buffer.</param>
        /// <param name="skip">The items to skip before starting a new buffer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IList<T>> Buffer<T>(this IObservableSource<T> source, int size, int skip)
        {
            RequireNonNull(source, nameof(source));
            RequirePositive(size, nameof(size));

            return new ObservableSourceBuffer<T, IList<T>>(source, () => new List<T>(), size, skip);
        }

        /// <summary>
        /// Buffer items into custom collections, possibly overlapping (skip &lt; size),
        /// possibly skipping (skip &gt; size) or exact chunks (skip == size).
        /// </summary>
        /// <typeparam name="T">The element type of the upstream.</typeparam>
        /// <typeparam name="B">The buffer type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="size">The number of items to put into each buffer.</param>
        /// <param name="skip">The items to skip before starting a new buffer.</param>
        /// <param name="bufferSupplier">The function returning a fresh buffer when a previous buffer became full.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<B> Buffer<T, B>(this IObservableSource<T> source, int size, int skip, Func<B> bufferSupplier) where B : ICollection<T>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(bufferSupplier, nameof(bufferSupplier));
            RequirePositive(size, nameof(size));
            RequirePositive(skip, nameof(skip));

            return new ObservableSourceBuffer<T, B>(source, bufferSupplier, size, skip);
        }

        /// <summary>
        /// Buffer the upstream elements into a list
        /// (no skipping, no overlapping) until the boundary
        /// signals an item, at which point the buffer is emitted and a new buffer is
        /// started.
        /// </summary>
        /// <typeparam name="T">The upstream sequence type.</typeparam>
        /// <typeparam name="U">The element type of the boundary sequence.</typeparam>
        /// <param name="source">The source to buffer.</param>
        /// <param name="boundary">The sequence to signal the end of the previous buffer and
        /// the start of a new buffer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<IList<T>> Buffer<T, U>(this IObservableSource<T> source, IObservableSource<U> boundary)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(boundary, nameof(boundary));

            return new ObservableSourceBufferBoundary<T, IList<T>, U>(source, boundary, () => new List<T>());
        }

        /// <summary>
        /// Buffer the upstream elements into a custom collection
        /// (no skipping, no overlapping) until the boundary
        /// signals an item, at which point the buffer is emitted and a new buffer is
        /// started.
        /// </summary>
        /// <typeparam name="T">The upstream sequence type.</typeparam>
        /// <typeparam name="B">The custom collection type.</typeparam>
        /// <typeparam name="U">The element type of the boundary sequence.</typeparam>
        /// <param name="source">The source to buffer.</param>
        /// <param name="boundary">The sequence to signal the end of the previous buffer and
        /// the start of a new buffer.</param>
        /// <param name="bufferSupplier">The function returning a custom collection on demand.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<B> Buffer<T, B, U>(this IObservableSource<T> source, IObservableSource<U> boundary, Func<B> bufferSupplier) where B : ICollection<T>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(boundary, nameof(boundary));
            RequireNonNull(bufferSupplier, nameof(bufferSupplier));

            return new ObservableSourceBufferBoundary<T, B, U>(source, boundary, bufferSupplier);
        }

        /// <summary>
        /// Signals the element with the given index or signals an
        /// IndexOutOfBoundsException if the source sequence is shorter.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to get an element from.</param>
        /// <param name="index">The index of the item to return (zero-based).</param>
        /// <returns>The new observable source sequence.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> ElementAt<T>(this IObservableSource<T> source, long index)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(index, nameof(index));

            return new ObservableSourceElementAt<T>(source, index);
        }

        /// <summary>
        /// Signals the element with the given index or signals the
        /// given default item if the source sequence is shorter.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to get an element from.</param>
        /// <param name="index">The index of the item to return (zero-based).</param>
        /// <param name="defaultItem">The item to emit if the source is shorter.</param>
        /// <returns>The new observable source sequence.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> ElementAt<T>(this IObservableSource<T> source, long index, T defaultItem)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(index, nameof(index));

            return new ObservableSourceElementAtDefault<T>(source, index, defaultItem);
        }

        /// <summary>
        /// Signals the first element from the source sequence or
        /// IndexOutOfBoundsException if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to get the first element from.</param>
        /// <returns>The new observable source sequence.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> First<T>(this IObservableSource<T> source)
        {
            return ElementAt(source, 0L);
        }

        /// <summary>
        /// Signals the first element from the source sequence or
        /// the given default item if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to get the first element from.</param>
        /// <param name="defaultItem">The default item to emit if the source is empty.</param>
        /// <returns>The new observable source sequence.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> First<T>(this IObservableSource<T> source, T defaultItem)
        {
            return ElementAt(source, 0L, defaultItem);
        }

        /// <summary>
        /// Signals the single item of the sequence or an IndexOutOfBoundsException
        /// if the source is empty or contains more than one item.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence that should contain one item.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> Single<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceSingle<T>(source);
        }

        /// <summary>
        /// Signals the single item of the sequence, the given default item
        /// if the source is empty or an IndexOutOfBoundsException
        /// if the source contains more than one item.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence that should contain one item.</param>
        /// <param name="defaultItem">The item to emit if the source is empty.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> Single<T>(this IObservableSource<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceSingleDefault<T>(source, defaultItem);
        }

        /// <summary>
        /// Signals the last item of the sequence or an IndexOutOfBoundsException
        /// if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to get the last item of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> Last<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceLast<T>(source);
        }

        /// <summary>
        /// Signals the last item of the sequence or the given default item
        /// if the source is empty.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to get the last item of.</param>
        /// <param name="defaultItem">The item to emit if the source is empty.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> Last<T>(this IObservableSource<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceLastDefault<T>(source, defaultItem);
        }

        /// <summary>
        /// Relay items from the source sequence while the predicate returns true
        /// (checked before emission).
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="predicate">The function receiving the current item and should
        /// return true if that item may pass, false to complete the sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> TakeWhile<T>(this IObservableSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceTakeWhile<T>(source, predicate);

        }

        /// <summary>
        /// Relay items from the source sequence until the predicate returns false
        /// (checked after emission).
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="predicate">The function receiving the current item and should
        /// return false if that item may pass, true to complete the sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> TakeUntil<T>(this IObservableSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceTakeUntil<T>(source, predicate);

        }

        /// <summary>
        /// Skip items from the source sequence while the predicate returns true
        /// (checked before emission).
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="predicate">The function receiving the current item and should
        /// return false if that item and any subsequent item may pass, true to keep skipping.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> SkipWhile<T>(this IObservableSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceSkipWhile<T>(source, predicate);

        }

        /// <summary>
        /// Takes the last <paramref name="n"/> items from the upstream and
        /// ignores items before that.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source to keep only the last items.</param>
        /// <param name="n">The number of last items to keep.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> TakeLast<T>(this IObservableSource<T> source, int n)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(n, nameof(n));

            if (n == 0)
            {
                return new ObservableSourceIgnoreElements<T>(source);
            }

            return new ObservableSourceTakeLast<T>(source, n);
        }

        /// <summary>
        /// Skips the last <paramref name="n"/> items from the upstream and
        /// relays items before that.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source to skip some last items.</param>
        /// <param name="n">The number of last items to skip.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.20</remarks>
        public static IObservableSource<T> SkipLast<T>(this IObservableSource<T> source, int n)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(n, nameof(n));

            if (n == 0)
            {
                return source;
            }

            return new ObservableSourceSkipLast<T>(source, n);
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
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> ConcatMapEager<T, R>(this IObservableSource<T> source, Func<T, IObservableSource<R>> mapper, int maxConcurrency = int.MaxValue, int capacityHint = 128)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequirePositive(maxConcurrency, nameof(maxConcurrency));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ObservableSourceConcatMapEager<T, R>(source, mapper, maxConcurrency, capacityHint);
        }

        /// <summary>
        /// Maps the upstream items to <see cref="IEnumerable{T}"/>s and emits their items in order.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The result value type</typeparam>
        /// <param name="source">The source observable.</param>
        /// <param name="mapper">The function that turns an upstream item into an enumerable sequence.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> ConcatMap<T, R>(this IObservableSource<T> source, Func<T, IEnumerable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new ObservableSourceConcatMapEnumerable<T, R>(source, mapper);
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
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> SwitchMap<T, R>(this IObservableSource<T> source, Func<T, IObservableSource<R>> mapper, bool delayErrors = false, int capacityHint = 128)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ObservableSourceSwitchMap<T, R>(source, mapper, delayErrors, capacityHint);
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
        /// <param name="others">The parameter array of alternate observables to use the latest values of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> WithLatestFrom<T, U, R>(
            this IObservableSource<T> source,
            Func<T, U[], R> mapper,
            params IObservableSource<U>[] others)
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
        /// <param name="others">The parameter array of alternate observables to use the latest values of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> WithLatestFrom<T, U, R>(
            this IObservableSource<T> source,
            Func<T, U[], R> mapper,
            bool delayErrors,
            params IObservableSource<U>[] others)
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
        /// <param name="others">The parameter array of alternate observables to use the latest values of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<R> WithLatestFrom<T, U, R>(
            this IObservableSource<T> source,
            Func<T, U[], R> mapper,
            bool delayErrors,
            bool sourceFirst,
            params IObservableSource<U>[] others)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));
            RequireNonNull(others, nameof(others));
            RequirePositive(others.Length, nameof(others) + ".Length");

            return new ObservableSourceWithLatestFrom<T, U, R>(source, others, mapper, delayErrors, sourceFirst);
        }

        /// <summary>
        /// Concatenates (appends) the other observable source after the
        /// main source.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The main source to run first.</param>
        /// <param name="other">The main source to run next.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<T> Concat<T>(this IObservableSource<T> source, IObservableSource<T> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new ObservableSourceConcatWith<T>(source, other);
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
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<T> RetryWhen<T, U>(this IObservableSource<T> source, Func<IObservableSource<Exception>, IObservableSource<U>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new ObservableSourceRetryWhen<T, U>(source, handler);
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
        /// <remarks>Since 0.0.21</remarks>
        public static IObservableSource<T> RepeatWhen<T, U>(this IObservableSource<T> source, Func<IObservableSource<object>, IObservableSource<U>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new ObservableSourceRepeatWhen<T, U>(source, handler);
        }

        /// <summary>
        /// Repeatedly re-subscribes to the source observable if the predicate
        /// function returns true upon the completion of the previous
        /// subscription.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to repeat.</param>
        /// <param name="predicate">Function to determine whether to repeat the <paramref name="source"/> or not.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Repeat<T>(this IObservableSource<T> source, Func<bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceRepeatPredicate<T>(source, predicate);
        }

        /// <summary>
        /// Repeatedly re-subscribes to the source observable if the predicate
        /// function returns true upon the completion of the previous
        /// subscription.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to repeat.</param>
        /// <param name="predicate">Function receiving the repeat count to determine whether to repeat the <paramref name="source"/> or not.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Repeat<T>(this IObservableSource<T> source, Func<long, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceRepeatPredicateCount<T>(source, predicate);
        }

        /// <summary>
        /// Repeatedly re-subscribes to the source observable if the predicate
        /// function returns true upon the completion of the previous
        /// subscription.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to repeat.</param>
        /// <param name="times">The number of times to repeat, zero means no subscription at all.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Repeat<T>(this IObservableSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(times, nameof(times));

            return new ObservableSourceRepeatCount<T>(source, times);
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
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Retry<T>(this IObservableSource<T> source, Func<Exception, long, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new ObservableSourceRetryPredicate<T>(source, predicate);
        }


        /// <summary>
        /// Repeatedly re-subscribes to the source observable if the predicate
        /// function returns true upon the failure of the previous
        /// subscription.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to repeat.</param>
        /// <param name="times">The number of times to resubscribe if the source failed,
        /// zero means no retry on failure.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Retry<T>(this IObservableSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(times, nameof(times));

            return new ObservableSourceRetryCount<T>(source, times);
        }


        /// <summary>
        /// Caches all upstream events and relays/replays it to current or
        /// late observers.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source"></param>
        /// <param name="capacityHint">The items are stored internally in a linked-array structure for 
        /// which this is the capacity hint for sizing those arrays: a trade-off between memory usage and
        /// locality of memory.</param>
        /// <param name="cancel">Called with the disposable when the source is subscribed on the first observer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<T> Cache<T>(this IObservableSource<T> source, int capacityHint = 16, Action<IDisposable> cancel = null)
        {
            RequireNonNull(source, nameof(source));
            RequirePositive(capacityHint, nameof(capacityHint));

            return new ObservableSourceCache<T>(source, cancel, capacityHint);
        }

        /// <summary>
        /// Multicasts the signals of the source observable sequence
        /// with the help of an observable subject for the
        /// duration of the handler function.
        /// </summary>
        /// <typeparam name="T">The upstream element type.</typeparam>
        /// <typeparam name="R">The result element type.</typeparam>
        /// <param name="source">The source to multicast.</param>
        /// <param name="subjectFactory">Returns the subject used for multicasting signals.</param>
        /// <param name="handler">Receives a shared observable
        /// source sequence and should return an observable
        /// sequence to be relayed to the downstream consumer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<R> Multicast<T, R>(this IObservableSource<T> source, Func<IObservableSubject<T>> subjectFactory, Func<IObservableSource<T>, IObservableSource<R>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(subjectFactory, nameof(subjectFactory));
            RequireNonNull(handler, nameof(handler));

            return new ObservableSourceMulticast<T, R>(source, subjectFactory, handler);
        }

        /// <summary>
        /// Multicasts the signals of the source observable sequence
        /// with the help of a connectable observable source for the
        /// duration of the handler function.
        /// </summary>
        /// <typeparam name="T">The upstream element type.</typeparam>
        /// <typeparam name="U">The element type of the connectable observable source.</typeparam>
        /// <typeparam name="R">The result element type.</typeparam>
        /// <param name="source">The source to multicast.</param>
        /// <param name="connectableSelector">Returns the connectable observable
        /// source to multicast signals with.</param>
        /// <param name="handler">Receives a shared observable
        /// source sequence and should return an observable
        /// sequence to be relayed to the downstream consumer.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<R> Multicast<T, U, R>(this IObservableSource<T> source, Func<IObservableSource<T>, IConnectableObservableSource<U>> connectableSelector, Func<IObservableSource<U>, IObservableSource<R>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(connectableSelector, nameof(connectableSelector));
            RequireNonNull(handler, nameof(handler));

            return new ObservableSourceMulticastConnect<T, U, R>(source, connectableSelector, handler);
        }

        /// <summary>
        /// Shares a single connection to the source observable
        /// and multicasts signals through a PublishSubject
        /// for the duration of the handler function.
        /// </summary>
        /// <typeparam name="T">The source element type.</typeparam>
        /// <typeparam name="R">The result element type.</typeparam>
        /// <param name="source">The observable sequence to share and publish.</param>
        /// <param name="handler">The function called for each
        /// individual observer with a multicasting observable source
        /// and should return an observable source sequence
        /// to be relayed to the downstream.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<R> Publish<T, R>(this IObservableSource<T> source, Func<IObservableSource<T>, IObservableSource<R>> handler)
        {
            return Multicast(source, () => new PublishSubject<T>(), handler);
        }

        /// <summary>
        /// Shares a single connection to the source observable
        /// and replays signals through a CacheSubject
        /// for the duration of the handler function.
        /// </summary>
        /// <typeparam name="T">The source element type.</typeparam>
        /// <typeparam name="R">The result element type.</typeparam>
        /// <param name="source">The observable sequence to share and publish.</param>
        /// <param name="handler">The function called for each
        /// individual observer with a multicasting observable source
        /// and should return an observable source sequence
        /// to be relayed to the downstream.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IObservableSource<R> Replay<T, R>(this IObservableSource<T> source, Func<IObservableSource<T>, IObservableSource<R>> handler)
        {
            return Multicast(source, () => new CacheSubject<T>(), handler);
        }

        /// <summary>
        /// Wraps the observable source and exposes it as
        /// a connectable observable source that multicasts
        /// items of it to multiple observers.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The source to multicast via a connectable observable source.</param>
        /// <returns>The new connectable observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IConnectableObservableSource<T> Publish<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourcePublish<T>(source);
        }

        /// <summary>
        /// Wraps the observable source and exposes it as
        /// a connectable observable source that replays
        /// items of it to multiple observers.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The source to multicast via a connectable observable source.</param>
        /// <returns>The new connectable observable source instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IConnectableObservableSource<T> Replay<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceReplay<T>(source);
        }

        // --------------------------------------------------------------
        // Consumer methods
        // --------------------------------------------------------------

        /// <summary>
        /// Creates a <see cref="TestObserver{T}"/> with the specified
        /// initial conditions, subscribes it to the source observable
        /// and returns this instance to perform assertions on.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The upstream observable source.</param>
        /// <param name="cancel">If true, the <see cref="TestObserver{T}"/> immediately cancels the upstream.</param>
        /// <param name="fusionMode">The requested fusion mode, see <see cref="FusionSupport"/> for the values.</param>
        /// <returns>The test observer instance</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static TestObserver<T> Test<T>(this IObservableSource<T> source, bool cancel = false, int fusionMode = 0)
        {
            RequireNonNull(source, nameof(source));

            var test = new TestObserver<T>(true, fusionMode);
            if (cancel)
            {
                test.Dispose();
            }
            source.Subscribe(test);
            return test;
        }

        /// <summary>
        /// Subscribes with a (subclass of a) <see cref="ISignalObserver{T}"/>
        /// to the source observable and returns this instance.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <typeparam name="S">The consumer subtype.</typeparam>
        /// <param name="source">The source sequence to observe.</param>
        /// <param name="observer">The observer to subscribe with and return.</param>
        /// <returns>The <paramref name="observer"/> itself.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static S SubscribeWith<T, S>(this IObservableSource<T> source, S observer) where S : ISignalObserver<T>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            source.Subscribe(observer);
            return observer;
        }

        /// <summary>
        /// Consume the upstream source sequence via (optional) lambda callbacks.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source sequence to consume.</param>
        /// <param name="onNext">Called with the next source item.</param>
        /// <param name="onError">Called with the exception of the upstream or the <paramref name="onNext"/> failure.</param>
        /// <param name="onCompleted">Called when the upstream terminates.</param>
        /// <param name="onSubscribe">Called with a disposable to allow disposing the sequence.</param>
        /// <returns>The disposable to stop the consumption.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IDisposable Subscribe<T>(this IObservableSource<T> source, Action<T> onNext = null, Action<Exception> onError = null, Action onCompleted = null, Action<IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            var observer = new LambdaSignalObserver<T>(onSubscribe, onNext, onError, onCompleted);
            source.Subscribe(observer);
            return observer;
        }

        /// <summary>
        /// Wraps the given <paramref name="observer"/> so that concurrent
        /// calls to the returned observer's OnNext, OnError or OnCompleted methods are serialized.
        /// </summary>
        /// <typeparam name="T">The element type of the flow</typeparam>
        /// <param name="observer">The observer to wrap and serialize signals for.</param>
        /// <returns>The serialized signal observer instance.</returns>
        public static ISignalObserver<T> ToSerialized<T>(this ISignalObserver<T> observer)
        {
            RequireNonNull(observer, nameof(observer));

            if (observer is SerializedSignalObserver<T> o)
            {
                return o;
            }

            return new SerializedSignalObserver<T>(observer);
        }


        /// <summary>
        /// Consumes the <paramref name="source"/> in a blocking fashion
        /// through an IEnumerable.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source observable to consume.</param>
        /// <returns>The new enumerable instance.</returns>
        /// <remarks>Since 0.0.22</remarks>
        public static IEnumerable<T> BlockingEnumerable<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceBlockingEnumerable<T>(source);
        }

        /// <summary>
        /// Consumes the source observable in a blocking fashion on
        /// the current thread and relays events to the given observer.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source observable to consume.</param>
        /// <param name="observer">The observer to call the OnXXX methods</param>
        /// <remarks>Since 0.0.22</remarks>
        public static void BlockingSubscribe<T>(this IObservableSource<T> source, ISignalObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            var consumer = new BlockingSubscribeSignalObserver<T>(observer);
            source.Subscribe(consumer);
            consumer.Run();
        }

        /// <summary>
        /// Consumes the source observable in a blocking fashion on
        /// the current thread and relays events to the given callback actions.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source observable to consume.</param>
        /// <param name="onNext">The action called with the next item.</param>
        /// <param name="onError">The action called with the error.</param>
        /// <param name="onComplete">The action called when the sequence completes.</param>
        /// <param name="dispose">The callback with the IDisposable to allow external cancellation.</param>
        /// <remarks>Since 0.0.22</remarks>
        public static void BlockingSubscribe<T>(this IObservableSource<T> source, Action<T> onNext, Action<Exception> onError = null, Action onComplete = null, Action<IDisposable> dispose = null)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(onNext, nameof(onNext));

            var consumer = new BlockingSubscribeSignalAction<T>(onNext, onError, onComplete);
            dispose?.Invoke(consumer);
            source.Subscribe(consumer);
            consumer.Run();
        }

        /// <summary>
        /// Consumes the source observable in a blocking fashion on
        /// the current thread and relays events to the given callback actions
        /// while the <paramref name="onNext"/> predicate returns true.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The source observable to consume.</param>
        /// <param name="onNext">The action called with the next item and should return true to keep
        /// the source running.</param>
        /// <param name="onError">The action called with the error.</param>
        /// <param name="onComplete">The action called when the sequence completes.</param>
        /// <param name="dispose">The callback with the IDisposable to allow external cancellation.</param>
        /// <remarks>Since 0.0.22</remarks>
        public static void BlockingSubscribeWhile<T>(this IObservableSource<T> source, Func<T, bool> onNext, Action<Exception> onError = null, Action onComplete = null, Action<IDisposable> dispose = null)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(onNext, nameof(onNext));

            var consumer = new BlockingSubscribeSignalPredicate<T>(onNext, onError, onComplete);
            dispose?.Invoke(consumer);
            source.Subscribe(consumer);
            consumer.Run();
        }

        // --------------------------------------------------------------
        // Interoperation methods
        // --------------------------------------------------------------

        /// <summary>
        /// Emits the elements of the array to observers.
        /// </summary>
        /// <typeparam name="T">The element type of the source array.</typeparam>
        /// <param name="array">The array whose elements to emit.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static IObservableSource<T> ToObservableSource<T>(this T[] array)
        {
            RequireNonNull(array, nameof(array));
            // TODO handle corner cases of Length == 0 and Length == 1

            return new ObservableSourceArray<T>(array);
        }

        /// <summary>
        /// Convert a legacy IObservable into an observable source.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The observable to convert into an observable source.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> ToObservableSource<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceFromObservable<T>(source);
        }

        /// <summary>
        /// Convert an IObservableSource into a legacy IObservable.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The observable source to convert into a legacy observable sequence.</param>
        /// <returns>The new legacy observable instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservable<T> ToObservable<T>(this IObservableSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceToObservable<T>(source);
        }

        /// <summary>
        /// Convert an enumerable sequence into an observable source.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The enumerable to convert into an observable source.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.18</remarks>
        public static IObservableSource<T> ToObservableSource<T>(this IEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new ObservableSourceEnumerable<T>(source);
        }

        /// <summary>
        /// Converts a Task and signals its terminal event to observers.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="task">The task to wrap and signal terminal events of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<T> ToObservableSource<T>(this Task task)
        {
            RequireNonNull(task, nameof(task));

            return new ObservableSourceFromTask<T>(task);
        }

        /// <summary>
        /// Converts a Task and signals its terminal value or error to observers.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="task">The task to wrap and signal terminal events of.</param>
        /// <returns>The new observable source instance.</returns>
        /// <remarks>Since 0.0.19</remarks>
        public static IObservableSource<T> ToObservableSource<T>(this Task<T> task)
        {
            RequireNonNull(task, nameof(task));

            return new ObservableSourceFromTaskValue<T>(task);
        }

        /// <summary>
        /// Automatically connect the upstream IConnectableObservable at most once when the
        /// specified number of IObservers have subscribed to this IObservable.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The connectable observable sequence.</param>
        /// <param name="minObservers">The number of observers required to subscribe before the connection to source happens, non-positive value will trigger an immediate subscription.</param>
        /// <param name="onConnect">If not null, the connection's IDisposable is provided to it.</param>
        /// <returns>An observable source sequence that connects to the source at most once when the given number of observers have subscribed to it.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IObservableSource<T> AutoConnect<T>(this IConnectableObservableSource<T> source, int minObservers = 1, Action<IDisposable> onConnect = null)
        {
            RequireNonNull(source, nameof(source));
            if (minObservers <= 0)
            {
                var d = source.Connect();
                onConnect?.Invoke(d);
                return source;
            }
            return new ObservableSourceAutoConnect<T>(source, minObservers, onConnect);
        }
    }
}
