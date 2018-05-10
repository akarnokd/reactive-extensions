using BenchmarkDotNet.Attributes;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace akarnokd.reactive_extensions_benchmarks
{
    [Config(typeof(FastAndDirtyConfig))]
    public class QueuePerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        List<int> list = new List<int>();

        ConcurrentQueue<int> cq = new ConcurrentQueue<int>();

        SpscLinkedArrayQueue<int> aq = new SpscLinkedArrayQueue<int>(32);

        int wip;

        [Benchmark]
        public void List_Baseline()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                list.Add(0);
                list.RemoveAt(0);
            }
        }

        [Benchmark]
        public void List_Baseline_Atomics()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                list.Add(0);
                Interlocked.Increment(ref wip);
                list.RemoveAt(0);
                Interlocked.Decrement(ref wip);
            }
        }

        [Benchmark]
        public void ConcurrentQueue()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                cq.Enqueue(0);
                cq.TryDequeue(out var _);
            }
        }

        [Benchmark]
        public void ConcurrentQueue_Atomics()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                cq.Enqueue(0);
                Interlocked.Increment(ref wip);
                cq.TryDequeue(out var _);
                Interlocked.Decrement(ref wip);
            }
        }

        [Benchmark]
        public void ConcurrentQueue_Long()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                cq.Enqueue(0);
            }
            for (int i = 0; i < n; i++)
            {
                cq.TryDequeue(out var _);
            }
        }

        [Benchmark]
        public void ConcurrentQueue_Long_Atomics()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                cq.Enqueue(0);
                Interlocked.Increment(ref wip);
            }
            for (int i = 0; i < n; i++)
            {
                cq.TryDequeue(out var _);
            }
            Interlocked.Add(ref wip, -n);
        }


        [Benchmark]
        public void SpscLinkedArrayQueue()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                aq.Offer(0);
                aq.TryPoll(out var _);
            }
        }

        [Benchmark]
        public void SpscLinkedArrayQueue_Atomics()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                aq.Offer(0);
                Interlocked.Increment(ref wip);
                aq.TryPoll(out var _);
                Interlocked.Decrement(ref wip);
            }
        }

        [Benchmark]
        public void SpscLinkedArrayQueue_Long()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                aq.Offer(0);
            }
            for (int i = 0; i < n; i++)
            {
                aq.TryPoll(out var _);
            }
        }

        [Benchmark]
        public void SpscLinkedArrayQueue_Long_Atomics()
        {
            var n = N;
            for (int i = 0; i < n; i++)
            {
                aq.Offer(0);
                Interlocked.Increment(ref wip);
            }
            for (int i = 0; i < n; i++)
            {
                aq.TryPoll(out var _);
            }
            Interlocked.Add(ref wip, -n);
        }
    }
}
