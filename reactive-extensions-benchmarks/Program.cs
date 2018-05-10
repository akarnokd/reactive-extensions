using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            // Yes, these have to be manually enabled and disabled!
            BenchmarkRunner.Run<ConcatMapXPerf>();


            // --------------------------------------------------
            Console.ReadLine();
        }
    }
}
