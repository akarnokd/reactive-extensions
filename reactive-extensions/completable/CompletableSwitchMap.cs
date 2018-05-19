using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the upstream observable sequence into a
    /// completable source and switches to it, disposing the
    /// previous completable source, optionally delaying
    /// errors until all sources terminated.
    /// </summary>
    /// <typeparam name="T">The element type of the observable sequence.</typeparam>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableSwitchMap<T> : ICompletableSource
    {
        readonly IObservable<T> source;

        readonly Func<T, ICompletableSource> mapper;

        readonly bool delayErrors;

        public CompletableSwitchMap(IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new SwitchMapObserver(observer, mapper, delayErrors);
            observer.OnSubscribe(parent);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class SwitchMapObserver : IObserver<T>, IDisposable
        {
            readonly ICompletableObserver downstream;

            readonly Func<T, ICompletableSource> mapper;

            readonly bool delayErrors;

            IDisposable upstream;

            InnerObserver current;

            static readonly InnerObserver DISPOSED = new InnerObserver(null);

            volatile bool done;

            Exception errors;

            public SwitchMapObserver(ICompletableObserver downstream, Func<T, ICompletableSource> mapper, bool delayErrors)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                Interlocked.Exchange(ref current, DISPOSED)?.Dispose();
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnCompleted()
            {
                done = true;
                if (Volatile.Read(ref current) == null)
                {
                    Terminate();
                }
            }

            void Terminate()
            {
                var ex = ExceptionHelper.Terminate(ref errors);
                if (ex != ExceptionHelper.TERMINATED)
                {
                    if (ex != null)
                    {
                        downstream.OnError(ex);
                    }
                    else
                    {
                        downstream.OnCompleted();
                    }

                    DisposableHelper.Dispose(ref upstream);
                }
            }

            public void OnError(Exception error)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, error);
                }
                else
                {
                    if (Interlocked.CompareExchange(ref errors, error, null) == null)
                    {
                        Interlocked.Exchange(ref current, null)?.Dispose();
                    }
                }
                done = true;
                if (Volatile.Read(ref current) == null)
                {
                    Terminate();
                }
            }

            public void OnNext(T value)
            {
                if (done)
                {
                    return;
                }
                for (; ; )
                {
                    var c = Volatile.Read(ref current);
                    if (c == DISPOSED)
                    {
                        return;
                    }
                    if (c == null)
                    {
                        break;
                    }
                    if (Interlocked.CompareExchange(ref current, null, c) == c)
                    {
                        c.Dispose();
                        break;
                    }
                }

                var source = default(ICompletableSource);

                try
                {
                    source = RequireNonNullRef(mapper(value), "The mapper returned a null ICompletableSource");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    DisposableHelper.Dispose(ref upstream);
                    return;
                }

                var inner = new InnerObserver(this);
                if (Interlocked.CompareExchange(ref current, inner, null) == null)
                {
                    source.Subscribe(inner);
                }
            }

            void InnerCompleted(InnerObserver sender)
            {
                var c = Volatile.Read(ref current);
                if (c == sender)
                {
                    if (Interlocked.CompareExchange(ref current, null, c) == c)
                    {
                        if (done && Volatile.Read(ref current) == null)
                        {
                            Terminate();
                        }
                    }
                }
            }

            void InnerError(InnerObserver sender, Exception ex)
            {
                var c = Volatile.Read(ref current);
                if (c == sender)
                {
                    if (Interlocked.CompareExchange(ref current, null, c) == c)
                    {
                        if (delayErrors)
                        {
                            ExceptionHelper.AddException(ref errors, ex);
                            if (done && Volatile.Read(ref current) == null)
                            {
                                Terminate();
                            }
                        }
                        else
                        {
                            if (Interlocked.CompareExchange(ref errors, ex, null) == null)
                            {
                                Interlocked.Exchange(ref current, DISPOSED)?.Dispose();
                                Terminate();
                            }
                        }

                    }
                }
            }

            internal sealed class InnerObserver : ICompletableObserver, IDisposable
            {
                readonly SwitchMapObserver parent;

                IDisposable upstream;

                public InnerObserver(SwitchMapObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.InnerCompleted(this);
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(this, error);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }
}
