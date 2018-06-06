﻿using System;
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
    }
}
