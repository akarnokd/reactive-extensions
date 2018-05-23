using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Disposables;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class SingleAssignmentDisposableTest
    {
        [Test]
        public void Basic()
        {
            var sad = new akarnokd.reactive_extensions.SingleAssignmentDisposable();

            Assert.IsNull(sad.Disposable);

            var count = 0;

            sad.Disposable = Disposable.Create(() => count++);

            Assert.IsNotNull(sad.Disposable);

            sad.Dispose();

            Assert.IsNotNull(sad.Disposable);

            Assert.AreEqual(DisposableHelper.EMPTY, sad.Disposable);
        }

        [Test]
        public void Multi_Assign()
        {
            var sad = new akarnokd.reactive_extensions.SingleAssignmentDisposable();

            sad.Disposable = DisposableHelper.EMPTY;

            var count = 0;

            try
            {
                sad.Disposable = Disposable.Create(() => count++);

                Assert.Fail();
            }
            catch (InvalidOperationException)
            {

            }

            Assert.AreEqual(1, count);
        }
    }
}
