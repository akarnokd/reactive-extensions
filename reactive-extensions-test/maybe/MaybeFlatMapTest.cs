using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeFlatMapTest
    {
        #region + IMaybeSource +

        [Test]
        public void Maybe_Basic()
        {
            MaybeSource.Just(1)
                .FlatMap(v => MaybeSource.Just("" + (v + 1)))
                .Test()
                .AssertResult("2");
        }

        [Test]
        public void Maybe_Empty()
        {
            var count = 0;

            MaybeSource.Empty<int>()
                .FlatMap(v => MaybeSource.FromFunc(() => {
                    count++;
                    return v + 1;
                }))
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Maybe_Inner_Empty()
        {
            var count = 0;

            MaybeSource.Just(1)
                .FlatMap(v => MaybeSource.FromAction<string>(() => {
                    count++;
                }))
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Maybe_Error()
        {
            var count = 0;

            MaybeSource.Error<int>(new InvalidOperationException())
                .FlatMap(v => MaybeSource.FromAction<string>(() => {
                    count++;
                }))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Maybe_Error_Inner()
        {
            MaybeSource.Just(1)
                .FlatMap(v => MaybeSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Maybe_Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.FlatMap(v => MaybeSource.Just(2)));
        }

        [Test]
        public void Maybe_Dispose_Inner()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => MaybeSource.Just(1).FlatMap(v => m));
        }

        [Test]
        public void Maybe_Mapper_Crash()
        {
            Func<int, IMaybeSource<int>> f = v =>
            {
                throw new InvalidOperationException();
            };

            MaybeSource.Just(1)
                .FlatMap(f)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + IMaybeSource +

        #region + ISingleSource +

        [Test]
        public void Single_Basic()
        {
            MaybeSource.Just(1)
                .FlatMap(v => SingleSource.Just("" + (v + 1)))
                .Test()
                .AssertResult("2");
        }

        [Test]
        public void Single_Empty()
        {
            var count = 0;

            MaybeSource.Empty<int>()
                .FlatMap(v => SingleSource.FromFunc(() => {
                    count++;
                    return v + 1;
                }))
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Single_Error()
        {
            var count = 0;

            MaybeSource.Error<int>(new InvalidOperationException())
                .FlatMap(v => SingleSource.FromFunc<string>(() => {
                    return "" + (++count);
                }))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Single_Error_Inner()
        {
            MaybeSource.Just(1)
                .FlatMap(v => SingleSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Single_Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.FlatMap(v => SingleSource.Just(2)));
        }

        [Test]
        public void Single_Dispose_Inner()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => MaybeSource.Just(1).FlatMap(v => m));
        }

        [Test]
        public void Single_Mapper_Crash()
        {
            Func<int, ISingleSource<int>> f = v =>
            {
                throw new InvalidOperationException();
            };

            MaybeSource.Just(1)
                .FlatMap(f)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + ISingleSource +
    }
}
