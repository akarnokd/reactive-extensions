using BenchmarkDotNet.Running;
using System;

namespace akarnokd.reactive_extensions_benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            // Yes, these have to be manually enabled and disabled!
            //BenchmarkRunner.Run<ConcatMapXPerf>();
            //BenchmarkRunner.Run<ObserveOnPerf>();
            //BenchmarkRunner.Run<QueuePerf>();
            //BenchmarkRunner.Run<FlatMapXPerf>();
            //BenchmarkRunner.Run<ConcatMapEagerXPerf>();
            //BenchmarkRunner.Run<XMapYPerf>();
            BenchmarkRunner.Run<MergeMapXPerf>();
            //BenchmarkRunner.Run<AsyncEnumJustPerf>();
            //BenchmarkRunner.Run<AsyncEnumRangePerf>();
            // --------------------------------------------------
            Console.ReadLine();
        }
    }
}
