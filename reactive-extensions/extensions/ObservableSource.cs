using System;
using System.Collections.Generic;
using System.Text;

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

        // --------------------------------------------------------------
        // Instance methods
        // --------------------------------------------------------------

        // --------------------------------------------------------------
        // Consumer methods
        // --------------------------------------------------------------

        public static TestObserver<T> Test<T>(this IObservableSource<T> source, bool cancel = false)
        {
            var test = new TestObserver<T>(true);
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
