using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourcePeek<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Action<T> onNext;
        readonly Action<T> onAfterNext;
        readonly Action<Exception> onError;
        readonly Action onCompleted;
        readonly Action onTerminate;
        readonly Action onAfterTerminate;
        readonly Action doFinally;
        readonly Action<IDisposable> onSubscribe;
        readonly Action onDispose;

        private ObservableSourcePeek(IObservableSource<T> source, Action<T> onNext, Action<T> onAfterNext, Action<Exception> onError, Action onCompleted, Action onTerminate, Action onAfterTerminate, Action doFinally, Action<IDisposable> onSubscribe, Action onDispose)
        {
            this.source = source;
            this.onNext = onNext;
            this.onAfterNext = onAfterNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
            this.onTerminate = onTerminate;
            this.onAfterTerminate = onAfterTerminate;
            this.doFinally = doFinally;
            this.onSubscribe = onSubscribe;
            this.onDispose = onDispose;
        }

        internal static IObservableSource<T> Create(
            IObservableSource<T> source, 
            Action<T> onNext = null, 
            Action<T> onAfterNext = null, 
            Action<Exception> onError = null, 
            Action onCompleted = null, 
            Action onTerminate = null, 
            Action onAfterTerminate = null, 
            Action doFinally = null, 
            Action<IDisposable> onSubscribe = null, 
            Action onDispose = null
            )
        {

            if (source is ObservableSourcePeek<T> p)
            {
                return new ObservableSourcePeek<T>(p.source,
                    Combine(p.onNext, onNext),
                    Combine(p.onAfterNext, onAfterNext),
                    Combine(p.onError, onError),
                    Combine(p.onCompleted, onCompleted),
                    Combine(p.onTerminate, onTerminate),
                    Combine(p.onAfterTerminate, onAfterTerminate),
                    Combine(p.doFinally, doFinally),
                    Combine(p.onSubscribe, onSubscribe),
                    Combine(p.onDispose, onDispose)
                );
            }
            return new ObservableSourcePeek<T>(source,
                onNext, onAfterNext, onError, onCompleted,
                onTerminate, onAfterTerminate, doFinally,
                onSubscribe, onDispose
            );
        }

        static Action<U> Combine<U>(Action<U> first, Action<U> second)
        {
            if (first == null)
            {
                return second;
            }
            if (second == null)
            {
                return first;
            }
            return v => { first(v); second(v); };
        }

        static Action Combine(Action first, Action second)
        {
            if (first == null)
            {
                return second;
            }
            if (second == null)
            {
                return first;
            }
            return () => { first(); second(); };
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new PeekObserver(
                observer,
                onNext, onAfterNext, onError, onCompleted,
                onTerminate, onAfterTerminate, doFinally,
                onSubscribe, onDispose
            ));
        }

        sealed class PeekObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly Action<T> onNext;
            readonly Action<T> onAfterNext;
            readonly Action<Exception> onError;
            readonly Action onCompleted;
            readonly Action onTerminate;
            readonly Action onAfterTerminate;

            Action doFinally;

            readonly Action<IDisposable> onSubscribe;
            readonly Action onDispose;

            IDisposable upstream;

            bool done;

            public PeekObserver(ISignalObserver<T> downstream, Action<T> onNext, Action<T> onAfterNext, Action<Exception> onError, Action onCompleted, Action onTerminate, Action onAfterTerminate, Action doFinally, Action<IDisposable> onSubscribe, Action onDispose)
            {
                this.downstream = downstream;
                this.onNext = onNext;
                this.onAfterNext = onAfterNext;
                this.onError = onError;
                this.onCompleted = onCompleted;
                this.onTerminate = onTerminate;
                this.onAfterTerminate = onAfterTerminate;
                this.doFinally = doFinally;
                this.onSubscribe = onSubscribe;
                this.onDispose = onDispose;
            }

            public void Dispose()
            {
                try
                {
                    onDispose?.Invoke();
                }
                catch (Exception)
                {
                    // TODO where to put these?
                }

                upstream.Dispose();

                DoFinally();
            }

            void DoFinally()
            {
                try
                {
                    Interlocked.Exchange(ref doFinally, null)?.Invoke();
                }
                catch (Exception)
                {
                    // TODO where to put these?
                }
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                try
                {
                    onCompleted?.Invoke();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return;
                }

                try
                {
                    onTerminate?.Invoke();
                }
                catch (Exception ex)
                {
                    OnErrorCore(ex, false);
                    return;
                }

                downstream.OnCompleted();

                try
                {
                    onAfterTerminate?.Invoke();
                }
                catch (Exception)
                {
                    // TODO where to put these?
                }

                DoFinally();
            }

            public void OnError(Exception ex)
            {
                OnErrorCore(ex, true);
            }

            void OnErrorCore(Exception ex, bool callOnTerminate)
            {
                if (done)
                {
                    return;
                }
                done = true;

                try
                {
                    onError?.Invoke(ex);
                }
                catch (Exception exc)
                {
                    ex = new AggregateException(ex, exc);
                }

                if (callOnTerminate)
                {
                    try
                    {
                        onTerminate?.Invoke();
                    }
                    catch (Exception exc)
                    {
                        ex = new AggregateException(ex, exc);
                    }
                }

                downstream.OnError(ex);

                try
                {
                    onAfterTerminate?.Invoke();
                }
                catch (Exception)
                {
                    // TODO where to put these?
                }

                DoFinally();
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }

                try
                {
                    onNext?.Invoke(item);
                }
                catch (Exception ex)
                {
                    Dispose();
                    OnError(ex);
                    return;
                }

                downstream.OnNext(item);

                try
                {
                    onAfterNext?.Invoke(item);
                }
                catch (Exception ex)
                {
                    Dispose();
                    OnError(ex);
                    return;
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                try
                {
                    onSubscribe?.Invoke(d);
                }
                catch (Exception ex)
                {
                    downstream.OnSubscribe(this);
                    OnError(ex);
                    return;
                }

                downstream.OnSubscribe(this);
            }
        }
    }
}
