using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Base interface for observing signals.<br/>
    /// The protocol is:<br/>
    /// <code>OnSubscribe OnNext* (OnError|OnCompleted)?</code>
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IObservableSource<out T>
    {
        void Subscribe(ISignalObserver<T> observer);
    }

    /// <summary>
    /// Base interface for consuming <see cref="IObservableSource{T}"/>s.
    /// The protocol is:<br/>
    /// <code>OnSubscribe OnNext* (OnError|OnCompleted)?</code>
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface ISignalObserver<in T>
    {
        void OnSubscribe(IDisposable d);

        void OnNext(T item);

        void OnError(Exception ex);

        void OnCompleted();
    }

    /// <summary>
    /// Abstraction over an <see cref="ISignalObserver{T}"/> that
    /// allows signaling the terminal state via safe
    /// method calls.
    /// </summary>
    /// <remarks>Since 0.0.17</remarks>
    public interface ISignalEmitter<in T>
    {
        void OnNext(T item);

        void OnError(Exception ex);

        void OnCompleted();

        void SetResource(IDisposable d);

        bool IsDisposed();
    }

    /// <summary>
    /// A basic queue interface for getting out items,
    /// checking for emptyness and clearing the queue.
    /// </summary>
    /// <typeparam name="T">The element type queued.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IQueueConsumer<out T>
    {
        T TryPoll(out bool success);

        bool IsEmpty();

        void Clear();
    }

    /// <summary>
    /// A basic queue implementation with production
    /// and consumption options.
    /// </summary>
    /// <typeparam name="T">The element type of the queue.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface ISimpleQueue<T> : IQueueConsumer<T>
    {
        bool TryOffer(T item);
    }

    /// <summary>
    /// Represents a fuseable source.
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IFuseableDisposable<out T> : IQueueConsumer<T>, IDisposable
    {
        int RequestFusion(int mode);
    }

    /// <summary>
    /// Constants for the <see cref="IFuseableDisposable{T}.RequestFusion(int)"/>
    /// parameter and return value.
    /// </summary>
    /// <remarks>Since 0.0.17</remarks>
    public static class FusionSupport
    {
        public static readonly int None = 0;
        public static readonly int Sync = 1;
        public static readonly int Async = 2;
        public static readonly int Barrier = 4;
    }

    /// <summary>
    /// Indicator interface for a reactive source
    /// indicating it can return a scalar value upon
    /// subscription directly.
    /// </summary>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IDynamicValue<out T>
    {
        T GetValue(out bool success);
    }

    /// <summary>
    /// An indicator interface for a reactive source
    /// indicating it can return a scalar value (constant) during
    /// flow assembly.
    /// </summary>
    /// <typeparam name="T">The scalar value type.</typeparam>
    public interface IStaticValue<out T> : IDynamicValue<T>
    {
    }

}
