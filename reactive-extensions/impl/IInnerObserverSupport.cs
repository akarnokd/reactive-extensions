using System;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Callback methods for handling signals of individual inner observers.
    /// </summary>
    /// <typeparam name="U">The item types of the inner observers</typeparam>
    internal interface IInnerObserverSupport<U>
    {
        void InnerNext(InnerObserver<U> sender, U item);

        void InnerError(InnerObserver<U> sender, Exception error);

        void InnerComplete(InnerObserver<U> sender);
    }
}
