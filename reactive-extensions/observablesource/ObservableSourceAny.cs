using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceAny<T> : IObservableSource<bool>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, bool> predicate;

        public ObservableSourceAny(IObservableSource<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISignalObserver<bool> observer)
        {
            source.Subscribe(new AnyObserver(observer, predicate));
        }

        sealed class AnyObserver : DeferredScalarDisposable<bool>, ISignalObserver<T>
        {
            readonly Func<T, bool> predicate;

            IDisposable upstream;

            internal AnyObserver(ISignalObserver<bool> downstream, Func<T, bool> predicate) : base(downstream)
            {
                this.predicate = predicate;
            }

            public void OnCompleted()
            {
                Complete(false);
            }

            public void OnError(Exception ex)
            {
                Error(ex);
            }

            public void OnNext(T item)
            {
                var result = false;
                try
                {
                    result = predicate(item);
                }
                catch (Exception ex)
                {
                    upstream.Dispose();
                    Error(ex);
                    return;
                }

                if (result)
                {
                    upstream.Dispose();
                    Complete(true);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public override void Dispose()
            {
                base.Dispose();
                upstream.Dispose();
            }
        }
    }
}
