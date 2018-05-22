using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Calls various delegates on the various lifecycle events of
    /// a single source.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SinglePeek<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly Action<T> onSuccess;

        readonly Action<T> onAfterSuccess;

        readonly Action<Exception> onError;

        readonly Action<IDisposable> onSubscribe;

        readonly Action onTerminate;

        readonly Action onAfterTerminate;

        readonly Action onDispose;

        readonly Action doFinally;

        public SinglePeek(
            ISingleSource<T> source,
            Action<T> onSuccess,
            Action<T> onAfterSuccess,
            Action<Exception> onError,
            Action<IDisposable> onSubscribe, 
            Action onTerminate, 
            Action onAfterTerminate, 
            Action onDispose, 
            Action doFinally)
        {
            this.source = source;
            this.onSuccess = onSuccess;
            this.onAfterSuccess = onAfterSuccess;
            this.onError = onError;
            this.onSubscribe = onSubscribe;
            this.onTerminate = onTerminate;
            this.onAfterTerminate = onAfterTerminate;
            this.onDispose = onDispose;
            this.doFinally = doFinally;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new PeekObserver(observer, 
                onSuccess,
                onAfterSuccess,
                onError, 
                onSubscribe, 
                onTerminate, 
                onAfterTerminate, 
                onDispose, 
                doFinally));
        }

        static Action Combine(Action a1, Action a2)
        {
            if (a1 == null && a2 != null)
            {
                return a2;
            }
            if (a1 != null && a2 == null)
            {
                return a1;
            }
            if (a1 != null && a2 != null)
            {
                return () => { a1(); a2(); };
            }
            return null;
        }

        static Action<U> Combine<U>(Action<U> a1, Action<U> a2)
        {
            if (a1 == null && a2 != null)
            {
                return a2;
            }
            if (a1 != null && a2 == null)
            {
                return a1;
            }
            if (a1 != null && a2 != null)
            {
                return v => { a1(v); a2(v); };
            }
            return null;
        }

        internal static ISingleSource<T> Create(ISingleSource<T> source,
            Action<T> onSuccess = null,
            Action<T> onAfterSuccess = null,
            Action<Exception> onError = null,
            Action<IDisposable> onSubscribe = null,
            Action onTerminate = null,
            Action onAfterTerminate = null,
            Action onDispose = null,
            Action doFinally = null
            )
        {
            if (source is SinglePeek<T> p)
            {
                return new SinglePeek<T>(p.source,
                    Combine(p.onSuccess, onSuccess),
                    Combine(p.onAfterSuccess, onAfterSuccess),
                    Combine(p.onError, onError),
                    Combine(p.onSubscribe, onSubscribe),
                    Combine(p.onTerminate, onTerminate),
                    Combine(p.onAfterTerminate, onAfterTerminate),
                    Combine(p.onDispose, onDispose),
                    Combine(p.doFinally, doFinally)
                );
            }
            return new SinglePeek<T>(source,
                onSuccess,
                onAfterSuccess,
                onError,
                onSubscribe,
                onTerminate,
                onAfterTerminate,
                onDispose,
                doFinally
            );
        }

        internal sealed class PeekObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly Action<T> onSuccess;

            readonly Action<T> onAfterSuccess;

            readonly Action<Exception> onError;

            readonly Action<IDisposable> onSubscribe;

            readonly Action onTerminate;

            readonly Action onAfterTerminate;

            readonly Action onDispose;

            Action doFinally;

            IDisposable upstream;

            bool done;

            public PeekObserver(
                ISingleObserver<T> downstream, 
                Action<T> onSuccess,
                Action<T> onAfterSuccess,
                Action<Exception> onError,
                Action<IDisposable> onSubscribe, 
                Action onTerminate, 
                Action onAfterTerminate, 
                Action onDispose, 
                Action doFinally)
            {
                this.downstream = downstream;
                this.onSuccess = onSuccess;
                this.onAfterSuccess = onAfterSuccess;
                this.onError = onError;
                this.onSubscribe = onSubscribe;
                this.onTerminate = onTerminate;
                this.onAfterTerminate = onAfterTerminate;
                this.onDispose = onDispose;
                this.doFinally = doFinally;
            }

            void Finally()
            {
                try
                {
                    Interlocked.Exchange(ref doFinally, null)?.Invoke();
                } catch (Exception)
                {
                    // FIXME what should happen with the exception
                }
            }

            public void Dispose()
            {
                try
                {
                    onDispose?.Invoke();
                } catch (Exception)
                {
                    // FIXME what should happen with the exception?
                } 
                upstream.Dispose();
                upstream = DisposableHelper.DISPOSED;

                Finally();
            }

            public void OnSuccess(T item)
            {
                if (done)
                {
                    return;
                }

                upstream = DisposableHelper.DISPOSED;

                try
                {
                    onSuccess?.Invoke(item);
                }
                catch (Exception ex)
                {
                    Error(ex, true);
                    return;
                }

                try
                {
                    onTerminate?.Invoke();
                }
                catch (Exception ex)
                {
                    Error(ex, false);
                    return;
                }

                downstream.OnSuccess(item);

                try
                {
                    onAfterSuccess?.Invoke(item);
                }
                catch (Exception)
                {
                    // FIXME what should happen with the exception
                }

                try
                {
                    onAfterTerminate?.Invoke();
                }
                catch (Exception)
                {
                    // FIXME what should happen with the exception
                }

                Finally();
            }

            void Error(Exception error, bool callTerminate)
            {
                upstream = DisposableHelper.DISPOSED;

                try
                {
                    onError?.Invoke(error);
                }
                catch (Exception ex)
                {
                    error = new AggregateException(error, ex);
                }

                if (callTerminate)
                {
                    try
                    {
                        onTerminate?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        error = new AggregateException(error, ex);
                    }
                }

                downstream.OnError(error);

                try
                {
                    onAfterTerminate?.Invoke();
                }
                catch (Exception)
                {
                    // FIXME what should happen with the exception
                }

                Finally();
            }

            public void OnError(Exception error)
            {
                if (done)
                {
                    return;
                }
                Error(error, true);
            }

            public void OnSubscribe(IDisposable d)
            {
                this.upstream = d;
                try
                {
                    onSubscribe?.Invoke(d);
                }
                catch (Exception ex)
                {
                    done = true;
                    try
                    {
                        onDispose?.Invoke();
                    }
                    catch (Exception exc)
                    {
                        ex = new AggregateException(ex, exc);
                    }

                    upstream.Dispose();
                    upstream = DisposableHelper.DISPOSED;

                    DisposableHelper.Error(downstream, ex);

                    Finally();
                    return;
                }
                downstream.OnSubscribe(this);
            }
        }
    }
}
