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

        // --------------------------------------------------------------
        // Instance methods
        // --------------------------------------------------------------

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
            source.Subscribe(observer);
            return observer;
        }

        // --------------------------------------------------------------
        // Interoperation methods
        // --------------------------------------------------------------
    }
}
