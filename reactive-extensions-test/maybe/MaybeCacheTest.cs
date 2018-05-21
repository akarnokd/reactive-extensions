using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeCacheTest
    {
        [Test]
        public void Success()
        {
            var source = MaybeSource.Just(1).Cache();

            source.Test().AssertResult(1);

            source.Test().AssertResult(1);

            source.Test(true).AssertEmpty();
        }

        [Test]
        public void Empty()
        {
            var source = MaybeSource.Empty<int>().Cache();

            source.Test().AssertResult();

            source.Test().AssertResult();

            source.Test(true).AssertEmpty();
        }


        [Test]
        public void Error()
        {
            var source = MaybeSource.Error<int>(new InvalidOperationException())
                .Cache();

            source.Test().AssertFailure(typeof(InvalidOperationException));

            source.Test().AssertFailure(typeof(InvalidOperationException));

            source.Test(true).AssertEmpty();
        }

        [Test]
        public void Dispose()
        {
            var ms = new MaybeSubject<int>();
            var cancel = new IDisposable[1];

            var source = ms.Cache(d => cancel[0] = d);

            Assert.Null(cancel[0], "cancel set?");
            Assert.False(ms.HasObserver(), "has observers?");

            var to = source.Test();

            Assert.NotNull(cancel[0], "cancel not set?");
            Assert.True(ms.HasObserver(), "no observers?");

            cancel[0].Dispose();
            cancel[0].Dispose();

            to.AssertFailure(typeof(OperationCanceledException));

            source.Test().AssertFailure(typeof(OperationCanceledException));

            Assert.False(ms.HasObserver(), "still observers?");
        }

        [Test]
        public void Dispose_Run_Cancel()
        {
            var ms = new MaybeSubject<int>();
            var cancel = new IDisposable[1];

            var source = ms.Cache(d => cancel[0] = d);

            Assert.Null(cancel[0], "cancel set?");
            Assert.False(ms.HasObserver(), "has observers?");

            var to = source.Test(true);

            Assert.NotNull(cancel[0], "cancel not set?");
            Assert.True(ms.HasObserver(), "no observers?");

            cancel[0].Dispose();
            cancel[0].Dispose();

            to.AssertEmpty();

            source.Test().AssertFailure(typeof(OperationCanceledException));

            Assert.False(ms.HasObserver(), "still observers?");
        }

        [Test]
        public void Multiple()
        {
            var ms = new MaybeSubject<int>();

            var source = ms.Cache();

            var to1 = source.Test();
            var to2 = source.Test();
            var to3 = source.Test(true);

            ms.OnSuccess(1);

            to1.AssertResult(1);
            to2.AssertResult(1);
            to3.AssertEmpty();
        }
    }
}
