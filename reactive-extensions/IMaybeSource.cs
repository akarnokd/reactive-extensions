using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A reactive base type that only succeeds, completes as empty or signals error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError|OnCompleted)?</code> protocol.
    /// </summary>
    public interface IMaybeSource<out T>
    {
        void Subscribe(IMaybeObserver<T> observer);
    }

    /// <summary>
    /// A consumer of <see cref="IMaybeSource{T}"/>s which only
    /// succeeds with one item, completes as empty or signals an error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError|OnCompleted)?</code> protocol.
    /// </summary>
    public interface IMaybeObserver<in T>
    {
        void OnSubscribe(IDisposable d);

        void OnSuccess(T item);

        void OnError(Exception error);

        void OnCompleted();
    }
}
