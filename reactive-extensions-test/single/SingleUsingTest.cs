using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleUsingTest
    {
        [Test]
        public void Success_Eager()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using(() => 1,
                v => SingleSource.Just(1),
                v => cleanup = run
            )
            .DoOnSuccess(v => run = 1)
            .Test()
                .AssertResult(1);

            Assert.AreEqual(0, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Success_Non_Eager()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using(() => 1,
                v => SingleSource.Just(1),
                v => cleanup = run,
                false
            )
            .DoOnSuccess(v => run = 1)
            .Test()
                .AssertResult(1);

            Assert.AreEqual(1, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Error_Eager()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using(() => 1,
                v => SingleSource.Error<int>(new InvalidOperationException()),
                v => cleanup = run
            )
            .DoOnError(e => run = 1)
            .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Error_Non_Eager()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using(() => 1,
                v => SingleSource.Error<int>(new InvalidOperationException()),
                v => cleanup = run,
                false
            )
            .DoOnError(e => run = 1)
            .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Dispose()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using(() => 1,
                v => SingleSource.Never<int>(),
                v => cleanup = run
            )
            .DoOnSuccess(v => run = 1)
            .Test()
                .Dispose();

            Assert.AreEqual(0, cleanup);
            Assert.AreEqual(0, run);
        }

        [Test]
        public void Resource_Supplier_Crash()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using<int, int>(() => { throw new InvalidOperationException();  },
                v => { run = 1; return SingleSource.Never<int>(); },
                v => cleanup = run
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(-1, cleanup);
            Assert.AreEqual(0, run);
        }

        [Test]
        public void Source_Selector_Crash_Eager()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using<int, int>(() => 1,
                v => { throw new InvalidOperationException(); },
                v => cleanup = run
            )
            .DoOnError(e => run = 1)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Source_Selector_Crash_Non_Eager()
        {
            var cleanup = -1;
            var run = 0;

            SingleSource.Using<int, int>(() => 1,
                v => { throw new InvalidOperationException(); },
                v => cleanup = run,
                false
            )
            .DoOnError(e => run = 1)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Success_Cleanup_Crash_Eager()
        {
            SingleSource.Using<int, int>(() => 1,
                v => SingleSource.Just(1),
                v => { throw new InvalidOperationException(); }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Success_Cleanup_Crash_Non_Eager()
        {
            SingleSource.Using<int, int>(() => 1,
                v => SingleSource.Just(1),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .AssertResult(1);
        }

        [Test]
        public void Error_Cleanup_Crash_Eager()
        {
            SingleSource.Using(() => 1,
                v => SingleSource.Error<int>(new NotImplementedException()),
                v => { throw new InvalidOperationException(); }
            )
            .Test()
            .AssertFailure(typeof(AggregateException))
            .AssertCompositeError(0, typeof(NotImplementedException))
            .AssertCompositeError(1, typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Cleanup_Crash_Non_Eager()
        {
            SingleSource.Using(() => 1,
                v => SingleSource.Error<int>(new NotImplementedException()),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .AssertFailure(typeof(NotImplementedException));
        }

        [Test]
        public void Dispose_Cleanup_Crash()
        {
            SingleSource.Using(() => 1,
                v => SingleSource.Never<int>(),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .Cancel()
            .AssertEmpty();
        }
    }
}
