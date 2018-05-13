using System;
using System.Collections.Generic;
using System.Text;
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
        /// <param name="dispose">Dispose </param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this ISingleSource<T> source, bool dispose = true)
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

    }
}
