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

        /// <summary>
        /// A task completion source indicating a terminated task.
        /// Do not leak this.
        /// </summary>
        /// <remarks>Since 0.0.26</remarks>
        internal static readonly TaskCompletionSource<bool> CompletedSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Unwrap an aggregate exception if it contains only a single inner exception.
        /// </summary>
        /// <param name="ex">The aggregate exception to unwrap.</param>
        /// <returns>The unwrapped exception.</returns>
        /// <remarks>Since 0.0.26</remarks>
        internal static Exception Unwrap(AggregateException ex)
        {
            if (ex.InnerExceptions.Count == 1)
            {
                return ex.InnerExceptions[0];
            }
            return ex;
        }
    }
}
