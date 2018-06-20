using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableSkipUntil<T, U> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly IAsyncEnumerable<U> other;

        public AsyncEnumerableSkipUntil(IAsyncEnumerable<T> source, IAsyncEnumerable<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var otherEnum = other.GetAsyncEnumerator();
            var parent = new SkipUntilMainAsyncEnumerator(source.GetAsyncEnumerator(), otherEnum);

            parent.RunOther();
            return parent;
        }

        sealed class SkipUntilMainAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> enumerator;

            readonly IAsyncEnumerator<U> otherEnumerator;

            T current;

            TaskCompletionSource<bool> currentTask;

            bool gate;

            int wip;

            public SkipUntilMainAsyncEnumerator(IAsyncEnumerator<T> enumerator, IAsyncEnumerator<U> otherEnumerator)
            {
                this.enumerator = enumerator;
                this.otherEnumerator = otherEnumerator;
            }

            public T Current => current;

            public Task DisposeAsync()
            {
                var cts = Interlocked.Exchange(ref currentTask, AsyncHelper.CompletedSource);

                if (cts != AsyncHelper.CompletedSource)
                {
                    cts?.TrySetCanceled();
                }

                return Task.WhenAll(enumerator.DisposeAsync(), otherEnumerator.DisposeAsync());
            }

            void MoveNext()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    do
                    {
                        current = default;

                        enumerator.MoveNextAsync().ContinueWith((s, t) =>
                        {
                            var @this = (SkipUntilMainAsyncEnumerator)t;
                            var ct = Volatile.Read(ref @this.currentTask);
                            if (ct != AsyncHelper.CompletedSource)
                            {
                                if (s.IsFaulted)
                                {
                                    ct.TrySetException(AsyncHelper.Unwrap(s.Exception));
                                }
                                else
                                {
                                    if (s.Result)
                                    {
                                        if (Volatile.Read(ref gate))
                                        {
                                            current = @this.enumerator.Current;
                                            ct.TrySetResult(true);
                                        }
                                        else
                                        {
                                            @this.MoveNext();
                                        }
                                    }
                                    else
                                    {
                                        ct.TrySetResult(false);
                                    }
                                }
                            }
                        }, this);

                    }
                    while (Interlocked.Decrement(ref wip) != 0);
                }
            }

            public Task<bool> MoveNextAsync()
            {
                var curr = Volatile.Read(ref currentTask);
                if (curr == AsyncHelper.CompletedSource)
                {
                    return AsyncHelper.FalseTask;
                }
                var cts = new TaskCompletionSource<bool>();

                if (Interlocked.CompareExchange(ref currentTask, cts, curr) != curr)
                {
                    return AsyncHelper.FalseTask;
                }

                MoveNext();

                return cts.Task;
            }

            internal void RunOther()
            {
                otherEnumerator.MoveNextAsync().ContinueWith((s, t) => 
                {
                    var @this = (SkipUntilMainAsyncEnumerator)t;
                    var cts = Volatile.Read(ref @this.currentTask);
                    if (cts != AsyncHelper.CompletedSource)
                    {
                        if (s.IsFaulted)
                        {
                            cts?.TrySetException(AsyncHelper.Unwrap(s.Exception));
                        }
                        else
                        {
                            Volatile.Write(ref @this.gate, true);
                        }
                    }
                }, this);
            }
            
        }
    }
}
