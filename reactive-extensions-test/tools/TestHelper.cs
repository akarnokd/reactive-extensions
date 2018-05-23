using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;

namespace akarnokd.reactive_extensions_test
{
    /// <summary>
    /// Test helper methods.
    /// </summary>
    internal static partial class TestHelper
    {
        /// <summary>
        /// Number of iterations to perform in concurrency-race tests by default.
        /// </summary>
        internal static readonly int RACE_LOOPS = 200;

        /// <summary>
        /// Runs two actions concurrently, synchronizing their
        /// execution as much as possible and waiting
        /// for them to finish. Exceptions from
        /// either of them are re-thrown.
        /// </summary>
        /// <param name="a1">The first action to run (on the current thread).</param>
        /// <param name="a2">The second action to run (on a background thread).</param>
        /// <remarks>The race times out after 5 seconds.</remarks>
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

        public static bool IsAssignableFrom<T>(this Type type, T item)
        {
            return type.GetTypeInfo().IsAssignableFrom(item.GetType().GetTypeInfo());
        }

        public static IObservable<T> ConcatError<T>(this IObservable<T> source, Exception ex)
        {
            return source.Concat(Observable.Throw<T>(ex));
        }

    }
}
