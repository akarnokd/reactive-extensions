using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourcePublishTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var src = ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Publish();

            Assert.AreEqual(0, count);

            var to1 = src.Test();

            Assert.AreEqual(0, count);

            var to2 = src.Test();

            Assert.AreEqual(0, count);

            src.Connect();

            Assert.AreEqual(1, count);

            to1.WithTag("to1").AssertResult(1, 2, 3, 4, 5);
            to2.WithTag("to2").AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);

            src.Test().AssertResult();

            Assert.AreEqual(1, count);

            src.Reset();

            var to3 = src.Test().AssertEmpty();

            Assert.AreEqual(1, count);

            src.Connect();

            Assert.AreEqual(2, count);

            to3.AssertResult(1, 2, 3, 4, 5);

            src.Test().AssertResult();

            Assert.AreEqual(2, count);

            src.Connect();

            Assert.AreEqual(3, count);

            src.Test().AssertResult();
        }

        [Test]
        public void Error()
        {
            var count = 0;

            var src = ObservableSource.Error<int>(new InvalidOperationException())
                .DoOnSubscribe(s => count++)
                .Publish();

            Assert.AreEqual(0, count);

            var to1 = src.Test();

            Assert.AreEqual(0, count);

            var to2 = src.Test();

            Assert.AreEqual(0, count);

            src.Connect();

            Assert.AreEqual(1, count);

            to1.AssertFailure(typeof(InvalidOperationException));
            to2.AssertFailure(typeof(InvalidOperationException));

            src.Test().AssertFailure(typeof(InvalidOperationException));

            src.Reset();
            src.Reset();

            Assert.AreEqual(1, count);

            src.Connect();

            Assert.AreEqual(2, count);
            src.Test().AssertFailure(typeof(InvalidOperationException));
        }
    }
}
