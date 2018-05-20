using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeSwitchIfEmptyTest
    {
        #region + ISingleSource +

        [Test]
        public void Single_Success()
        {
            MaybeSource.Just(1)
                .SwitchIfEmpty(SingleSource.Just(2))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Single_Empty()
        {
            MaybeSource.Empty<int>()
                .SwitchIfEmpty(SingleSource.Just(2))
                .Test()
                .AssertResult(2);
        }

        [Test]
        public void Single_Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .SwitchIfEmpty(SingleSource.Just(2))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Single_Error_Fallback()
        {
            MaybeSource.Empty<int>()
                .SwitchIfEmpty(SingleSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Single_Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.SwitchIfEmpty(SingleSource.Just(2)));
        }

        [Test]
        public void Single_Dispose_Fallback()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => MaybeSource.Empty<int>().SwitchIfEmpty(m));
        }

        #endregion + ISingleSource +

        #region + IMaybeSources +

        [Test]
        public void Maybes_Success()
        {
            MaybeSource.Just(1)
                .SwitchIfEmpty(MaybeSource.Empty<int>(), MaybeSource.Just(2))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Maybes_Fallback()
        {
            MaybeSource.Empty<int>()
                .SwitchIfEmpty(MaybeSource.Empty<int>(), MaybeSource.Just(2))
                .Test()
                .AssertResult(2);
        }

        [Test]
        public void Maybes_No_Fallback()
        {
            MaybeSource.Empty<int>()
                .SwitchIfEmpty()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Maybes_Fallback_Empty()
        {
            MaybeSource.Empty<int>()
                .SwitchIfEmpty(MaybeSource.Empty<int>())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Maybes_Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .SwitchIfEmpty(MaybeSource.Just(2))
                .Test()
                .AssertError(typeof(InvalidOperationException));
        }

        [Test]
        public void Maybes_Fallback_Error()
        {
            MaybeSource.Empty<int>()
                .SwitchIfEmpty(MaybeSource.Error<int>(new InvalidOperationException()), MaybeSource.Just(2))
                .Test()
                .AssertError(typeof(InvalidOperationException));
        }

        [Test]
        public void Maybes_Fallback_Null()
        {
            MaybeSource.Empty<int>()
                .SwitchIfEmpty(MaybeSource.Empty<int>(), null, MaybeSource.Empty<int>())
                .Test()
                .AssertError(typeof(NullReferenceException));
        }

        [Test]
        public void Maybes_Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m =>
                m.SwitchIfEmpty(MaybeSource.Just(2))
                );
        }

        [Test]
        public void Maybes_Fallback_Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m =>
                MaybeSource.Empty<int>().SwitchIfEmpty(MaybeSource.Empty<int>(), m)
                );
        }

        #endregion + IMaybeSources +
    }
}
