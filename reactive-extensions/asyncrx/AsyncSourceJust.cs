using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncSourceJust<T> : IAsyncSource<T>
    {
        readonly T item;

        public AsyncSourceJust(T item)
        {
            this.item = item;
        }

        public async Task<IAsyncCloseable> SubscribeAsync(IAsyncConsumer<T> consumer)
        {
            await consumer.OnNextAsync(item).ConfigureAwait(false);
            await consumer.OnCompletedAsync().ConfigureAwait(false);
            return AsyncCloseable.Empty;
        }
    }

    internal sealed class AsyncSourceJustExecutor<T> : IAsyncSource<T>
    {
        readonly T item;

        readonly IAsyncExecutor executor;

        public AsyncSourceJustExecutor(T item, IAsyncExecutor executor)
        {
            this.item = item;
            this.executor = executor;
        }

        public Task<IAsyncCloseable> SubscribeAsync(IAsyncConsumer<T> consumer)
        {
            return executor.ScheduleAsync(async ct => {
                if (ct.IsCancellationRequested) return;

                await consumer.OnNextAsync(item).ThenSchedule(ct, executor);

                if (ct.IsCancellationRequested) return;

                await consumer.OnCompletedAsync().ThenSchedule(ct, executor);
            });
        }
    }
}
