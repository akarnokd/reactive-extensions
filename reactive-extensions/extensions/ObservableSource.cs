using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
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
        /// <param name="observer">The observer to subscibe with and return.</param>
        /// <returns>The <paramref name="observer"/> itself.</returns>
        /// <remarks>Since 0.0.17</remarks>
        public static S SubscribeWith<T, S>(this IObservableSource<T> source, S observer) where S : ISignalObserver<T>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

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
    }
}
