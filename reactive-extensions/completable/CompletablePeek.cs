using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Calls various delegates on the various lifecycle events of
    /// a completable source.
    /// </summary>
    /// <remarks>Since 0.0.7</remarks>
    internal sealed class CompletablePeek : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly Action onCompleted;

        readonly Action<Exception> onError;

        readonly Action<IDisposable> onSubscribe;

        readonly Action onTerminate;

        readonly Action onAfterTerminate;

        readonly Action onDispose;

        readonly Action doFinally;

        public CompletablePeek(
            ICompletableSource source,
            Action onCompleted, 
            Action<Exception> onError, 
            Action<IDisposable> onSubscribe, 
            Action onTerminate, 
            Action onAfterTerminate, 
            Action onDispose, 
            Action doFinally)
        {
            this.source = source;
            this.onCompleted = onCompleted;
            this.onError = onError;
            this.onSubscribe = onSubscribe;
            this.onTerminate = onTerminate;
            this.onAfterTerminate = onAfterTerminate;
            this.onDispose = onDispose;
            this.doFinally = doFinally;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new PeekObserver(observer, onCompleted, onError,
                onSubscribe, onTerminate, onAfterTerminate, onDispose, doFinally));
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

        static Action<T> Combine<T>(Action<T> a1, Action<T> a2)
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

        internal static ICompletableSource Create(ICompletableSource source,
            Action onCompleted = null,
            Action<Exception> onError = null,
            Action<IDisposable> onSubscribe = null,
            Action onTerminate = null,
            Action onAfterTerminate = null,
            Action onDispose = null,
            Action doFinally = null
            )
        {
            if (source is CompletablePeek p)
            {
                return new CompletablePeek(p.source,
                    Combine(p.onCompleted, onCompleted),
                    Combine(p.onError, onError),
                    Combine(p.onSubscribe, onSubscribe),
                    Combine(p.onTerminate, onTerminate),
                    Combine(p.onAfterTerminate, onAfterTerminate),
                    Combine(p.onDispose, onDispose),
                    Combine(p.doFinally, doFinally)
                );
            }
            return new CompletablePeek(source,
                onCompleted,
                onError,
                onSubscribe,
                onTerminate,
                onAfterTerminate,
                onDispose,
                doFinally
            );
        }

        internal sealed class PeekObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            readonly Action onCompleted;

            readonly Action<Exception> onError;

            readonly Action<IDisposable> onSubscribe;

            readonly Action onTerminate;

            readonly Action onAfterTerminate;

            readonly Action onDispose;

            Action doFinally;

            IDisposable upstream;

            public PeekObserver(
                ICompletableObserver downstream, 
                Action onCompleted, 
                Action<Exception> onError, 
                Action<IDisposable> onSubscribe, 
                Action onTerminate, 
                Action onAfterTerminate, 
                Action onDispose, 
                Action doFinally)
            {
                this.downstream = downstream;
                this.onCompleted = onCompleted;
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

            public void OnCompleted()
            {
                upstream = DisposableHelper.DISPOSED;

                try
                {
                    onCompleted?.Invoke();
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

                downstream.OnCompleted();

                try
                {
                    onAfterTerminate?.Invoke();
                } catch (Exception)
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
