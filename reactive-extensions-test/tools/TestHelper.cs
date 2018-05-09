using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test
{
    /// <summary>
    /// Test helper methods.
    /// </summary>
    internal static class TestHelper
    {
        internal static readonly int RACE_LOOPS = 1000;

        internal static void Race(Action a1, Action a2)
        {
            var sync = new int[] { 2 };
            var error = new Exception[1];
            var latch = new CountdownEvent(1);

            Task.Factory.StartNew(() =>
            {
                // fine-grained thread synchronization
                if (Interlocked.Decrement(ref sync[0]) != 0)
                {
                    while (Volatile.Read(ref sync[0]) != 0) ;
                }

                try
                {
                    a2();
                }
                catch (Exception ex)
                {
                    Volatile.Write(ref error[0], ex);
                }
                latch.Signal();
            });

            // fine-grained thread synchronization
            if (Interlocked.Decrement(ref sync[0]) != 0)
            {
                while (Volatile.Read(ref sync[0]) != 0) ;
            }

            Exception ex1 = null;

            try
            {
                a1();
            }
            catch (Exception ex)
            {
                ex1 = ex;
            }

            if (!latch.Wait(5000))
            {
                ex1 = new Exception("Action a2 timed out after 5000ms");
            }

            Exception ex2 = Volatile.Read(ref error[0]);

            if (ex1 != null && ex2 != null)
            {
                throw new AggregateException(ex1, ex2);
            }

            if (ex1 != null && ex2 == null)
            {
                throw ex1;
            }

            if (ex1 == null && ex2 != null)
            {
                throw ex2;
            }

            // otherwise success
        }

        internal static void Emit<T>(this IObserver<T> subject, params T[] items)
        {
            foreach (var t in items)
            {
                subject.OnNext(t);
            }
        }

        internal static void EmitAll<T>(this IObserver<T> subject, params T[] items)
        {
            Emit(subject, items);
            subject.OnCompleted();
        }

        public static void EmitError<T>(this IObserver<T> subject, Exception error, params T[] items)
        {
            Emit(subject, items);
            subject.OnError(error);
        }

    }
}
