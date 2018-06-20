using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableFromTask<T> : IAsyncEnumerable<T>
    {
        readonly Task<T> task;

        public AsyncEnumerableFromTask(Task<T> task)
        {
            this.task = task;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromTaskAsyncEnumerator(task);
        }

        sealed class FromTaskAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly Task<T> task;

            public T Current => task.Result;

            bool once;

            public FromTaskAsyncEnumerator(Task<T> task)
            {
                this.task = task;
            }

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }

            public async Task<bool> MoveNextAsync()
            {
                if (once)
                {
                    return false;
                }
                once = true;

                await task;
                return true;
            }
        }
    }

    internal sealed class AsyncEnumerableFromTaskPlain<T> : IAsyncEnumerable<T>
    {
        readonly Task task;

        public AsyncEnumerableFromTaskPlain(Task task)
        {
            this.task = task;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FromTaskAsyncEnumerator(task);
        }

        sealed class FromTaskAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly Task task;

            public T Current => default;

            bool once;

            public FromTaskAsyncEnumerator(Task task)
            {
                this.task = task;
            }

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }

            public async Task<bool> MoveNextAsync()
            {
                if (!once)
                {
                    once = true;
                    await task;
                }

                return false;
            }
        }
    }
}
