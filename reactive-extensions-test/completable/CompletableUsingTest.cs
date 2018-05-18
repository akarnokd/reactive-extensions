using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableUsingTest
    {
        [Test]
        public void Basic_Eager()
        {
            var cleanup = -1;
            var run = 0;

            CompletableSource.Using(() => 1,
                v => CompletableSource.Empty(),
                v => cleanup = run
            )
            .DoOnCompleted(() => run = 1)
            .Test()
                .AssertResult();

            Assert.AreEqual(0, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Basic_Non_Eager()
        {
            var cleanup = -1;
            var run = 0;

            CompletableSource.Using(() => 1,
                v => CompletableSource.Empty(),
                v => cleanup = run,
                false
            )
            .DoOnCompleted(() => run = 1)
            .Test()
                .AssertResult();

            Assert.AreEqual(1, cleanup);
            Assert.AreEqual(1, run);
        }

        [Test]
        public void Error_Eager()
        {
            var cleanup = -1;
            var run = 0;

            CompletableSource.Using(() => 1,
                v => CompletableSource.Error(new InvalidOperationException()),
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

            CompletableSource.Using(() => 1,
                v => CompletableSource.Error(new InvalidOperationException()),
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

            CompletableSource.Using(() => 1,
                v => CompletableSource.Never(),
                v => cleanup = run
            )
            .DoOnCompleted(() => run = 1)
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

            CompletableSource.Using<int>(() => { throw new InvalidOperationException();  },
                v => { run = 1; return CompletableSource.Never(); },
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

            CompletableSource.Using(() => 1,
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

            CompletableSource.Using(() => 1,
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
        public void Completed_Cleanup_Crash_Eager()
        {
            CompletableSource.Using(() => 1,
                v => CompletableSource.Empty(),
                v => { throw new InvalidOperationException(); }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Completed_Cleanup_Crash_Non_Eager()
        {
            CompletableSource.Using(() => 1,
                v => CompletableSource.Empty(),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .AssertResult();
        }

        [Test]
        public void Error_Cleanup_Crash_Eager()
        {
            CompletableSource.Using(() => 1,
                v => CompletableSource.Error(new NotImplementedException()),
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
            CompletableSource.Using(() => 1,
                v => CompletableSource.Error(new NotImplementedException()),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .AssertFailure(typeof(NotImplementedException));
        }

        [Test]
        public void Dispose_Cleanup_Crash()
        {
            CompletableSource.Using(() => 1,
                v => CompletableSource.Never(),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .Cancel()
            .AssertEmpty();
        }
    }
}
