using BenchmarkDotNet.Attributes;
using System;
using akarnokd.reactive_extensions;
using System.Threading;

namespace akarnokd.reactive_extensions_benchmarks
{
    [Config(typeof(FastAndDirtyConfig))]
    public class ObserveOnPerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        [Benchmark]
        public void Range_Baseline()
        {
            BlockingSubscribe(FastRange(1, N));
        }

        /*
        [Benchmark]
        public void ObserveOn()
        {
            Observable.Range(1, N)
                    .ObserveOn(Scheduler.CurrentThread)
                    .Wait();
        }
        */

        [Benchmark]
        public void ObserveOn_Ext()
        {
            BlockingSubscribe(FastRange(1, N)
                    .ObserveOn(akarnokd.reactive_extensions.ImmediateScheduler.INSTANCE, false)
                );
        }

        [Benchmark]
        public void ObserveOn_Ext_DelayError()
        {
            BlockingSubscribe(FastRange(1, N)
                    .ObserveOn(akarnokd.reactive_extensions.ImmediateScheduler.INSTANCE, true)
                );
        }

        IObservable<int> FastRange(int start, int count)
        {
            return new FastRangeObservable(start, count);
        }

        sealed class FastRangeObservable : IObservable<int>
        {
            readonly int start;

            readonly int end;

            public FastRangeObservable(int start, int count)
            {
                this.start = start;
                this.end = start + count;
            }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                for (int i = start; i < end; i++)
                {
                    observer.OnNext(i);
                }
                observer.OnCompleted();
                return DisposableHelper.EMPTY;
            }
        }

        void BlockingSubscribe<T>(IObservable<T> source)
        {
            var b = new BlockingObserver<T>();
            source.Subscribe(b);
            b.Block();
        }

        sealed class BlockingObserver<T> : IObserver<T>
        {
            readonly CountdownEvent cde;

            internal BlockingObserver()
            {
                this.cde = new CountdownEvent(1);
            }

            public void OnCompleted()
            {
                cde.Signal();
            }

            public void OnError(Exception error)
            {
                cde.Signal();
            }

            public void OnNext(T value)
            {
                // ignored
            }

            public void Block()
            {
                if (cde.CurrentCount != 0)
                {
                    cde.Wait();
                }
            }
        }

    }
}
