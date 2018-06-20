using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Helper methods and instances for the async-enumerable implementations.
    /// </summary>
    /// <remarks>Since 0.0.25</remarks>
    internal static class AsyncHelper
    {
        /// <summary>
        /// A completed task with true value.
        /// </summary>
        internal static readonly Task<bool> TrueTask = Task.FromResult(true);

        /// <summary>
        /// A completed task with false value.
        /// </summary>
        internal static readonly Task<bool> FalseTask = Task.FromResult(false);
    }
}
