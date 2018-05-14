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
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableFromTask : ICompletableSource
    {
        readonly Task task;

        static readonly Action<Task, object> TASK =
            (task, self) => ((TaskDisposable)self).Run(task.Exception);

        public CompletableFromTask(Task task)
        {
            this.task = task;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new TaskDisposable(observer);
            observer.OnSubscribe(parent);

            task.ContinueWith(TASK, parent);
        }

        internal sealed class TaskDisposable : IDisposable
        {
            internal ICompletableObserver downstream;

            public TaskDisposable(ICompletableObserver downstream)
            {
                Volatile.Write(ref this.downstream, downstream);
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref this.downstream, null);
            }

            internal void Run(Exception ex)
            {
                if (ex == null)
                {
                    Volatile.Read(ref downstream)?.OnCompleted();
                }
                else
                {
                    Volatile.Read(ref downstream)?.OnError(ex);
                }
            }
        }
    }

    /// <summary>
    /// Relays the terminal event from a wrapped task.
    /// </summary>
    /// <typeparam name="T">The value type of the task (ignored)</typeparam>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableFromTask<T> : ICompletableSource
    {
        readonly Task<T> task;

        static readonly Action<Task<T>, object> TASK =
            (task, self) => ((CompletableFromTask.TaskDisposable)self).Run(task.Exception);

        public CompletableFromTask(Task<T> task)
        {
            this.task = task;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new CompletableFromTask.TaskDisposable(observer);
            observer.OnSubscribe(parent);

            task.ContinueWith(TASK, parent);
        }
    }
}
