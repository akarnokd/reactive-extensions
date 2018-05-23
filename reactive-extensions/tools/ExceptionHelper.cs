using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Utility methods to deal with normal and composite exceptions and
    /// Exception fields atomically.
    /// </summary>
    internal static class ExceptionHelper
    {
        internal static readonly Exception TERMINATED = new Exception("No further exceptions");

        internal static bool AddException(ref Exception field, Exception ex)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref field);
                if (a == TERMINATED)
                {
                    return false;
                }
                var b = default(Exception);
                if (a == null)
                {
                    b = ex;
                }
                else
                if (a is AggregateException g)
                {
                    b = new AggregateException(g.InnerExceptions.Concat(new[] { ex }));
                }
                else
                {
                    b = new AggregateException(a, ex);
                }

                if (Interlocked.CompareExchange(ref field, b, a) == a)
                {
                    return true;
                }
            }
        }

        internal static Exception Terminate(ref Exception field)
        {
            var ex = Volatile.Read(ref field);
            if (ex != TERMINATED)
            {
                ex = Interlocked.Exchange(ref field, TERMINATED);
            }
            return ex;
        }
    }
}
