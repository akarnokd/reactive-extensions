using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A subject variant that allows at most one IObserver during its lifetime
    /// and buffers events until one subscribes.
    /// </summary>
    /// <typeparam name="T">The input and output type.</typeparam>
    public sealed class UnicastSubject<T> : ISubject<T>
    {
        readonly SpscLinkedArrayQueue<T> queue;

        Action onTerminate;

        IObserver<T> observer;

        int once;

        int wip;

        bool done;
        Exception error;

        /// <summary>
        /// Constructs a new UnicastSubject with the given capacity hint (expected
        /// number of items to be buffered until consumed) and an action to
        /// call when the UnicastSubject terminates or the observer disposes.
        /// </summary>
        /// <param name="capacityHint">The expected number of items to be buffered until consumed.</param>
        /// <param name="onTerminate">The action to call when the UnicastSubject terminates or the observer disposes.</param>
        public UnicastSubject(int capacityHint = 128, Action onTerminate = null)
        {
            this.queue = new SpscLinkedArrayQueue<T>(capacityHint);
            Volatile.Write(ref this.onTerminate, onTerminate);
        }

        /// <summary>
        /// Called when the upstream completes normally.
        /// Calling this method multiple times has no effect.
        /// </summary>
        public void OnCompleted()
        {
            if (Volatile.Read(ref done) || IsDisposed())
            {
                return;
            }
            Volatile.Write(ref done, true);
            Terminate();
            Drain();
        }

        /// <summary>
        /// Called when the upstream completes with an exception.
        /// Calling this method multiple times has no effect and
        /// subsequent calls will drop the Exception provided.
        /// </summary>
        /// <param name="error">The terminal exception.</param>
        public void OnError(Exception error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }
            if (Volatile.Read(ref done) || IsDisposed())
            {
                return;
            }
            if (Interlocked.CompareExchange(ref this.error, error, null) == null)
            {
                Volatile.Write(ref done, true);
                Terminate();
                Drain();
            }
        }

        /// <summary>
        /// Called when a new item is available for consumption.
        /// Calling this method after the subject has been terminated
        /// or the observer disposed has no effect and the item is dropped.
        /// </summary>
        /// <param name="value">The new item available.</param>
        public void OnNext(T value)
        {
            if (Volatile.Read(ref done) || IsDisposed())
            {
                return;
            }

            queue.Offer(value);
            Drain();
        }

        /// <summary>
        /// Subscribes an observer to this UnicastSubject when there
        /// were no observers before and starts emitting buffered events
        /// to it.
        /// If multiple observers try to subscribe, the other observers
        /// will receive an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="observer">The incoming observer.</param>
        /// <returns>The IDisposable that can be called to stop the event delivery.</returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                Volatile.Write(ref this.observer, observer);
                Drain();
                return new DisposeObserver(this);
            }
            observer.OnError(new InvalidOperationException("The UnicastSubject allows at most one IObserver to subscribe during its existence"));
            return DisposableHelper.EMPTY;
        }

        /// <summary>
        /// Returns true if there is an IObserver currently subscribed to
        /// this UnicastSubject.
        /// </summary>
        /// <returns>True if there is an observer currently subscribed.</returns>
        public bool HasObserver()
        {
            return Volatile.Read(ref observer) != null;
        }

        /// <summary>
        /// Returns true if this UnicastSubject terminated with an error.
        /// </summary>
        /// <returns>True if this UnicastSubject terminated with an error.</returns>
        public bool HasException()
        {
            return Volatile.Read(ref error) != null;
        }

        /// <summary>
        /// Returns true if this UnicastSubject terminated normally.
        /// </summary>
        /// <returns>True if this UnicastSubject terminated normally.</returns>
        public bool HasCompleted()
        {
            return Volatile.Read(ref done);
        }

        /// <summary>
        /// Returns the terminal exception, if any.
        /// </summary>
        /// <returns>The terminal exception or null if the UnicastSubject has not yet terminated or not with an error.</returns>
        public Exception GetException()
        {
            return Volatile.Read(ref error);
        }

        void Dispose()
        {
            Volatile.Write(ref observer, null);
            Volatile.Write(ref once, 2);
            Terminate();
            Drain();
        }

        void Terminate()
        {
            Interlocked.Exchange(ref onTerminate, null)?.Invoke();
        }

        bool IsDisposed()
        {
            return Volatile.Read(ref once) == 2;
        }

        void Drain()
        {
            if (Interlocked.Increment(ref wip) != 1)
            {
                return;
            }

            int missed = 1;

            for (; ; )
            {
                var observer = Volatile.Read(ref this.observer);

                if (observer != null)
                {
                    for (; ; )
                    {
                        if (IsDisposed())
                        {
                            queue.Clear();
                            break;
                        }

                        var d = Volatile.Read(ref done);
                        var empty = !queue.TryPoll(out var v);

                        if (d && empty)
                        {
                            var ex = Volatile.Read(ref error);
                            if (ex != null)
                            {
                                observer.OnError(ex);
                            }
                            else
                            {
                                observer.OnCompleted();
                            }

                            Volatile.Write(ref this.observer, null);
                            Volatile.Write(ref once, 2);
                            break;
                        }

                        if (empty)
                        {
                            break;
                        }

                        observer.OnNext(v);
                    }
                }
                else
                if (IsDisposed())
                {
                    queue.Clear();
                }

                missed = Interlocked.Add(ref wip, -missed);
                if (missed == 0)
                {
                    break;
                }
            }
        }

        sealed class DisposeObserver : IDisposable
        {
            UnicastSubject<T> parent;

            public DisposeObserver(UnicastSubject<T> parent)
            {
                Volatile.Write(ref this.parent, parent);
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref parent, null)?.Dispose();
            }
        }
    }
}
