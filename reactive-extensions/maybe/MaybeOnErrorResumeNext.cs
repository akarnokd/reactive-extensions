using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Switches to a fallback maybe source if
    /// the upstream fails.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeOnErrorResumeNext<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly IMaybeSource<T> fallback;

        public MaybeOnErrorResumeNext(IMaybeSource<T> source, IMaybeSource<T> fallback)
        {
            this.source = source;
            this.fallback = fallback;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new OnErrorResumeNextObserver(observer, fallback));
        }

        sealed class OnErrorResumeNextObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IDisposable upstream;

            IMaybeSource<T> fallback;

            IDisposable fallbackObserver;

            public OnErrorResumeNextObserver(IMaybeObserver<T> downstream, IMaybeSource<T> fallback)
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
                var inner = new MaybeInnerObserver<T>(downstream);
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

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }

    /// <summary>
    /// Switches to a fallback maybe source if
    /// the upstream fails.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeOnErrorResumeNextSelector<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly Func<Exception, IMaybeSource<T>> handler;

        public MaybeOnErrorResumeNextSelector(IMaybeSource<T> source, Func<Exception, IMaybeSource<T>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new OnErrorResumeNextObserver(observer, handler));
        }

        sealed class OnErrorResumeNextObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            readonly Func<Exception, IMaybeSource<T>> handler;

            IDisposable upstream;

            IDisposable fallbackObserver;

            public OnErrorResumeNextObserver(IMaybeObserver<T> downstream, Func<Exception, IMaybeSource<T>> handler)
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
                var fb = default(IMaybeSource<T>);

                try
                {
                    fb = RequireNonNullRef(handler(error), "The handler returned a null IMaybeSource");
                }
                catch (Exception ex)
                {
                    downstream.OnError(new AggregateException(error, ex));
                    return;
                }

                var inner = new MaybeInnerObserver<T>(downstream);
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

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }
}
