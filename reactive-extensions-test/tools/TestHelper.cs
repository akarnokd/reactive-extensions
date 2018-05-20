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

        public static bool IsAssignableFrom<T>(this Type type, T item)
        {
            return type.GetTypeInfo().IsAssignableFrom(item.GetType().GetTypeInfo());
        }

        public static IObservable<T> ConcatError<T>(this IObservable<T> source, Exception ex)
        {
            return source.Concat(Observable.Throw<T>(ex));
        }

        /// <summary>
        /// Calls a transform function with an MaybeSubject,
        /// subscribes to the resulting IMaybeSource, disposes
        /// the connection and verifies if the MaybeSubject
        /// lost its observer, verifying the Dispose() call
        /// composes through.
        /// </summary>
        /// <typeparam name="T">The source value type.</typeparam>
        /// <typeparam name="R">The result value type.</typeparam>
        /// <param name="transform">The function to map a source into another source.</param>
        /// <param name="waitSeconds">How many seconds to wait at most till the dispose reaches the upstream.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void VerifyDisposeMaybe<T, R>(Func<IMaybeSource<T>, IMaybeSource<R>> transform, int waitSeconds = 1)
        {
            var ms = new MaybeSubject<T>();

            var source = transform(ms);

            var to = source.Test();

            Assert.True(ms.HasObserver());

            to.Dispose();

            for (int i = 0; i < waitSeconds * 10; i++)
            {
                if (ms.HasObserver())
                {
                    Thread.Sleep(100);
                }
                else
                {
                    return;
                }
            }

            Assert.False(ms.HasObserver());
        }

        /// <summary>
        /// Calls a transform function with an MaybeSubject,
        /// subscribes to the resulting IMaybeSource, disposes
        /// the connection and verifies if the MaybeSubject
        /// lost its observer, verifying the Dispose() call
        /// composes through.
        /// </summary>
        /// <typeparam name="T">The source value type.</typeparam>
        /// <typeparam name="R">The result value type.</typeparam>
        /// <param name="transform">The function to map a source into another source.</param>
        /// <param name="waitSeconds">How many seconds to wait at most till the dispose reaches the upstream.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void VerifyDisposeMaybe<T, R>(Func<IMaybeSource<T>, ISingleSource<R>> transform, int waitSeconds = 1)
        {
            var ms = new MaybeSubject<T>();

            var source = transform(ms);

            var to = source.Test();

            Assert.True(ms.HasObserver());

            to.Dispose();

            for (int i = 0; i < waitSeconds * 10; i++)
            {
                if (ms.HasObserver())
                {
                    Thread.Sleep(100);
                }
                else
                {
                    return;
                }
            }

            Assert.False(ms.HasObserver());
        }

        /// <summary>
        /// Calls a transform function with an MaybeSubject,
        /// subscribes to the resulting IMaybeSource, disposes
        /// the connection and verifies if the MaybeSubject
        /// lost its observer, verifying the Dispose() call
        /// composes through.
        /// </summary>
        /// <typeparam name="T">The source value type.</typeparam>
        /// <param name="transform">The function to map a source into another source.</param>
        /// <param name="waitSeconds">How many seconds to wait at most till the dispose reaches the upstream.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void VerifyDisposeMaybe<T>(Func<IMaybeSource<T>, ICompletableSource> transform, int waitSeconds = 1)
        {
            var ms = new MaybeSubject<T>();

            var source = transform(ms);

            var to = source.Test();

            Assert.True(ms.HasObserver());

            to.Dispose();

            for (int i = 0; i < waitSeconds * 10; i++)
            {
                if (ms.HasObserver())
                {
                    Thread.Sleep(100);
                }
                else
                {
                    return;
                }
            }

            Assert.False(ms.HasObserver());
        }

        /// <summary>
        /// Calls a transform function with an SingleSubject,
        /// subscribes to the resulting IMaybeSource, disposes
        /// the connection and verifies if the SingleSubject
        /// lost its observer, verifying the Dispose() call
        /// composes through.
        /// </summary>
        /// <typeparam name="T">The source value type.</typeparam>
        /// <typeparam name="R">The result value type.</typeparam>
        /// <param name="transform">The function to map a source into another source.</param>
        /// <param name="waitSeconds">How many seconds to wait at most till the dispose reaches the upstream.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void VerifyDisposeSingle<T, R>(Func<ISingleSource<T>, IMaybeSource<R>> transform, int waitSeconds = 1)
        {
            var ms = new SingleSubject<T>();

            var source = transform(ms);

            var to = source.Test();

            Assert.True(ms.HasObserver());

            to.Dispose();

            for (int i = 0; i < waitSeconds * 10; i++)
            {
                if (ms.HasObserver())
                {
                    Thread.Sleep(100);
                }
                else
                {
                    return;
                }
            }

            Assert.False(ms.HasObserver());
        }
    }
}
