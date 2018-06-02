using System;
using System.Collections.Generic;
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
    }
}
