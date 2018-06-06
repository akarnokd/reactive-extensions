using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceFromTask<T> : IObservableSource<T>
    {
        readonly Task task;

        public ObservableSourceFromTask(Task task)
        {
            this.task = task;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new FromTaskDisposable(observer);
            observer.OnSubscribe(parent);

            task.ContinueWith((t, o) => (parent as FromTaskDisposable).Handle(t), parent);
        }

        sealed class FromTaskDisposable : IFuseableDisposable<T>
        {
            ISignalObserver<T> observer;

            public FromTaskDisposable(ISignalObserver<T> observer)
            {
                Volatile.Write(ref this.observer, observer);
            }

            internal void Handle(Task t)
            {
                if (t.IsCanceled)
                {
                    observer?.OnError(new OperationCanceledException());
                }
                else
                if (t.IsFaulted)
                {
                    observer?.OnError(t.Exception);
                }
                else
                {
                    observer?.OnCompleted();
                }
            }

            public void Clear()
            {
                // always empty
            }

            public void Dispose()
            {
                Volatile.Write(ref observer, null);
            }

            public bool IsEmpty()
            {
                return true;
            }

            public int RequestFusion(int mode)
            {
                return mode & FusionSupport.Async;
            }

            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called!");
            }

            public T TryPoll(out bool success)
            {
                success = false;
                return default;
            }
        }
    }

    internal sealed class ObservableSourceFromTaskValue<T> : IObservableSource<T>
    {
        readonly Task<T> task;

        public ObservableSourceFromTaskValue(Task<T> task)
        {
            this.task = task;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new FromTaskDisposable(observer);
            observer.OnSubscribe(parent);

            task.ContinueWith((t, o) => (parent as FromTaskDisposable).Handle(t), parent);
        }

        sealed class FromTaskDisposable : DeferredScalarDisposable<T>
        {
            public FromTaskDisposable(ISignalObserver<T> observer) : base(observer)
            {
            }

            internal void Handle(Task<T> t)
            {
                if (t.IsCanceled)
                {
                    Error(new OperationCanceledException());
                }
                else
                if (t.IsFaulted)
                {
                    Error(t.Exception);
                }
                else
                {
                    Complete(t.Result);
                }
            }
        }
    }
}
