using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal static class AsyncCloseable
    {
        internal static IAsyncCloseable Empty { get; } = new EmptyAsyncCloseable();
    }

    internal sealed class EmptyAsyncCloseable : IAsyncCloseable
    {
        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }
    }
}
