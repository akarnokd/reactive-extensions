using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Represents a source that signals items and/or terminal events to an
    /// asynchronous consumer and awaits the asynchronous processing of such
    /// signals in the consumer.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    /// <remarks>Since 0.0.27</remarks>
    public interface IAsyncSource<out T>
    {
        Task<IAsyncCloseable> SubscribeAsync(IAsyncConsumer<T> consumer);    
    }

    /// <summary>
    /// Consumes push-events from a <see cref="IAsyncSource{T}"/> and
    /// returns a <see cref="Task"/> to indicate when this consumer can accept
    /// the next event.
    /// The caller should await the returned Tasks before signaling the next signal.
    /// </summary>
    /// <typeparam name="T">The element type consumed by this async consumer.</typeparam>
    /// <remarks>Since 0.0.27</remarks>
    public interface IAsyncConsumer<in T>
    {
        Task OnNextAsync(T item);

        Task OnErrorAsync(Exception exception);

        Task OnCompletedAsync();
    }

    /// <summary>
    /// Close a resource/connection/subscription asynchronously.
    /// </summary>
    /// <remarks>Since 0.0.27</remarks>
    public interface IAsyncCloseable
    {
        Task CloseAsync();
    }

    /// <summary>
    /// An async source and consumer at the same time.
    /// </summary>
    /// <typeparam name="T">The input and output value type of the async sequence.</typeparam>
    /// <remarks>Since 0.0.27</remarks>
    public interface IAsyncSubject<T> : IAsyncSource<T>, IAsyncConsumer<T>, IAsyncCloseable
    {

    }

    /// <summary>
    /// An async source with a key value associated with it.
    /// </summary>
    /// <typeparam name="T">The element type of the async sequence.</typeparam>
    /// <typeparam name="K">The key type.</typeparam>
    /// <remarks>Since 0.0.27</remarks>
    public interface IGroupedAsyncSource<T, K> : IAsyncSource<T>
    {
        K Key { get; }
    }

    /// <summary>
    /// Schedules work to be executed asynchronously.
    /// </summary>
    /// <remarks>Since 0.0.27</remarks>
    public interface IAsyncExecutor
    {
        Task<IAsyncCloseable> ScheduleAsync(Func<CancellationToken, Task> work);

        Task<IAsyncCloseable> ScheduleAsync(Func<CancellationToken, Task> work, TimeSpan delay);
    }
}
