using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceIgnoreElements<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        public ObservableSourceIgnoreElements(IObservableSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new IgnoreElementsObserver(observer));
        }

        sealed class IgnoreElementsObserver : IFuseableDisposable<T>, ISignalObserver<T>
        {
            readonly ISignalObserver<T> downstream;

            IDisposable upstream;

            public IgnoreElementsObserver(ISignalObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public void OnNext(T item)
            {
                // deliberately ignored
            }

            public void OnError(Exception ex)
            {
                downstream.OnError(ex);
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public int RequestFusion(int mode)
            {
                return mode & FusionSupport.Async;
            }

            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called");
            }

            public T TryPoll(out bool success)
            {
                success = false;
                return default(T);
            }

            public bool IsEmpty()
            {
                return true;
            }

            public void Clear()
            {
                // nothing to clear
            }

            public void Dispose()
            {
                upstream.Dispose();
            }
        }
    }
}
