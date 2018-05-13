using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension and factory methods for dealing with
    /// <see cref="ISingleSource{T}"/>s.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public static class SingleSource
    {
        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> single.
        /// </summary>
        /// <typeparam name="T">The value type of the source single.</typeparam>
        /// <param name="source">The source single to test.</param>
        /// <param name="dispose">Dispose the TestObserver before the subscription happens</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this ISingleSource<T> source, bool dispose = false)
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
        /// Creates a single that calls the specified <paramref name="onSubscribe"/>
        /// action with a <see cref="ISingleEmitter{T}"/> to allow
        /// bridging the callback world with the reactive world.
        /// </summary>
        /// <param name="onSubscribe">The action that is called with an emitter
        /// that can be used for signalling an item or error event.</param>
        /// <returns>The new single instance</returns>
        public static ISingleSource<T> Create<T>(Action<ISingleEmitter<T>> onSubscribe)
        {
            RequireNonNull(onSubscribe, nameof(onSubscribe));

            return new SingleCreate<T>(onSubscribe);
        }

        /// <summary>
        /// Creates a failing completable that signals the specified error
        /// immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <param name="error">The error to signal.</param>
        /// <returns>The new completable instance.</returns>
        public static ISingleSource<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new SingleError<T>(error);
        }

        /// <summary>
        /// Creates a single that never terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the single.</typeparam>
        /// <returns>The shared never-terminating single instance.</returns>
        public static ISingleSource<T> Never<T>()
        {
            return SingleNever<T>.INSTANCE;
        }

        //-------------------------------------------------
        // Instance methods
        //-------------------------------------------------

        // ------------------------------------------------
        // Leaving the reactive world
        // ------------------------------------------------

        public static void SubscribeSafe<T>(this ISingleSource<T> source, ISingleObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IDisposable Subscribe<T>(this ISingleSource<T> source, Action<T> onSuccess, Action<Exception> onError = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe<T>(this ISingleSource<T> source, ISingleObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe<T>(this ISingleSource<T> source, Action<T> onSuccess, Action<Exception> onError = null, Action<IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void Wait<T>(this ISingleSource<T> source, long timeoutMillis = long.MinValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        //-------------------------------------------------
        // Interoperation with other reactive types
        //-------------------------------------------------


    }
}
