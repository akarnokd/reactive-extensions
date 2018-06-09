using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourcePublish<T> : IConnectableObservableSource<T>
    {
        readonly IObservableSource<T> source;

        PublishSubject<T> connection;

        public ObservableSourcePublish(IObservableSource<T> source)
        {
            this.source = source;
        }

        public IDisposable Connect(Action<IDisposable> onConnect = null)
        {
            for (; ; )
            {
                var subject = Volatile.Read(ref connection);
                if (subject == null)
                {
                    subject = new PublishSubject<T>();
                    if (Interlocked.CompareExchange(ref connection, subject, null) != null)
                    {
                        continue;
                    }
                }
                else
                {
                    if (subject.HasException() || subject.HasCompleted())
                    {
                        Interlocked.CompareExchange(ref connection, null, subject);
                        continue;
                    }
                }

                var shouldConnect = subject.Prepare();

                onConnect?.Invoke(subject);

                if (shouldConnect)
                {
                    source.Subscribe(subject);
                }

                return subject;
            }
        }

        public void Reset()
        {
            var subject = Volatile.Read(ref connection);
            if (subject != null) {
                if (subject.HasException() || subject.HasCompleted())
                {
                    Interlocked.CompareExchange(ref connection, null, subject);
                }
            }
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            for (; ; )
            {
                var subject = Volatile.Read(ref connection);
                if (subject == null)
                {
                    subject = new PublishSubject<T>();
                    if (Interlocked.CompareExchange(ref connection, subject, null) != null)
                    {
                        continue;
                    }
                }

                subject.Subscribe(observer);

                break;
            }
        }
    }
}
