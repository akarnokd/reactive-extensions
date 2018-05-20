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
    // Test helper methods to verify disposing a single source.
    internal static partial class TestHelper
    {
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

            Assert.True(ms.HasObserver(), "Not subscribed to the source subject!");

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

            Assert.False(ms.HasObserver(), "Still subscribed to the source subject!");
        }

        /// <summary>
        /// Calls a transform function with an SingleSubject,
        /// subscribes to the resulting ISingleSource, disposes
        /// the connection and verifies if the SingleSubject
        /// lost its observer, verifying the Dispose() call
        /// composes through.
        /// </summary>
        /// <typeparam name="T">The source value type.</typeparam>
        /// <typeparam name="R">The result value type.</typeparam>
        /// <param name="transform">The function to map a source into another source.</param>
        /// <param name="waitSeconds">How many seconds to wait at most till the dispose reaches the upstream.</param>
        /// <remarks>Since 0.0.11</remarks>
        public static void VerifyDisposeSingle<T, R>(Func<ISingleSource<T>, ISingleSource<R>> transform, int waitSeconds = 1)
        {
            var ms = new SingleSubject<T>();

            var source = transform(ms);

            var to = source.Test();

            Assert.True(ms.HasObserver(), "Not subscribed to the source subject!");

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

            Assert.False(ms.HasObserver(), "Still subscribed to the source subject!");
        }

    }
}
