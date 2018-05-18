using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Suppresses an upstream error and completes the completable observer
    /// instead.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableOnErrorComplete : ICompletableSource
    {
        readonly ICompletableSource source;

        public CompletableOnErrorComplete(ICompletableSource source)
        {
            this.source = source;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new OnErrorCompleteObserver(observer));
        }

        sealed class OnErrorCompleteObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            IDisposable upstream;

            public OnErrorCompleteObserver(ICompletableObserver downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                downstream.OnCompleted();
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
