using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Helper methods to call the OnXXX methods in a serialized
    /// fashion where OnNext is guaranteed to be not called
    /// concurrently with itself but the other OnXXX may be.
    /// </summary>
    /// <remarks>Since 0.0.13</remarks>
    internal static class HalfSerializer
    {

        public static void OnNext<T>(IObserver<T> observer, T item, ref int wip, ref Exception error)
        {
            if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
            {
                observer.OnNext(item);
                if (Interlocked.Decrement(ref wip) != 0)
                {
                    var ex = Volatile.Read(ref error);
                    if (ex != ExceptionHelper.TERMINATED)
                    {
                        observer.OnError(ex);
                    }
                    else
                    {
                        observer.OnCompleted();
                    }
                }
            }
        }

        public static void OnError<T>(IObserver<T> observer, Exception ex, ref int wip, ref Exception error)
        {
            if (Interlocked.CompareExchange(ref error, ex, null) == null)
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    observer.OnError(ex);
                }
            }
        }

        public static void OnCompleted<T>(IObserver<T> observer, ref int wip, ref Exception error)
        {
            if (Interlocked.CompareExchange(ref error, ExceptionHelper.TERMINATED, null) == null)
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    observer.OnCompleted();
                }
            }
        }

        public static void OnNext<T>(ISignalObserver<T> observer, T item, ref int wip, ref Exception error)
        {
            if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
            {
                observer.OnNext(item);
                if (Interlocked.Decrement(ref wip) != 0)
                {
                    var ex = Volatile.Read(ref error);
                    if (ex != ExceptionHelper.TERMINATED)
                    {
                        observer.OnError(ex);
                    }
                    else
                    {
                        observer.OnCompleted();
                    }
                }
            }
        }

        public static void OnError<T>(ISignalObserver<T> observer, Exception ex, ref int wip, ref Exception error)
        {
            if (Interlocked.CompareExchange(ref error, ex, null) == null)
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    observer.OnError(ex);
                }
            }
        }

        public static void OnCompleted<T>(ISignalObserver<T> observer, ref int wip, ref Exception error)
        {
            if (Interlocked.CompareExchange(ref error, ExceptionHelper.TERMINATED, null) == null)
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    observer.OnCompleted();
                }
            }
        }
    }
}
