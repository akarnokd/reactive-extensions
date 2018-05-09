using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension methods for IObservables.
    /// </summary>
    public static class ReactiveExtensions
    {

        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> observable.
        /// </summary>
        /// <typeparam name="T">The value type of the source observable.</typeparam>
        /// <param name="source">The source observable to test.</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this IObservable<T> source)
        {
            var to = new TestObserver<T>();
            to.OnSubscribe(source.Subscribe(to));
            return to;
        }

        /// <summary>
        /// Emits the upstream item on the specified <paramref name="scheduler"/>
        /// and optionally delays an upstream error.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream source observable.</param>
        /// <param name="scheduler">The IScheduler to use.</param>
        /// <param name="delayError">If true, an upstream error is emitted last. If false, an error may cut ahead of other values.</param>
        /// <returns>The new IObservable instance.</returns>
        public static IObservable<T> ObserveOn<T>(
            this IObservable<T> source, 
            IScheduler scheduler, 
            bool delayError)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }

            return new ObserveOn<T>(source, scheduler, delayError);
        }

        /// <summary>
        /// Calls the specified <paramref name="handler"/> when an observer
        /// subscribes to the source observable.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect on various terminal cases.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoOnSubscribe<T>(source, handler);
        }

        /// <summary>
        /// Calls the specified <paramref name="handler"/> when the downstream
        /// disposes the sequence.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect when the downstream disposes.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoOnDispose<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoOnDispose<T>(source, handler);
        }

        /// <summary>
        /// Calls the specified <paramref name="handler"/> after the current
        /// OnNext item has been emitted to the downstream but before the
        /// next item or terminal signal.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect after each upstream item.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoAfterNext<T>(this IObservable<T> source, Action<T> handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoAfterNext<T>(source, handler);
        }

        /// <summary>
        /// Call the specified <paramref name="handler"/> after the upstream completed
        /// normally or with an error.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect the termination of.</param>
        /// <param name="handler">The action to call.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoAfterTerminate<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoAfterTerminate<T>(source, handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> exactly once when the source
        /// completes normally, with an error or the downstream disposes the stream.
        /// </summary>
        /// <typeparam name="T">The value type of the sequence.</typeparam>
        /// <param name="source">The upstream observable to side-effect on various terminal cases.</param>
        /// <param name="handler">The action to call when the upstream terminates or the downstream disposes.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<T> DoFinally<T>(this IObservable<T> source, Action handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new DoFinally<T>(source, handler);
        }

        /// <summary>
        /// Wraps the given <paramref name="observer"/> so that concurrent
        /// calls to the returned observer's OnXXX methods are serialized.
        /// </summary>
        /// <typeparam name="T">The element type of the flow</typeparam>
        /// <param name="observer">The observer to wrap and serialize signals for.</param>
        /// <returns>The serialized observer instance.</returns>
        public static IObserver<T> ToSerialized<T>(this IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            if (observer is SerializedObserver<T> o)
            {
                return o;
            }

            return new SerializedObserver<T>(observer);
        }

        /// <summary>
        /// Wraps the given <paramref name="subject"/> so that concurrent
        /// calls to the returned subject's OnXXX methods are serialized.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The subject's output value type.</typeparam>
        /// <param name="subject">The subject to wrap and serialize signals for.</param>
        /// <returns>The serialized observer instance.</returns>
        public static ISubject<T, R> ToSerialized<T, R>(this ISubject<T, R> subject)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }
            if (subject is SerializedSubject<T, R> o)
            {
                return o;
            }

            return new SerializedSubject<T, R>(subject);
        }

        /// <summary>
        /// Wraps the given <paramref name="subject"/> so that concurrent
        /// calls to the returned subject's OnXXX methods are serialized.
        /// </summary>
        /// <typeparam name="T">The value type of the flow.</typeparam>
        /// <param name="subject">The subject to wrap and serialize signals for.</param>
        /// <returns>The serialized observer instance.</returns>
        public static ISubject<T> ToSerialized<T>(this ISubject<T> subject)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }
            if (subject is SerializedSubject<T> || subject is SerializedSubject<T, T>)
            {
                return subject;
            }

            return new SerializedSubject<T>(subject);
        }

        /// <summary>
        /// Maps the upstream items into observables, runs some or all of them at once, emits items from one
        /// of the observables until it completes, then switches to the next observable.
        /// </summary>
        /// <typeparam name="T">The value type of the upstream.</typeparam>
        /// <typeparam name="R">The output value type.</typeparam>
        /// <param name="source">The source observable to be mapper and concatenated eagerly.</param>
        /// <param name="mapper">The function that returns an observable for an upstream item.</param>
        /// <param name="maxConcurrency">The maximum number of observables to run at a time.</param>
        /// <param name="capacityHint">The number of items expected from each observable.</param>
        /// <returns>The new observable instance.</returns>
        public static IObservable<R> ConcatMapEager<T, R>(this IObservable<T> source, Func<T, IObservable<R>> mapper, int maxConcurrency = int.MaxValue, int capacityHint = 128)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }
            if (maxConcurrency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), maxConcurrency, nameof(maxConcurrency) + " must be positive");
            }
            if (capacityHint <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacityHint), capacityHint, nameof(capacityHint) + " must be positive");
            }

            return new ConcatMapEager<T, R>(source, mapper, maxConcurrency, capacityHint);
        }
    }
}
