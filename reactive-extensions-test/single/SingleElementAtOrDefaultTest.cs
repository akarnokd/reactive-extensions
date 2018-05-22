using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleElementAtOrDefaultTest
    {
        #region + FirstOrDefault +

        [Test]
        public void First()
        {
            Observable.Range(1, 5)
                .FirstOrDefault(-100)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void First_Empty()
        {
            Observable.Empty<int>()
                .FirstOrDefault(-100)
                .Test()
                .AssertResult(-100);
        }

        [Test]
        public void First_Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .FirstOrDefault(-100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + FirstOrDefault +

        #region + ElementAtOrDefault +

        [Test]
        public void Index_Middle()
        {
            Observable.Range(1, 5)
                .ElementAtOrDefault(2, -100)
                .Test()
                .AssertResult(3);
        }

        [Test]
        public void Index_Bigger()
        {
            Observable.Range(1, 5)
                .ElementAtOrDefault(6, -100)
                .Test()
                .AssertResult(-100);
        }


        [Test]
        public void Index_Empty()
        {
            Observable.Empty<int>()
                .ElementAtOrDefault(2, -100)
                .Test()
                .AssertResult(-100);
        }

        [Test]
        public void Index_Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .ElementAtOrDefault(2, -100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + ElementAtOrDefault +
    }
}
