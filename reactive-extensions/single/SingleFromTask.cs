using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Relays the terminal event from a wrapped task.
    /// </summary>
    /// <typeparam name="T">The value type of the task (ignored)</typeparam>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class SingleFromTask<T> : ISingleSource<T>
    {
        readonly Task<T> task;

        static readonly Action<Task<T>, object> TASK =
            (task, self) => ((TaskDisposable)self).Run(task);

        public SingleFromTask(Task<T> task)
        {
            this.task = task;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new TaskDisposable(observer);
            observer.OnSubscribe(parent);

            task.ContinueWith(TASK, parent);
        }

        internal sealed class TaskDisposable : IDisposable
        {
            internal ISingleObserver<T> downstream;

            public TaskDisposable(ISingleObserver<T> downstream)
            {
                Volatile.Write(ref this.downstream, downstream);
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref this.downstream, null);
            }

            internal void Run(Task<T> task)
            {
                var ex = task.Exception;
                if (ex == null)
                {
                    Volatile.Read(ref downstream)?.OnSuccess(task.Result);
                }
                else
                {
                    Volatile.Read(ref downstream)?.OnError(ex);
                }
            }
        }
    }
}
