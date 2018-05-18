using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Switches to a fallback completable source if
    /// the upstream fails.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableOnErrorResumeNext : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly ICompletableSource fallback;

        public CompletableOnErrorResumeNext(ICompletableSource source, ICompletableSource fallback)
        {
            this.source = source;
            this.fallback = fallback;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new OnErrorResumeNextObserver(observer, fallback));
        }

        sealed class OnErrorResumeNextObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            IDisposable upstream;

            ICompletableSource fallback;

            IDisposable fallbackObserver;

            public OnErrorResumeNextObserver(ICompletableObserver downstream, ICompletableSource fallback)
            {
                this.downstream = downstream;
                this.fallback = fallback;
            }

            public void Dispose()
            {
                upstream.Dispose();
                DisposableHelper.Dispose(ref fallbackObserver);
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                var inner = new CompletableInnerObserver(downstream);
                if (Interlocked.CompareExchange(ref fallbackObserver, inner, null) == null)
                {
                    var fb = fallback;
                    fallback = null;

                    fb.Subscribe(inner);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }

    /// <summary>
    /// Switches to a fallback completable source if
    /// the upstream fails.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableOnErrorResumeNextSelector : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly Func<Exception, ICompletableSource> handler;

        public CompletableOnErrorResumeNextSelector(ICompletableSource source, Func<Exception, ICompletableSource> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new OnErrorResumeNextObserver(observer, handler));
        }

        sealed class OnErrorResumeNextObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            readonly Func<Exception, ICompletableSource> handler;

            IDisposable upstream;

            IDisposable fallbackObserver;

            public OnErrorResumeNextObserver(ICompletableObserver downstream, Func<Exception, ICompletableSource> handler)
            {
                this.downstream = downstream;
                this.handler = handler;
            }

            public void Dispose()
            {
                upstream.Dispose();
                DisposableHelper.Dispose(ref fallbackObserver);
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                var fb = default(ICompletableSource);

                try
                {
                    fb = RequireNonNullRef(handler(error), "The handler returned a null ICompletableSource");
                }
                catch (Exception ex)
                {
                    downstream.OnError(new AggregateException(error, ex));
                    return;
                }

                var inner = new CompletableInnerObserver(downstream);
                if (Interlocked.CompareExchange(ref fallbackObserver, inner, null) == null)
                {
                    fb.Subscribe(inner);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
