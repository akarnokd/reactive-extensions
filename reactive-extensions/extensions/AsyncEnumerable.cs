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

        // -------------------------------------------------------------------------------------
        // Instance/In-sequence methods
        // -------------------------------------------------------------------------------------


        // -------------------------------------------------------------------------------------
        // Consumer methods
        // -------------------------------------------------------------------------------------

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
                        continue;
                    }
                    else if (state == AsyncFusedState.Terminated)
                    {
                        to.OnCompleted();
                        break;
                    }

                    if (!await en.MoveNextAsync())
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

    }
}
