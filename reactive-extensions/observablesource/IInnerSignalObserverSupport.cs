using System;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Callback methods for handling signals of individual inner observers.
    /// </summary>
    /// <typeparam name="U">The item types of the inner observers</typeparam>
    internal interface IInnerSignalObserverSupport<U>
    {
        void InnerNext(InnerSignalObserver<U> sender, U item);

        void InnerError(InnerSignalObserver<U> sender, Exception error);

        void InnerComplete(InnerSignalObserver<U> sender);

        void Drain();
    }
}
