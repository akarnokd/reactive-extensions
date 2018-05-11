using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Switches to the fallback observables if the main source
    /// or a previous fallback is empty.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.3</remarks>
    internal sealed class SwitchIfEmpty<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly IObservable<T>[] fallbacks;

        public SwitchIfEmpty(IObservable<T> source, IObservable<T>[] fallbacks)
        {
            this.source = source;
            this.fallbacks = fallbacks;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new SwitchIfEmptyObserver(observer, fallbacks);
            parent.Next(source);
            return parent;
        }

        sealed class SwitchIfEmptyObserver : BaseObserver<T, T>
        {

            readonly IObservable<T>[] fallbacks;

            bool nonEmpty;

            int wip;

            int index;

            internal SwitchIfEmptyObserver(IObserver<T> downstream, IObservable<T>[] fallbacks) : base(downstream)
            {
                this.fallbacks = fallbacks;
            }

            public override void OnCompleted()
            {
                if (nonEmpty)
                {
                    downstream.OnCompleted();
                    Dispose();
                }
                else
                {
                    var d = Volatile.Read(ref upstream);
                    if (d != DisposableHelper.DISPOSED)
                    {
                        if (Interlocked.CompareExchange(ref upstream, null, d) == d)
                        {
                            d?.Dispose();
                            Next(null);
                        }
                    }
                }
            }

            public override void OnError(Exception error)
            {
                downstream.OnError(error);
                Dispose();
            }

            public override void OnNext(T value)
            {
                nonEmpty = true;
                downstream.OnNext(value);
            }

            internal void Next(IObservable<T> first)
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    do
                    {
                        var sad = new SingleAssignmentDisposable();
                        if (Interlocked.CompareExchange(ref upstream, sad, null) == null)
                        {
                            if (first != null)
                            {
                                sad.Disposable = first.Subscribe(this);
                                first = null;
                            }
                            else
                            {
                                if (index == fallbacks.Length)
                                {
                                    downstream.OnCompleted();
                                } else
                                {
                                    var src = fallbacks[index];
                                    if (src != null)
                                    {
                                        fallbacks[index] = null;

                                        index++;
                                        sad.Disposable = src.Subscribe(this);
                                    }
                                    else
                                    {
                                        downstream.OnError(new NullReferenceException("The fallbacks[" + index + "] is null"));
                                    }
                                }
                            }
                        }
                    }
                    while (Interlocked.Decrement(ref wip) != 0);
                }
            }
        }
    }
}
