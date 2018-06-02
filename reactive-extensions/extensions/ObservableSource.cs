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

        public static IObservableSource<T> Just<T>(T item)
        {
            return new ObservableSourceJust<T>(item);
        }

        public static IObservableSource<T> Empty<T>()
        {
            return ObservableSourceEmpty<T>.Instance;
        }

        public static IObservableSource<T> Never<T>()
        {
            return ObservableSourceNever<T>.Instance;
        }

        public static IObservableSource<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new ObservableSourceError<T>(error);
        }

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
