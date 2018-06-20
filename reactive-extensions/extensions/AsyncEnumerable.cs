using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension methods for building and composing
    /// <see cref="IAsyncEnumerable{T}"/> sequences.
    /// </summary>
    /// <remarks>Since 0.0.25</remarks>
    public static class AsyncEnumerable
    {
        // -------------------------------------------------------------------------------------
        // Factory methods
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// An empty async enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <returns>The shared singleton empty async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Empty<T>()
        {
            return AsyncEnumerableEmpty<T>.Instance;
        }

        /// <summary>
        /// An empty async enumerable sequence that never signals
        /// an item nor terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <returns>The shared singleton never async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Never<T>()
        {
            return AsyncEnumerableNever<T>.Instance;
        }

        /// <summary>
        /// An async enumerable sequence of the single item provided.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="item">The only item to signal.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Just<T>(T item)
        {
            return new AsyncEnumerableJust<T>(item);
        }

        /// <summary>
        /// An async enumerable sequence of monotonically increasing numbers.
        /// </summary>
        /// <param name="start">The first value of the range.</param>
        /// <param name="count">The number of items to signal, non-negative.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<int> Range(int start, int count)
        {
            RequireNonNegative(count, nameof(count));

            return new AsyncEnumerableRange(start, start + count);
        }

        /// <summary>
        /// Signals an error task upon enumeration.
        /// </summary>
        /// <typeparam name="T">The target element type of the sequence.</typeparam>
        /// <param name="error">The Exception to signal via MoveNextAsync's returned Task.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new AsyncEnumerableError<T>(error);
        }

        /// <summary>
        /// Defers the creation of the actual async enumerable instance.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <param name="factory">The function called when the resulting
        /// <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator()"/> is called and should return
        /// the actual async enumerable to be consumed.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Defer<T>(Func<IAsyncEnumerable<T>> factory)
        {
            RequireNonNull(factory, nameof(factory));

            return new AsyncEnumerableDefer<T>(factory);
        }

        /// <summary>
        /// Concatenates the elements of multiple async enumerable sources
        /// by running them one-by-one and non-overlapping fashion, relaying elements in order
        /// they are presented by each source sequence.
        /// </summary>
        /// <typeparam name="T">The element type of the inner and result sequences.</typeparam>
        /// <param name="sources">The array of async enumerable sources.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Concat<T>(params IAsyncEnumerable<T>[] sources)
        {
            RequireNonNull(sources, nameof(sources));

            return new AsyncEnumerableConcatArray<T>(sources);
        }

        /// <summary>
        /// Wraps an IEnumerable source and exposes it as a
        /// async enumerable.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The source enumerable to expose as an async enumerable.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> FromEnumerable<T>(IEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new AsyncEnumerableFromEnumerable<T>(source);
        }

        /// <summary>
        /// Wraps a valueless task into an async enumerable sequence
        /// which completes when the task completes.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="source">The source task to wrap.</param>
        /// <returns>The new async enumerable instance.</returns>
        /// <remarks>Since 0.0.26</remarks>
        public static IAsyncEnumerable<T> FromTask<T>(Task source)
        {
            RequireNonNull(source, nameof(source));

            return new AsyncEnumerableFromTaskPlain<T>(source);
        }

        /// <summary>
        /// Wraps a generic task into an async enumerable sequence
        /// which completes when the task completes.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="source">The source task to wrap.</param>
        /// <returns>The new async enumerable instance.</returns>
        /// <remarks>Since 0.0.26</remarks>
        public static IAsyncEnumerable<T> FromTask<T>(Task<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new AsyncEnumerableFromTask<T>(source);
        }

        // -------------------------------------------------------------------------------------
        // Instance/In-sequence methods
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Maps each source async enumerable element into another value
        /// via a function.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The result value type.</typeparam>
        /// <param name="source">The source async enumerable sequence to map.</param>
        /// <param name="mapper">The function receiving the upstream item and should
        /// produce the value to be signaled downstream.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<R> Map<T, R>(this IAsyncEnumerable<T> source, Func<T, R> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new AsyncEnumerableMap<T, R>(source, mapper);
        }

        /// <summary>
        /// Filters elements from the source async enumerable and lets only
        /// those through for which the predicate returns true.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <param name="source">The source to filter elements of.</param>
        /// <param name="predicate">The predicate receiving the next item from upstream
        /// and should return true if that item should be relayed to the downstream.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new AsyncEnumerableFilter<T>(source, predicate);
        }

        /// <summary>
        /// Relays at most the given number of items from the upstream and completes.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <param name="source">The source to take some items from.</param>
        /// <param name="n">The number of items to take.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, long n)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(n, nameof(n));

            return new AsyncEnumerableTake<T>(source, n);
        }

        /// <summary>
        /// Skips at most the given number of items from the beginning of the upstream sequence,
        /// then relays the rest.
        /// </summary>
        /// <typeparam name="T">The element type of the async sequence.</typeparam>
        /// <param name="source">The source to skip elements of.</param>
        /// <param name="n">The number of items to skip.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, long n)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(n, nameof(n));

            return new AsyncEnumerableSkip<T>(source, n);
        }

        /// <summary>
        /// Creates a collection for each async enumerator requested
        /// and collects items from the source into it via a collector function,
        /// then signals the collection as a single result.
        /// </summary>
        /// <typeparam name="T">The element type of the source.</typeparam>
        /// <typeparam name="C">The type of the collection.</typeparam>
        /// <param name="source">The upstream async enumerable source.</param>
        /// <param name="collectionSupplier">The function called for each requested async
        /// enumerator and should return a collection to be passed to <paramref name="collector"/> and
        /// as a result.</param>
        /// <param name="collector">The action receiving the collection instance and the current
        /// upstream item.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<C> Collect<T, C>(this IAsyncEnumerable<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(collectionSupplier, nameof(collectionSupplier));
            RequireNonNull(collector, nameof(collector));

            return new AsyncEnumerableCollect<T, C>(source, collectionSupplier, collector);
        }

        /// <summary>
        /// Reduces the source async enumerable into a single value by
        /// applying a function to the previous accumulator value and
        /// the current upstream value to produce the next accumulator value.
        /// This accumulator value is then signaled to the downstream.
        /// If the source is empty, this async enumerable will be empty too.
        /// </summary>
        /// <typeparam name="T">The element type of the source and the result.</typeparam>
        /// <param name="source">The sequence to reduce via a function to a single value.</param>
        /// <param name="reducer">The function that receives the previous accumulator
        /// value (or the very first upstream item) and the current upstream item and
        /// should return the new accumulated value.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> Reduce<T>(this IAsyncEnumerable<T> source, Func<T, T, T> reducer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(reducer, nameof(reducer));

            return new AsyncEnumerableReducePlain<T>(source, reducer);
        }

        /// <summary>
        /// Reduces the source async enumerable into a single value by
        /// applying a function to the previous accumulator value and
        /// the current upstream value to produce the next accumulator value.
        /// This accumulator value is then signaled to the downstream.
        /// </summary>
        /// <typeparam name="T">The element type of the source and the result.</typeparam>
        /// <typeparam name="R">The type of the accumulator and result.</typeparam>
        /// <param name="source">The sequence to reduce via a function to a single value.</param>
        /// <param name="initialSupplier">Function that provides the initial accumulator value.</param>
        /// <param name="reducer">The function that receives the previous accumulator
        /// value (or the very first upstream item) and the current upstream item and
        /// should return the new accumulated value.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<R> Reduce<T, R>(this IAsyncEnumerable<T> source, Func<R> initialSupplier, Func<R, T, R> reducer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(initialSupplier, nameof(initialSupplier));
            RequireNonNull(reducer, nameof(reducer));

            return new AsyncEnumerableReduce<T, R>(source, initialSupplier, reducer);
        }

        /// <summary>
        /// Maps each upstream async item into an IEnumerable and relays their items to the downstream
        /// in order..
        /// </summary>
        /// <typeparam name="T">The element type of the upstream source.</typeparam>
        /// <typeparam name="R">The element type of the resulting async sequence and the
        /// inner IEnumerables.</typeparam>
        /// <param name="source">The source to map to IEnumerables and relay their items.</param>
        /// <param name="mapper">The mapper that receives an upstream item and should
        /// return an IEnumerable to be emitted.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<R> ConcatMap<T, R>(this IAsyncEnumerable<T> source, Func<T, IEnumerable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new AsyncEnumerableConcatMapEnumerable<T, R>(source, mapper);
        }

        /// <summary>
        /// Relays items from the source async enumerable until the other 
        /// async enumerable signals an item or completes.
        /// </summary>
        /// <typeparam name="T">The element type of the main and result async sequences.</typeparam>
        /// <typeparam name="U">The element type of the termination-providing async sequence.</typeparam>
        /// <param name="source">The source async sequence to relay elements of.</param>
        /// <param name="other">The async sequence to signal the termination of the main sequence.</param>
        /// <returns>The new async enumerable instance.</returns>
        /// <remarks>Since 0.0.26</remarks>
        public static IAsyncEnumerable<T> TakeUntil<T, U>(this IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new AsyncEnumerableTakeUntil<T, U>(source, other);
        }

        /// <summary>
        /// Skips items from the source async enumerable until the other 
        /// async enumerable signals an item or completes.
        /// </summary>
        /// <typeparam name="T">The element type of the main and result async sequences.</typeparam>
        /// <typeparam name="U">The element type of the termination-providing async sequence.</typeparam>
        /// <param name="source">The source async sequence to relay elements of.</param>
        /// <param name="other">The async sequence to open the gate on the main sequence.</param>
        /// <returns>The new async enumerable instance.</returns>
        /// <remarks>Since 0.0.26</remarks>
        public static IAsyncEnumerable<T> SkipUntil<T, U>(this IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new AsyncEnumerableSkipUntil<T, U>(source, other);
        }

        // -------------------------------------------------------------------------------------
        // Consumer methods
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Blocks until the async enumerable produces its first item and
        /// returns it.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence and return item.</typeparam>
        /// <param name="source">The source async sequence to take the first element of.</param>
        /// <returns>The first element of the async sequence.</returns>
        /// <exception cref="OperationCanceledException">If the underlying operation is canceled.</exception>
        /// <exception cref="IndexOutOfRangeException">If the source is empty</exception>
        public static T BlockingFirst<T>(this IAsyncEnumerable<T> source)
        {
            var en = source.GetAsyncEnumerator();

            try
            {
                var task = en.MoveNextAsync();

                for (; ; )
                {
                    if (task.IsCanceled)
                    {
                        throw new OperationCanceledException();
                    }
                    else if (task.IsFaulted)
                    {
                        var ex = task.Exception;
                        if (ex.InnerExceptions.Count == 1)
                        {
                            throw ex.InnerExceptions[0];
                        }
                        throw ex;
                    }
                    else if (task.IsCompleted)
                    {
                        return task.Result ? en.Current : throw new IndexOutOfRangeException("Empty source IAsyncEnumerable");
                    }

                    task.Wait();
                }
            }
            finally
            {
                en.DisposeAsync();
            }
        }

        /// <summary>
        /// Test the source by exhaustively consuming it and returning
        /// a TestObserver with the results.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The async enumerable sequence to test.</param>
        /// <returns>The task with the TestObserver instance</returns>
        public static async Task<TestObserver<T>> TestAsync<T>(this IAsyncEnumerable<T> source)
        {
            var to = new TestObserver<T>();
            var en = source.GetAsyncEnumerator();
            try
            {
                while (await en.MoveNextAsync())
                {
                    to.OnNext(en.Current);
                }
                to.OnCompleted();
            }
            catch (Exception ex)
            {
                to.OnError(ex);
            }
            finally
            {
                await en.DisposeAsync();
            }
            return to;
        }

        /// <summary>
        /// Test the source by exhaustively consuming it, requiring it to support the
        /// <see cref="IAsyncFusedEnumerator{T}"/> interface, and returning
        /// a TestObserver with the results.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The async enumerable sequence to test.</param>
        /// <returns>The task with the TestObserver instance</returns>
        public static async Task<TestObserver<T>> TestAsyncFused<T>(this IAsyncEnumerable<T> source)
        {
            var to = new TestObserver<T>();
            var en = source.GetAsyncEnumerator();
            var f = en as IAsyncFusedEnumerator<T>;
            if (f == null)
            {
                throw new ArgumentException("The IAsyncEnumerator returned by source is not fuseable");
            }
            try
            {
                for (; ; )
                {
                    var v = f.TryPoll(out var state);

                    if (state == AsyncFusedState.Ready)
                    {
                        to.OnNext(v);
                    }
                    else if (state == AsyncFusedState.Terminated)
                    {
                        to.OnCompleted();
                        break;
                    }
                    else if (!await en.MoveNextAsync())
                    {
                        to.OnCompleted();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                to.OnError(ex);
            }
            finally
            {
                await en.DisposeAsync();
            }
            return to;
        }

        // -------------------------------------------------------------------------------------
        // Interoperation methods
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Wraps an IEnumerable source and exposes it as a
        /// async enumerable.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences.</typeparam>
        /// <param name="source">The source enumerable to expose as an async enumerable.</param>
        /// <returns>The new async enumerable instance.</returns>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new AsyncEnumerableFromEnumerable<T>(source);
        }

        /// <summary>
        /// Wraps a valueless task into an async enumerable sequence
        /// which completes when the task completes.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="source">The source task to wrap.</param>
        /// <returns>The new async enumerable instance.</returns>
        /// <remarks>Since 0.0.26</remarks>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task source)
        {
            RequireNonNull(source, nameof(source));

            return new AsyncEnumerableFromTaskPlain<T>(source);
        }

        /// <summary>
        /// Wraps a generic task into an async enumerable sequence
        /// which completes when the task completes.
        /// </summary>
        /// <typeparam name="T">The target element type.</typeparam>
        /// <param name="source">The source task to wrap.</param>
        /// <returns>The new async enumerable instance.</returns>
        /// <remarks>Since 0.0.26</remarks>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new AsyncEnumerableFromTask<T>(source);
        }
    }
}
