using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeUsingTest
    {
        [Test]
        public void Basic_Eager()
        {
            var cleanup = -1;
            var run = 0;

            MaybeSource.Using(() => 1,
                v => MaybeSource.Empty<int>(),
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

            MaybeSource.Using(() => 1,
                v => MaybeSource.Empty<int>(),
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
        public void Success_Eager()
        {
            var cleanup = -1;
            var run = 0;

            MaybeSource.Using(() => 1,
                v => MaybeSource.Just(1),
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

            MaybeSource.Using(() => 1,
                v => MaybeSource.Just(1),
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

            MaybeSource.Using(() => 1,
                v => MaybeSource.Error<int>(new InvalidOperationException()),
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

            MaybeSource.Using(() => 1,
                v => MaybeSource.Error<int>(new InvalidOperationException()),
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

            MaybeSource.Using(() => 1,
                v => MaybeSource.Never<int>(),
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

            MaybeSource.Using<int, int>(() => { throw new InvalidOperationException();  },
                v => { run = 1; return MaybeSource.Never<int>(); },
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

            MaybeSource.Using<int, int>(() => 1,
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

            MaybeSource.Using<int, int>(() => 1,
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
            MaybeSource.Using<int, int>(() => 1,
                v => MaybeSource.Just(1),
                v => { throw new InvalidOperationException(); }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Success_Cleanup_Crash_Non_Eager()
        {
            MaybeSource.Using<int, int>(() => 1,
                v => MaybeSource.Just(1),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .AssertResult(1);
        }

        [Test]
        public void Completed_Cleanup_Crash_Eager()
        {
            MaybeSource.Using<int, int>(() => 1,
                v => MaybeSource.Empty<int>(),
                v => { throw new InvalidOperationException(); }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Completed_Cleanup_Crash_Non_Eager()
        {
            MaybeSource.Using<int, int>(() => 1,
                v => MaybeSource.Empty<int>(),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .AssertResult();
        }

        [Test]
        public void Error_Cleanup_Crash_Eager()
        {
            MaybeSource.Using(() => 1,
                v => MaybeSource.Error<int>(new NotImplementedException()),
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
            MaybeSource.Using(() => 1,
                v => MaybeSource.Error<int>(new NotImplementedException()),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .AssertFailure(typeof(NotImplementedException));
        }

        [Test]
        public void Dispose_Cleanup_Crash()
        {
            MaybeSource.Using(() => 1,
                v => MaybeSource.Never<int>(),
                v => { throw new InvalidOperationException(); },
                false
            )
            .Test()
            .Cancel()
            .AssertEmpty();
        }
    }
}
