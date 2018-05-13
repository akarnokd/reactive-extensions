using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A reactive base type that only succeeds or signals error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError)?</code> protocol.
    /// </summary>
    public interface ISingleSource<out T>
    {
        void Subscribe(ISingleObserver<T> observer);
    }

    /// <summary>
    /// A consumer of <see cref="ISingleSource{T}"/>s which only
    /// succeeds with one item or signals an error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError)?</code> protocol.
    /// </summary>
    public interface ISingleObserver<in T>
    {
        void OnSubscribe(IDisposable d);

        void OnSuccess(T item);

        void OnError(Exception error);
    }
}
