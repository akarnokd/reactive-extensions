using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension and factory methods for dealing with
    /// <see cref="IMaybeSource{T}"/>s.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public static class MaybeSource
    {
        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> maybe.
        /// </summary>
        /// <typeparam name="T">The value type of the source maybe.</typeparam>
        /// <param name="source">The source maybe to test.</param>
        /// <param name="dispose">Dispose the TestObserver before the subscription happens</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this IMaybeSource<T> source, bool dispose = false)
        {
            RequireNonNull(source, nameof(source));
            var to = new TestObserver<T>();
            if (dispose)
            {
                to.Dispose();
            }
            source.Subscribe(to);
            return to;
        }

        //-------------------------------------------------
        // Factory methods
        //-------------------------------------------------

        /// <summary>
        /// Creates a maybe that calls the specified <paramref name="onSubscribe"/>
        /// action with a <see cref="IMaybeEmitter{T}"/> to allow
        /// bridging the callback world with the reactive world.
        /// </summary>
        /// <param name="onSubscribe">The action that is called with an emitter
        /// that can be used for signalling an item, completion or error event.</param>
        /// <returns>The new maybe instance</returns>
        public static IMaybeSource<T> Create<T>(Action<IMaybeEmitter<T>> onSubscribe)
        {
            RequireNonNull(onSubscribe, nameof(onSubscribe));

            return new MaybeCreate<T>(onSubscribe);
        }

        /// <summary>
        /// Creates an empty completable that completes immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <returns>The shared empty completable instance.</returns>
        public static IMaybeSource<T> Empty<T>()
        {
            return MaybeEmpty<T>.INSTANCE;
        }

        /// <summary>
        /// Creates a failing completable that signals the specified error
        /// immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <param name="error">The error to signal.</param>
        /// <returns>The new completable instance.</returns>
        public static IMaybeSource<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new MaybeError<T>(error);
        }

        /// <summary>
        /// Creates a maybe that never terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <returns>The shared never-terminating maybe instance.</returns>
        public static IMaybeSource<T> Never<T>()
        {
            return MaybeNever<T>.INSTANCE;
        }

        //-------------------------------------------------
        // Instance methods
        //-------------------------------------------------

        // ------------------------------------------------
        // Leaving the reactive world
        // ------------------------------------------------

        public static void SubscribeSafe<T>(this IMaybeSource<T> source, IMaybeObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IDisposable Subscribe<T>(this IMaybeSource<T> source, Action<T> onSuccess, Action<Exception> onError = null, Action onCompleted = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe<T>(this IMaybeSource<T> source, IMaybeObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe<T>(this IMaybeSource<T> source, Action<T> onSuccess, Action<Exception> onError = null, Action onCompleted = null, Action <IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void Wait<T>(this IMaybeSource<T> source, long timeoutMillis = long.MinValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        //-------------------------------------------------
        // Interoperation with other reactive types
        //-------------------------------------------------

    }
}
