using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceConcatMap<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, IObservableSource<R>> mapper;

        readonly bool delayErrors;

        readonly int capacityHint;

        public ObservableSourceConcatMap(IObservableSource<T> source, Func<T, IObservableSource<R>> mapper, bool delayErrors, int capacityHint)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.capacityHint = capacityHint;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            source.Subscribe(new ConcatMapMainObserver(observer, mapper, delayErrors, capacityHint));
        }

        sealed class ConcatMapMainObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<R> downstream;

            readonly Func<T, IObservableSource<R>> mapper;

            readonly bool delayErrors;

            readonly int capacityHint;

            readonly InnerObserver inner;

            IDisposable upstream;

            ISimpleQueue<T> queue;

            int wip;

            Exception errors;
            bool done;

            bool disposed;

            bool sourceFused;

            int active;

            public ConcatMapMainObserver(ISignalObserver<R> downstream, Func<T, IObservableSource<R>> mapper, bool delayErrors, int capacityHint)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.capacityHint = capacityHint;
                this.inner = new InnerObserver(downstream, this);
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                DisposableHelper.Dispose(ref upstream);
                inner.Dispose();
                Drain();
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T item)
            {
                if (!sourceFused)
                {
                    queue.TryOffer(item);
                }
                Drain();
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                if (d is IFuseableDisposable<T> f)
                {
                    var m = f.RequestFusion(FusionSupport.Any);

                    if (m == FusionSupport.Sync)
                    {
                        sourceFused = true;
                        this.queue = f;
                        Volatile.Write(ref done, true);
                        downstream.OnSubscribe(this);
                        Drain();
                        return;
                    }
                    if (m == FusionSupport.Async)
                    {
                        sourceFused = true;
                        this.queue = f;
                        downstream.OnSubscribe(this);
                        return;
                    }
                }

                queue = new SpscLinkedArrayQueue<T>(capacityHint);
                downstream.OnSubscribe(this);
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var delayErrors = this.delayErrors;
                var queue = this.queue;

                for (; ;)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        queue.Clear();
                    }
                    else
                    {
                        if (Volatile.Read(ref active) == 0)
                        {
                            if (!delayErrors)
                            {
                                var ex = Volatile.Read(ref errors);
                                if (ex != null)
                                {
                                    Volatile.Write(ref disposed, true);
                                    DisposableHelper.Dispose(ref upstream);
                                    downstream.OnError(ex);
                                    continue;
                                }
                            }

                            var d = Volatile.Read(ref done);

                            var v = default(T);

                            var success = false;
                            try
                            {
                                v = queue.TryPoll(out success);
                            }
                            catch (Exception ex)
                            {
                                DisposableHelper.Dispose(ref upstream);
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref errors, ex);
                                }
                                else
                                {
                                    Interlocked.CompareExchange(ref errors, ex, null);
                                }

                                ex = ExceptionHelper.Terminate(ref errors);
                                Volatile.Write(ref disposed, true);
                                Volatile.Write(ref done, true);

                                downstream.OnError(ex);
                                continue;
                            }

                            if (d && !success)
                            {
                                Volatile.Write(ref disposed, true);
                                var ex = Volatile.Read(ref errors);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                }
                                else
                                {
                                    downstream.OnCompleted();
                                }
                            } else
                            if (success)
                            {
                                var src = default(IObservableSource<R>);

                                try
                                {
                                    src = RequireNonNullRef(mapper(v), "The mapper returned a null IObservableSource");
                                }
                                catch (Exception ex)
                                {
                                    DisposableHelper.Dispose(ref upstream);
                                    if (delayErrors)
                                    {
                                        ExceptionHelper.AddException(ref errors, ex);
                                    }
                                    else
                                    {
                                        Interlocked.CompareExchange(ref errors, ex, null);
                                    }

                                    ex = ExceptionHelper.Terminate(ref errors);
                                    Volatile.Write(ref disposed, true);
                                    Volatile.Write(ref done, true);

                                    downstream.OnError(ex);
                                    continue;
                                }

                                Interlocked.Exchange(ref active, 1);
                                src.Subscribe(inner);
                            }
                        }
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        break;
                    }
                }
            }

            void InnerCompleted()
            {
                Interlocked.Exchange(ref active, 0);
                Drain();
            }

            void InnerError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }

                Interlocked.Exchange(ref active, 0);
                Drain();
            }

            sealed class InnerObserver : ISignalObserver<R>, IDisposable
            {
                readonly ISignalObserver<R> downstream;

                readonly ConcatMapMainObserver parent;

                IDisposable upstream;

                public InnerObserver(ISignalObserver<R> downstream, ConcatMapMainObserver parent)
                {
                    this.downstream = downstream;
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.InnerCompleted();
                }

                public void OnError(Exception ex)
                {
                    parent.InnerError(ex);
                }

                public void OnNext(R item)
                {
                    downstream.OnNext(item);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.Replace(ref upstream, d);
                }
            }
        }
    }
}
