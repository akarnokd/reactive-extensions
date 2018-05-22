using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleRetryTest
    {
        #region + Times +

        [Test]
        public void Times_Success()
        {
            SingleSource.Just(1)
                .Retry()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Times_Error()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                return count;
            })
            .Retry()
            .Test()
            .AssertResult(5);
        }

        [Test]
        public void Times_Error_Limit()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                return count;
            })
            .Retry(5)
            .Test()
            .AssertResult(5);
        }

        [Test]
        public void Times_Error_Limit_Fail()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                return count;
            })
            .Retry(3)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Times_Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.Retry());
        }

        #endregion + Times +

        #region + Handler +

        [Test]
        public void Handler_Success()
        {
            SingleSource.Just(1)
                .Retry((e, i) => true)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Handler_Error()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                return count;
            })
            .Retry((e, i) => true)
            .Test()
            .AssertResult(5);
        }

        [Test]
        public void Handler_Error_Limit()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                return count;
            })
            .Retry((e, i) => i < 5)
            .Test()
            .AssertResult(5);
        }

        [Test]
        public void Handler_Error_Limit_Fail()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                return count;
            })
            .Retry((e, i) => i < 3)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Handler_Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.Retry((e, i) => true));
        }

        [Test]
        public void Handler_Wrong_Error()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            })
            .Retry((e, i) => e is InvalidOperationException)
            .Test()
            .AssertFailure(typeof(ArgumentOutOfRangeException));
        }

        [Test]
        public void Handler_Crash()
        {
            SingleSource.Error<int>(new InvalidOperationException("main"))
                .Retry((e, i) => throw new InvalidOperationException("inner"))
                .Test()
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeError(0, typeof(InvalidOperationException), "main")
                .AssertCompositeError(1, typeof(InvalidOperationException), "inner")
                .AssertCompositeErrorCount(2)
                ;
        }

        #endregion + Handler +
    }
}
