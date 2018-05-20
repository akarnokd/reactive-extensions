using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeElementAtTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                .FirstElement()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void One_Element()
        {
            Observable.Return(1)
                .FirstElement()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void At_Index()
        {
            for (int i = 0; i < 5; i++)
            {
                Observable.Range(1, 5)
                    .ElementAtIndex(i)
                    .Test()
                    .AssertResult(i + 1);
            }
        }

        [Test]
        public void Empty()
        {
            Observable.Empty<int>()
                .FirstElement()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Shorter()
        {
            Observable.Range(1, 5)
                .ElementAtIndex(5)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .FirstElement()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Comes_Next()
        {
            Observable.Return(1).ConcatError(new InvalidOperationException())
                .FirstElement()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservable<int, int>(o => o.FirstElement());
        }
    }
}
