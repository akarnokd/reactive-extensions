using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Horology;

namespace akarnokd.reactive_extensions_benchmarks
{
    public class FastAndDirtyConfig : ManualConfig
    {
        public FastAndDirtyConfig()
        {
            Add(Job.Default
                .WithLaunchCount(1)
                .WithIterationTime(TimeInterval.FromSeconds(1))
                .WithWarmupCount(5)
                .WithTargetCount(5)
            );
        }
    }
}
