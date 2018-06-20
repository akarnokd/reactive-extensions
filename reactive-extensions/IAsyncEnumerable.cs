using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// An enumerable type that can produce async
    /// enumerators that produce items one-by-one
    /// in an asynchronous manner.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    /// <remarks>Since 0.0.25</remarks>
    public interface IAsyncEnumerable<out T>
    {
        /// <summary>
        /// Get an async enumerator for this enumerable.
        /// </summary>
        /// <returns>The async enumerator instance.</returns>
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    /// <summary>
    /// An async enumerator that can produce items
    /// one-by-one upon calling MoveNextAsync and
    /// when its returned task indicates true.
    /// Whenever the enumerator is no longer needed,
    /// one should call <see cref="IAsyncDisposable.DisposeAsync"/>
    /// on it, even after <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>
    /// signaled false.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    /// <remarks>Since 0.0.25</remarks>
    public interface IAsyncEnumerator<out T> : IAsyncDisposable
    {
        /// <summary>
        /// Try generating the next item asynchronously,
        /// if the returned task signals false, no more items
        /// are available
        /// </summary>
        /// <returns></returns>
        Task<bool> MoveNextAsync();

        /// <summary>
        /// Returns the current item if <see cref="MoveNextAsync"/> signaled a true task.
        /// </summary>
        T Current { get; }
    }

    /// <summary>
    /// Dispose a resource asynchronously.
    /// </summary>
    /// <remarks>Since 0.0.25</remarks>
    public interface IAsyncDisposable
    {
        /// <summary>
        /// Trigger the asynchronous disposal
        /// and allow waiting for it to finish.
        /// </summary>
        /// <returns>The task that can be awaited.</returns>
        Task DisposeAsync();
    }

    /// <summary>
    /// Marker interface if the IAsyncEnumerator supports
    /// synchronous polling for the next item.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    public interface IAsyncFusedEnumerator<out T>
    {
        /// <summary>
        /// Try getting the next item if available
        /// or throws in case of an upstream exception.
        /// Calling <see cref="IAsyncEnumerator{T}.MoveNextAsync()"/> before this
        /// method is optional but is recommended if the state outcome
        /// is <see cref="AsyncFusedState.Empty"/> as only the <code>MoveNextAsync</code>
        /// is expected to trigger the generation of more items.
        /// </summary>
        /// <param name="state">The outcome of the attempt: no item,
        /// item polled or no further data.</param>
        /// <returns>The next item if <paramref name="state"/> is <see cref="AsyncFusedState.Ready"/></returns>
        T TryPoll(out AsyncFusedState state);
    }

    /// <summary>
    /// Represents the outcome for the <see cref="IAsyncFusedEnumerator{T}.TryPoll(out AsyncFusedState)"/>
    /// call.
    /// </summary>
    public enum AsyncFusedState
    {
        /// <summary>
        /// No item is currently available and the caller
        /// should try with <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>.
        /// </summary>
        Empty = 0,
        /// <summary>
        /// An item is currently available and has been returned.
        /// </summary>
        Ready = 1,
        /// <summary>
        /// The upstream has no further items at all (so <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>
        /// would signal false anyway).
        /// </summary>
        Terminated = 2
    }
}
