using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension and factory methods for dealing with
    /// <see cref="IMaybeSource{T}"/>s.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public static class MaybeSource
    {
        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> maybe.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The source maybe to test.</param>
        /// <param name="dispose">Dispose the TestObserver before the subscription happens</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this IMaybeSource<T> source, bool dispose = false)
        {
            RequireNonNull(source, nameof(source));
            var to = new TestObserver<T>();
            if (dispose)
            {
                to.Dispose();
            }
            source.Subscribe(to);
            return to;
        }

        //-------------------------------------------------
        // Factory methods
        //-------------------------------------------------

        /// <summary>
        /// Creates a maybe that calls the specified <paramref name="onSubscribe"/>
        /// action with a <see cref="IMaybeEmitter{T}"/> to allow
        /// bridging the callback world with the reactive world.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="onSubscribe">The action that is called with an emitter
        /// that can be used for signaling an item, completion or error event.</param>
        /// <returns>The new maybe instance</returns>
        public static IMaybeSource<T> Create<T>(Action<IMaybeEmitter<T>> onSubscribe)
        {
            RequireNonNull(onSubscribe, nameof(onSubscribe));

            return new MaybeCreate<T>(onSubscribe);
        }

        /// <summary>
        /// Creates an empty maybe that completes immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <returns>The shared empty maybe source instance.</returns>
        public static IMaybeSource<T> Empty<T>()
        {
            return MaybeEmpty<T>.INSTANCE;
        }

        /// <summary>
        /// Creates a failing maybe that signals the specified error
        /// immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <param name="error">The error to signal.</param>
        /// <returns>The new maybe source instance.</returns>
        public static IMaybeSource<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new MaybeError<T>(error);
        }

        /// <summary>
        /// Creates a maybe that never terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <returns>The shared never-terminating maybe instance.</returns>
        public static IMaybeSource<T> Never<T>()
        {
            return MaybeNever<T>.INSTANCE;
        }

        /// <summary>
        /// Creates a maybe that succeeds with the given <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of the single item.</typeparam>
        /// <param name="item">The item to succeed with.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IMaybeSource<T> Just<T>(T item)
        {
            return new MaybeJust<T>(item);
        }

        /// <summary>
        /// Wraps and calls the given action for each individual
        /// maybe observer then completes or fails the observer
        /// depending on the action completes normally or threw an exception.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="action">The action to invoke for each individual maybe observer.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromAction<T>(Action action)
        {
            RequireNonNull(action, nameof(action));

            return new MaybeFromAction<T>(action);
        }

        /// <summary>
        /// Wraps and runs a function for each incoming
        /// maybe observer and signals the value returned
        /// by the function as the success event.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="func">The function to call for each observer.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromFunc<T>(Func<T> func)
        {
            RequireNonNull(func, nameof(func));

            return new MaybeFromFunc<T>(func);
        }

        /// <summary>
        /// Creates a maybe source that completes or fails
        /// its observers when the given (possibly still ongoing)
        /// task terminates.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="task">The task to wrap.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromTask<T>(Task task)
        {
            return task.ToMaybe<T>();
        }

        /// <summary>
        /// Creates a maybe source that succeeds or fails
        /// its observers when the given (possibly still ongoing)
        /// task terminates.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="task">The task to wrap.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromTask<T>(Task<T> task)
        {
            return task.ToMaybe();
        }

        public static IMaybeSource<T> AmbAll<T>(this IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Amb<T>(params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs each source maybe and emits their success items in order,
        /// optionally delaying errors from all of them until
        /// all terminate.
        /// </summary>
        /// <typeparam name="T">The success and result value type.</typeparam>
        /// <param name="sources">The array of maybe sources.</param>
        /// <param name="delayErrors">If true, errors are delayed until all sources terminate.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IObservable<T> ConcatAll<T>(this IMaybeSource<T>[] sources, bool delayErrors = false)
        {
            RequireNonNull(sources, nameof(sources));

            return new MaybeConcat<T>(sources, delayErrors);
        }

        /// <summary>
        /// Runs each source maybe and emits their success items in order.
        /// </summary>
        /// <typeparam name="T">The success and result value type.</typeparam>
        /// <param name="sources">The array of maybe sources.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IObservable<T> Concat<T>(params IMaybeSource<T>[] sources)
        {
            return ConcatAll(sources, false);
        }

        /// <summary>
        /// Runs each source maybe and emits their success items in order,
        /// optionally delaying errors from all of them until
        /// all terminate.
        /// </summary>
        /// <typeparam name="T">The success and result value type.</typeparam>
        /// <param name="sources">The array of maybe sources.</param>
        /// <param name="delayErrors">If true, errors are delayed until all sources terminate.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IObservable<T> Concat<T>(bool delayErrors, params IMaybeSource<T>[] sources)
        {
            return ConcatAll(sources, delayErrors);
        }

        /// <summary>
        /// Runs each source maybe returned by the enumerable sequence
        /// and emits their success items in order,
        /// optionally delaying errors from all of them until
        /// all terminate.
        /// </summary>
        /// <typeparam name="T">The success and result value type.</typeparam>
        /// <param name="sources">The enumerable sequence of maybe sources.</param>
        /// <param name="delayErrors">If true, errors are delayed until all sources terminate.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IObservable<T> Concat<T>(this IEnumerable<IMaybeSource<T>> sources, bool delayErrors = false)
        {
            RequireNonNull(sources, nameof(sources));

            return new MaybeConcatEnumerable<T>(sources, delayErrors);
        }

        /// <summary>
        /// Runs each inner maybe source produced by the observable sequence
        /// and emits their success items in order,
        /// optionally delaying errors from all of them until
        /// all terminate.
        /// </summary>
        /// <typeparam name="T">The success and result value type.</typeparam>
        /// <param name="sources">The array of maybe sources.</param>
        /// <param name="delayErrors">If true, errors are delayed until all sources terminate.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IObservable<T> Concat<T>(this IObservable<IMaybeSource<T>> sources, bool delayErrors = false)
        {
            return ConcatMap(sources, v => v, delayErrors);
        }

        public static IObservable<T> ConcatEagerAll<T>(this IMaybeSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(IEnumerable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(int maxConcurrency, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(int maxConcurrency, bool delayErrors, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(this IObservable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Defers the creation of the actual maybe source
        /// provided by a supplier function until a maybe observer completes.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="supplier">The function called for each individual maybe
        /// observer and should return a maybe source to subscribe to.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> Defer<T>(Func<IMaybeSource<T>> supplier)
        {
            RequireNonNull(supplier, nameof(supplier));

            return new MaybeDefer<T>(supplier);
        }

        public static IObservable<T> MergeAll<T>(this IMaybeSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(IEnumerable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(int maxConcurrency, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(int maxConcurrency, bool delayErrors, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(this IObservable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Signals 0L after a specified time elapsed on the given scheduler.
        /// </summary>
        /// <param name="time">The time to wait before signaling a success value of 0L.</param>
        /// <param name="scheduler">The scheduler to use for emitting the success value.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<long> Timer(TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(scheduler, nameof(scheduler));

            return new MaybeTimer(time, scheduler);
        }

        /// <summary>
        /// Generates a resource and a dependent maybe source
        /// for each maybe observer and cleans up the resource
        /// just before or just after the maybe source terminated
        /// or the observer has disposed the setup.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <typeparam name="S">The resource type.</typeparam>
        /// <param name="resourceSupplier">The supplier for a per-observer resource.</param>
        /// <param name="sourceSelector">Function that receives the per-observer resource returned
        /// by <paramref name="resourceSupplier"/> and returns a maybe source.</param>
        /// <param name="resourceCleanup">The optional callback for cleaning up the resource supplied by
        /// the <paramref name="resourceSupplier"/>.</param>
        /// <param name="eagerCleanup">If true, the per-observer resource is cleaned up before the
        /// terminal event is signaled to the downstream. If false, the cleanup happens after.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> Using<T, S>(Func<S> resourceSupplier, Func<S, IMaybeSource<T>> sourceSelector, Action<S> resourceCleanup = null, bool eagerCleanup = true)
        {
            RequireNonNull(resourceSupplier, nameof(resourceSupplier));
            RequireNonNull(sourceSelector, nameof(sourceSelector));

            return new MaybeUsing<T, S>(resourceSupplier, sourceSelector, resourceCleanup, eagerCleanup);
        }

        /// <summary>
        /// Waits for all maybe sources to produce a success item and
        /// calls the <paramref name="mapper"/> function to generate
        /// the output success value to be signaled to the downstream.
        /// </summary>
        /// <typeparam name="T">The success value type of the <paramref name="sources"/>.</typeparam>
        /// <typeparam name="R">The output success value type.</typeparam>
        /// <param name="mapper">The function receiving the success values of all the
        /// <paramref name="sources"/> and should return the result value to be
        /// signaled as the success value.</param>
        /// <param name="sources">The array of maybe sources to zip together.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.12<br/>
        /// If any of the sources don't succeed, the other sources are disposed and
        /// the output is the completion/exception of that source.
        /// </remarks>
        public static IMaybeSource<R> Zip<T, R>(Func<T[], R> mapper, params IMaybeSource<T>[] sources)
        {
            return Zip(sources, mapper, false);
        }

        /// <summary>
        /// Waits for all maybe sources to produce a success item and
        /// calls the <paramref name="mapper"/> function to generate
        /// the output success value to be signaled to the downstream.
        /// </summary>
        /// <typeparam name="T">The success value type of the <paramref name="sources"/>.</typeparam>
        /// <typeparam name="R">The output success value type.</typeparam>
        /// <param name="mapper">The function receiving the success values of all the
        /// <paramref name="sources"/> and should return the result value to be
        /// signaled as the success value.</param>
        /// <param name="sources">The array of maybe sources to zip together.</param>
        /// <param name="delayErrors">If true, the operator waits for all
        /// sources to terminate, even if some of them didn't produce a success item
        /// and terminates with the aggregate signal. If false, the downstream
        /// is terminated with the terminal event of the first empty source.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IMaybeSource<R> Zip<T, R>(Func<T[], R> mapper, bool delayErrors, params IMaybeSource<T>[] sources)
        {
            return Zip(sources, mapper, delayErrors);
        }

        /// <summary>
        /// Waits for all maybe sources to produce a success item and
        /// calls the <paramref name="mapper"/> function to generate
        /// the output success value to be signaled to the downstream.
        /// </summary>
        /// <typeparam name="T">The success value type of the <paramref name="sources"/>.</typeparam>
        /// <typeparam name="R">The output success value type.</typeparam>
        /// <param name="mapper">The function receiving the success values of all the
        /// <paramref name="sources"/> and should return the result value to be
        /// signaled as the success value.</param>
        /// <param name="sources">The array of maybe sources to zip together.</param>
        /// <param name="delayErrors">If true, the operator waits for all
        /// sources to terminate, even if some of them didn't produce a success item
        /// and terminates with the aggregate signal. If false, the downstream
        /// is terminated with the terminal event of the first empty source.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IMaybeSource<R> Zip<T, R>(this IMaybeSource<T>[] sources, Func<T[], R> mapper, bool delayErrors = false)
        {
            RequireNonNull(sources, nameof(sources));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeZip<T, R>(sources, mapper, delayErrors);
        }

        /// <summary>
        /// Waits for all maybe sources to produce a success item and
        /// calls the <paramref name="mapper"/> function to generate
        /// the output success value to be signaled to the downstream.
        /// </summary>
        /// <typeparam name="T">The success value type of the <paramref name="sources"/>.</typeparam>
        /// <typeparam name="R">The output success value type.</typeparam>
        /// <param name="mapper">The function receiving the success values of all the
        /// <paramref name="sources"/> and should return the result value to be
        /// signaled as the success value.</param>
        /// <param name="sources">The enumerable sequence of maybe sources to zip together.</param>
        /// <param name="delayErrors">If true, the operator waits for all
        /// sources to terminate, even if some of them didn't produce a success item
        /// and terminates with the aggregate signal. If false, the downstream
        /// is terminated with the terminal event of the first empty source.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IMaybeSource<R> Zip<T, R>(this IEnumerable<IMaybeSource<T>> sources, Func<T[], R> mapper, bool delayErrors = false)
        {
            RequireNonNull(sources, nameof(sources));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeZipEnumerable<T, R>(sources, mapper, delayErrors);
        }

        //-------------------------------------------------
        // Instance methods
        //-------------------------------------------------

        /// <summary>
        /// Applies a function to the source at assembly-time and returns the
        /// maybe source returned by this function.
        /// This allows creating reusable set of operators to be applied to maybe sources.
        /// </summary>
        /// <typeparam name="T">The upstream element type.</typeparam>
        /// <typeparam name="R">The element type of the returned maybe source.</typeparam>
        /// <param name="source">The upstream maybe source.</param>
        /// <param name="composer">The function called immediately on <paramref name="source"/>
        /// and should return a maybe source.</param>
        /// <returns>The maybe source returned by the <paramref name="composer"/> function.</returns>
        public static IMaybeSource<R> Compose<T, R>(this IMaybeSource<T> source, Func<IMaybeSource<T>, IMaybeSource<R>> composer)
        {
            return composer(source);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever the
        /// upstream maybe <paramref name="source"/> signals a success item.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoOnSuccess<T>(this IMaybeSource<T> source, Action<T> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onSuccess: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> after the
        /// upstream maybe <paramref name="source"/>'s success
        /// item has been signaled to the downstream.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoAfterSuccess<T>(this IMaybeSource<T> source, Action<T> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onAfterSuccess: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever a
        /// maybe observer subscribes to the maybe <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoOnSubscribe<T>(this IMaybeSource<T> source, Action<IDisposable> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onSubscribe: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever a
        /// maybe observer disposes to the connection to
        /// the maybe <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoOnDispose<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onDispose: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// maybe observer gets completed by
        /// the maybe <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoOnCompleted<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onCompleted: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// maybe observer receives the error signal from
        /// the maybe <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoOnError<T>(this IMaybeSource<T> source, Action<Exception> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onError: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// maybe observer gets terminated normally or with an error by
        /// the maybe <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoOnTerminate<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onTerminate: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> after a
        /// maybe observer gets terminated normally or exceptionally by
        /// the maybe <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoAfterTerminate<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, onAfterTerminate: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> exactly once per maybe
        /// observer and after the maybe observer gets terminated normally
        /// or exceptionally or the observer disposes the connection to the
        /// the maybe <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DoFinally<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return MaybePeek<T>.Create(source, doFinally: handler);
        }

        /// <summary>
        /// If the upstream doesn't terminate within the specified
        /// timeout, the maybe observer is terminated with
        /// a <see cref="TimeoutException"/> or is switched to the optional
        /// <paramref name="fallback"/> maybe source.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to timeout.</param>
        /// <param name="timeout">The time to wait before canceling the source.</param>
        /// <param name="scheduler">The scheduler to use wait for the termination of the upstream.</param>
        /// <param name="fallback">The optional maybe source to switch to if the upstream times out.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> Timeout<T>(this IMaybeSource<T> source, TimeSpan timeout, IScheduler scheduler, IMaybeSource<T> fallback = null)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new MaybeTimeout<T>(source, timeout, scheduler, fallback);
        }

        /// <summary>
        /// Suppresses an upstream error and completes the maybe observer
        /// instead.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to suppress the errors of.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static IMaybeSource<T> OnErrorComplete<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeOnErrorComplete<T>(source);
        }

        /// <summary>
        /// Switches to a <paramref name="fallback"/> maybe source if
        /// the upstream fails.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source that can fail.</param>
        /// <param name="fallback">The fallback maybe source to resume with if <paramref name="source"/> fails.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> OnErrorResumeNext<T>(this IMaybeSource<T> source, IMaybeSource<T> fallback)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(fallback, nameof(fallback));

            return new MaybeOnErrorResumeNext<T>(source, fallback);
        }

        /// <summary>
        /// Switches to a fallback maybe source provided
        /// by a handler function if the main maybe source fails.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source that can fail.</param>
        /// <param name="handler">The function that receives the exception from the main
        /// source and should return a fallback maybe source to resume with.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> OnErrorResumeNext<T>(this IMaybeSource<T> source, Func<Exception, IMaybeSource<T>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new MaybeOnErrorResumeNextSelector<T>(source, handler);
        }

        public static IObservable<T> Repeat<T>(this IMaybeSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> Repeat<T>(this IMaybeSource<T> source, Func<long, bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> RepeatWhen<T, U>(this IMaybeSource<T> source, Func<IObservable<object>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Retry<T>(this IMaybeSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Retry<T>(this IMaybeSource<T> source, Func<Exception, long, bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> RetryWhen<T, U>(this IMaybeSource<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribes to the source on the given scheduler.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The target maybe source to subscribe to</param>
        /// <param name="scheduler">The scheduler to use when subscribing to <paramref name="source"/>.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> SubscribeOn<T>(this IMaybeSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new MaybeSubscribeOn<T>(source, scheduler);
        }

        /// <summary>
        /// Signals the terminal events of the maybe source
        /// through the specified <paramref name="scheduler"/>.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to observe on the specified scheduler.</param>
        /// <param name="scheduler">The scheduler to use.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> ObserveOn<T>(this IMaybeSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new MaybeObserveOn<T>(source, scheduler);
        }

        /// <summary>
        /// When the downstream disposes, the upstream's disposable
        /// is called from the given scheduler.
        /// Note that termination in general doesn't call
        /// <code>Dispose()</code> on the upstream.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to dispose.</param>
        /// <param name="scheduler">The scheduler to use.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> UnsubscribeOn<T>(this IMaybeSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new MaybeUnsubscribeOn<T>(source, scheduler);
        }

        /// <summary>
        /// When the upstream terminates or the downstream disposes,
        /// it detaches the references between the two, avoiding
        /// leaks of one or the other.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to detach from upon termination or cancellation.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> OnTerminateDetach<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeOnTerminateDetach<T>(source);
        }

        /// <summary>
        /// Cache the terminal signal of the upstream
        /// and relay/replay it to current or future
        /// maybe observers.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The upstream maybe source to cache.</param>
        /// <param name="cancel">Called once when subscribing to the source
        /// upon the first subscriber.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IMaybeSource<T> Cache<T>(this IMaybeSource<T> source, Action<IDisposable> cancel = null)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeCache<T>(source, cancel);
        }

        /// <summary>
        /// Delay the delivery of the terminal events from the
        /// upstream maybe source by the given time amount.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to delay signals of.</param>
        /// <param name="time">The time delay.</param>
        /// <param name="scheduler">The scheduler to use for the timed wait and signal emission.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> Delay<T>(this IMaybeSource<T> source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new MaybeDelay<T>(source, time, scheduler);
        }

        /// <summary>
        /// Delay the subscription to the main maybe source
        /// until the specified time elapsed.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to delay subscribing to.</param>
        /// <param name="time">The delay time.</param>
        /// <param name="scheduler">The scheduler to use for the timed wait and subscription.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DelaySubscription<T>(this IMaybeSource<T> source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new MaybeDelaySubscriptionTime<T>(source, time, scheduler);
        }

        /// <summary>
        /// Delay the subscription to the main maybe source
        /// until the other source completes.
        /// </summary>
        /// <typeparam name="T">The success value type main source.</typeparam>
        /// <typeparam name="U">The success value type of the other source.</typeparam>
        /// <param name="source">The maybe source to delay subscribing to.</param>
        /// <param name="other">The source that should complete to trigger the main subscription.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> DelaySubscription<T, U>(this IMaybeSource<T> source, IMaybeSource<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new MaybeDelaySubscription<T, U>(source, other);
        }

        /// <summary>
        /// Terminates when either the main or the other source terminates,
        /// disposing the other sequence.
        /// </summary>
        /// <typeparam name="T">The success value type main source.</typeparam>
        /// <typeparam name="U">The success value type of the other source.</typeparam>
        /// <param name="source">The main completable source to consume.</param>
        /// <param name="other">The other completable source that could stop the <paramref name="source"/>.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> TakeUntil<T, U>(this IMaybeSource<T> source, IMaybeSource<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new MaybeTakeUntil<T, U>(source, other);
        }

        /// <summary>
        /// Terminates when either the main or the other source terminates,
        /// disposing the other sequence.
        /// </summary>
        /// <typeparam name="T">The success value type main source.</typeparam>
        /// <typeparam name="U">The success value type of the other source.</typeparam>
        /// <param name="source">The main completable source to consume.</param>
        /// <param name="other">The other observable that could stop the <paramref name="source"/>
        /// by emitting an item or completing.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> TakeUntil<T, U>(this IMaybeSource<T> source, IObservable<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new MaybeTakeUntilObservable<T, U>(source, other);
        }

        /// <summary>
        /// Maps the success value of the upstream maybe source
        /// into another value.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The result value type</typeparam>
        /// <param name="source">The upstream maybe source to map.</param>
        /// <param name="mapper">The function receiving the upstream success
        /// item and returns a new success item for the downstream.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<R> Map<T, R>(this IMaybeSource<T> source, Func<T, R> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeMap<T, R>(source, mapper);
        }

        /// <summary>
        /// Tests the upstream's success value via a predicate
        /// and relays it if the predicate returns false,
        /// completed the downstream otherwise.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <param name="source">The upstream maybe source to map.</param>
        /// <param name="predicate">The function that receives the upstream
        /// success item and should return true if the success
        /// value should be passed along.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> Filter<T>(this IMaybeSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new MaybeFilter<T>(source, predicate);
        }

        /// <summary>
        /// Maps the upstream success item into a maybe source,
        /// subscribes to it and relays its success or terminal signals
        /// to the downstream.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The value type of the inner maybe source.</typeparam>
        /// <param name="source">The maybe source to map onto another maybe source.</param>
        /// <param name="mapper">The function receiving the upstream success item
        /// and should return a maybe source to subscribe to.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, IMaybeSource<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeFlatMapMaybe<T, R>(source, mapper);
        }

        /// <summary>
        /// Maps the success value of the upstream
        /// maybe source onto an enumerable sequence
        /// and emits the items of this sequence.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <typeparam name="R">The element type of the enumerable sequence.</typeparam>
        /// <param name="source">The maybe source to map.</param>
        /// <param name="mapper">The function receiving the success item and
        /// should return an enumerable sequence.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IObservable<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, IEnumerable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeFlatMapEnumerable<T, R>(source, mapper);
        }

        /// <summary>
        /// Maps the success value of the upstream
        /// maybe source onto an observable sequence
        /// and emits the items of this sequence.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <typeparam name="R">The element type of the enumerable sequence.</typeparam>
        /// <param name="source">The maybe source to map.</param>
        /// <param name="mapper">The function receiving the success item and
        /// should return an observable sequence.</param>
        /// <returns>The new observable sequence.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IObservable<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, IObservable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeFlatMapObservable<T, R>(source, mapper);
        }

        /// <summary>
        /// If the upstream maybe is empty, signal the default value
        /// as the success item to the downstream single observer.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The upstream maybe source.</param>
        /// <param name="defaultItem">The item to signal as the success value if the <paramref name="source"/> is empty.</param>
        /// <returns>The new single source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static ISingleSource<T> DefaultIfEmpty<T>(this IMaybeSource<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeDefaultIfEmpty<T>(source, defaultItem);
        }

        /// <summary>
        /// Switches to the fallbacks if the main source or
        /// the previous fallback is empty.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The upstream maybe source.</param>
        /// <param name="fallbacks">The array of fallback maybe sources to try if the main or
        /// the previous source is null.</param>
        /// <returns>The new single source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> SwitchIfEmpty<T>(this IMaybeSource<T> source, params IMaybeSource<T>[] fallbacks)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(fallbacks, nameof(fallbacks));

            return new MaybeSwitchIfEmptyMany<T>(source, fallbacks);
        }

        /// <summary>
        /// Switches the fallback a single source if the main source is empty.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The upstream maybe source.</param>
        /// <param name="fallback">The single source to switch to if the main <paramref name="source"/> is empty.</param>
        /// <returns>The new single source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static ISingleSource<T> SwitchIfEmpty<T>(this IMaybeSource<T> source, ISingleSource<T> fallback)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(fallback, nameof(fallback));

            return new MaybeSwitchIfEmpty<T>(source, fallback);
        }

        /// <summary>
        /// Hides the identity and disposable of the upstream from
        /// the downstream.
        /// </summary>
        /// <param name="source">The maybe source to hide.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IMaybeSource<T> Hide<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeHide<T>(source);
        }


        // ------------------------------------------------
        // Leaving the reactive world
        // ------------------------------------------------

        /// <summary>
        /// Subscribes to this maybe source and suppresses exceptions
        /// throw by the OnXXX methods of the <paramref name="observer"/>.
        /// </summary>
        /// <param name="source">The maybe source to subscribe to safely.</param>
        /// <param name="observer">The unreliable observer.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void SubscribeSafe<T>(this IMaybeSource<T> source, IMaybeObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            source.Subscribe(new MaybeSafeObserver<T>(observer));
        }

        /// <summary>
        /// Subscribe to this maybe source and call the
        /// appropriate action depending on the success or terminal signal received.
        /// </summary>
        /// <param name="source">The maybe source to observe.</param>
        /// <param name="onSuccess">Called with the success item when the maybe source succeeds.</param>
        /// <param name="onError">Called with the exception when the maybe source terminates with an error.</param>
        /// <param name="onCompleted">Called when the maybe source completes normally.</param>
        /// <returns>The disposable that allows canceling the source.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IDisposable Subscribe<T>(this IMaybeSource<T> source, Action<T> onSuccess = null, Action<Exception> onError = null, Action onCompleted = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new MaybeLambdaObserver<T>(onSuccess, onError, onCompleted);
            source.Subscribe(parent);
            return parent;
        }

        /// <summary>
        /// Subscribes to the source and blocks until it terminated, then
        /// calls the appropriate , maybe observer method on the current
        /// thread.
        /// </summary>
        /// <param name="source">The upstream maybe source to block for.</param>
        /// <param name="observer">The maybe observer to call the methods on the current thread.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void BlockingSubscribe<T>(this IMaybeSource<T> source, IMaybeObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            var parent = new MaybeBlockingObserver<T>(observer);
            observer.OnSubscribe(parent);

            source.Subscribe(parent);

            parent.Run();
        }

        /// <summary>
        /// Subscribes to the source and blocks until it terminated, then
        /// calls the appropriate maybe observer method on the current
        /// thread.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The upstream maybe source to block for.</param>
        /// <param name="onSuccess">Action called with the success item.</param>
        /// <param name="onError">Action called with the exception when the upstream fails.</param>
        /// <param name="onCompleted">Action called when the upstream completes.</param>
        /// <param name="onSubscribe">Action called with a disposable just before subscribing to the upstream
        /// and allows disposing the sequence and unblocking this method call.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void BlockingSubscribe<T>(this IMaybeSource<T> source, Action<T> onSuccess = null, Action<Exception> onError = null, Action onCompleted = null, Action <IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new MaybeBlockingConsumer<T>(onSuccess, onError, onCompleted);
            onSubscribe?.Invoke(parent);

            source.Subscribe(parent);

            parent.Run();
        }

        /// <summary>
        /// Wait until the upstream terminates and rethrow any exception it
        /// signaled.
        /// </summary>
        /// <param name="source">The maybe source to wait for.</param>
        /// <param name="timeoutMillis">The maximum time to wait for termination.</param>
        /// <param name="cts">The means to cancel the wait from outside.</param>
        /// <exception cref="TimeoutException">If a timeout happens, which also cancels the upstream.</exception>
        /// <remarks>Since 0.0.11</remarks>
        public static void Wait<T>(this IMaybeSource<T> source, int timeoutMillis = int.MaxValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new MaybeWait<T>();
            source.Subscribe(parent);

            parent.Wait(timeoutMillis, cts);
        }

        /// <summary>
        /// Wait until the upstream terminates and
        /// return its success value or rethrow any exception it
        /// signaled.
        /// </summary>
        /// <param name="source">The maybe source to wait for.</param>
        /// <param name="result">The success value if the method returns true</param>
        /// <param name="timeoutMillis">The maximum time to wait for termination.</param>
        /// <param name="cts">The means to cancel the wait from outside.</param>
        /// <returns>True if the source succeeded.</returns>
        /// <exception cref="TimeoutException">If a timeout happens, which also cancels the upstream.</exception>
        /// <remarks>Since 0.0.11</remarks>
        public static bool Wait<T>(this IMaybeSource<T> source, out T result, int timeoutMillis = int.MaxValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new MaybeWaitValue<T>();
            source.Subscribe(parent);

            return parent.Wait(out result, timeoutMillis, cts);
        }

        /// <summary>
        /// Subscribes a maybe observer (subclass) to the maybe
        /// source and returns this observer instance as well.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <typeparam name="U">The observer type.</typeparam>
        /// <param name="source">The maybe source to subscribe to.</param>
        /// <param name="observer">The maybe observer (subclass) to subscribe with.</param>
        /// <returns>The <paramref name="observer"/> provided as parameter.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static U SubscribeWith<T, U>(this IMaybeSource<T> source, U observer) where U : IMaybeObserver<T>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            source.Subscribe(observer);
            return observer;
        }

        //-------------------------------------------------
        // Interoperation with other reactive types
        //-------------------------------------------------

        public static IObservable<R> ConcatMap<T, R>(this IObservable<T> source, Func<T, IMaybeSource<R>> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IObservable<R> FlatMap<T, R>(this IObservable<T> source, Func<T, IMaybeSource<R>> mapper, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Maps the upstream success item into a single source,
        /// subscribes to it and relays its success or failure signals
        /// to the downstream.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The value type of the inner single source.</typeparam>
        /// <param name="source">The maybe source to map onto another single source.</param>
        /// <param name="mapper">The function receiving the upstream success item
        /// and should return a single source to subscribe to.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11<br/>
        /// Note that the result type remains IMaybeSource because the
        /// <paramref name="source"/> may be empty and thus the resulting
        /// sequence must be able to represent emptiness.
        /// </remarks>
        public static IMaybeSource<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, ISingleSource<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeFlatMapSingle<T, R>(source, mapper);
        }

        /// <summary>
        /// Maps the success value of the upstream maybe source
        /// into a completable source and signals its terminal
        /// events to the downstream.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe source.</typeparam>
        /// <param name="source">The maybe source to map into a completable source.</param>
        /// <param name="mapper">The function that takes the success value from the upstream
        /// and returns a completable source to subscribe to and relay terminal events of.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource FlatMap<T>(this IMaybeSource<T> source, Func<T, ICompletableSource> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new CompletableFlatMapMaybe<T>(source, mapper);
        }

        public static IObservable<R> SwitchMap<T, R>(this IObservable<T> source, Func<T, IMaybeSource<T>> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Ignores the success signal of the maybe source and
        /// completes the downstream completable observer instead.
        /// </summary>
        /// <typeparam name="T">The success value type of the source.</typeparam>
        /// <param name="source">The source to ignore the success value of.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ICompletableSource IgnoreElement<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableIgnoreElementMaybe<T>(source);
        }

        /// <summary>
        /// Signals the first as the
        /// success value or completes if the observable
        /// sequence is empty.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The observable sequence to get the first element from.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FirstElement<T>(this IObservable<T> source)
        {
            return ElementAtIndex(source, 0L);
        }

        /// <summary>
        /// Signals the only element of the observable sequence,
        /// completes if the sequence is empty or fails
        /// with an error if the source contains more than one element.
        /// </summary>
        /// <typeparam name="T">The value type of the source observable.</typeparam>
        /// <param name="source">The source observable sequence to get the single element from.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> SingleElement<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeSingleElement<T>(source);
        }

        /// <summary>
        /// Signals the last element of the observable sequence
        /// or completes if the sequence is empty.
        /// </summary>
        /// <typeparam name="T">The value type of the source observable.</typeparam>
        /// <param name="source">The source observable sequence.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> LastElement<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeLastElement<T>(source);
        }

        /// <summary>
        /// Signals the element at the specified index as the
        /// success value or completes if the observable
        /// sequence is shorter than the specified index.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The observable sequence to get an element from.</param>
        /// <param name="index">The index of the element to get (zero based).</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> ElementAtIndex<T>(this IObservable<T> source, long index)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(index, nameof(index));

            return new MaybeElementAt<T>(source, index);
        }

        /// <summary>
        /// Subscribe to a maybe source and expose the terminal
        /// signal as a <see cref="Task"/>.
        /// </summary>
        /// <param name="source">The source maybe to convert.</param>
        /// <param name="cts">The cancellation token source to watch for external cancellation.</param>
        /// <returns>The new task instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static Task<T> ToTask<T>(this IMaybeSource<T> source, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new MaybeToTask<T>();
            parent.Init(cts);
            source.Subscribe(parent);
            return parent.Task;
        }

        /// <summary>
        /// Converts an ongoing or already terminated task to a maybe source 
        /// and relays its terminal event to observers.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="task">The task to observe as a maybe source.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11<br/>
        /// Note that the <see cref="Task"/> API uses an <see cref="AggregateException"/>
        /// to signal there were one or more errors.
        /// </remarks>
        public static IMaybeSource<T> ToMaybe<T>(this Task task)
        {
            RequireNonNull(task, nameof(task));

            return new MaybeFromTaskPlain<T>(task);
        }

        /// <summary>
        /// Converts an ongoing or already terminated task to a maybe source 
        /// and relays its value or error to observers.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="task">The task to observe as a maybe source.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11<br/>
        /// Note that the <see cref="Task{TResult}"/> API uses an <see cref="AggregateException"/>
        /// to signal there were one or more errors.
        /// </remarks>
        public static IMaybeSource<T> ToMaybe<T>(this Task<T> task)
        {
            RequireNonNull(task, nameof(task));

            return new MaybeFromTask<T>(task);
        }

        /// <summary>
        /// Converts a maybe source into a single source,
        /// failing with an index out-of-range exception
        /// if the maybe source is empty
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <param name="source">The maybe source to expose as a single source.</param>
        /// <returns>The new single source instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static ISingleSource<T> ToSingle<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeToSingle<T>(source);
        }

        /// <summary>
        /// Exposes a maybe source as a legacy observable.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe and observable sequence.</typeparam>
        /// <param name="source">The maybe source to expose as an <see cref="IObservable{T}"/></param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.12</remarks>
        public static IObservable<T> ToObservable<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeToObservable<T>(source);
        }
    }
}
