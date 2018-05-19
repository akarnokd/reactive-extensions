using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableFlatMapTest
    {
        #region + ISingleSource +

        [Test]
        public void Single_Just()
        {
            SingleSource.Just(1)
                .FlatMap(v => CompletableSource.Empty())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Single_Basic()
        {
            var count = 0;

            SingleSource.Just(1)
                .FlatMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Single_Main_Error()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .FlatMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Single_Inner_Error()
        {
            SingleSource.Just(1)
                .FlatMap(v => CompletableSource.Error(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Single_Dispose_Main()
        {
            var ss = new SingleSubject<int>();
            var count = 0;

            var to = ss
                .FlatMap(v => CompletableSource.FromAction(() => count++))
                .Test();

            to.AssertSubscribed();

            Assert.True(ss.HasObserver());

            to.Dispose();

            Assert.False(ss.HasObserver());
        }

        [Test]
        public void Single_Dispose_Inner()
        {
            var ss = new SingleSubject<int>();
            var cs = new CompletableSubject();

            var to = ss
                .FlatMap(v => cs)
                .Test();

            to.AssertSubscribed();

            Assert.True(ss.HasObserver());
            Assert.False(cs.HasObserver());

            ss.OnSuccess(1);

            Assert.True(cs.HasObserver());
            Assert.False(ss.HasObserver());

            to.AssertEmpty();

            cs.OnCompleted();

            to.AssertResult();
        }

        #endregion +ISingleSource +

        #region + IMaybeSource +

        [Test]
        public void Maybe_Just()
        {
            MaybeSource.Just(1)
                .FlatMap(v => CompletableSource.Empty())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Maybe_Empty()
        {
            var count = 0;

            MaybeSource.Empty<int>()
                .FlatMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Maybe_Basic()
        {
            var count = 0;

            MaybeSource.Just(1)
                .FlatMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Maybe_Main_Error()
        {
            var count = 0;

            MaybeSource.Error<int>(new InvalidOperationException())
                .FlatMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Maybe_Inner_Error()
        {
            MaybeSource.Just(1)
                .FlatMap(v => CompletableSource.Error(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Maybe_Dispose_Main()
        {
            var ss = new MaybeSubject<int>();
            var count = 0;

            var to = ss
                .FlatMap(v => CompletableSource.FromAction(() => count++))
                .Test();

            to.AssertSubscribed();

            Assert.True(ss.HasObserver());

            to.Dispose();

            Assert.False(ss.HasObserver());
        }

        [Test]
        public void Maybe_Dispose_Inner()
        {
            var ss = new MaybeSubject<int>();
            var cs = new CompletableSubject();

            var to = ss
                .FlatMap(v => cs)
                .Test();

            to.AssertSubscribed();

            Assert.True(ss.HasObserver());
            Assert.False(cs.HasObserver());

            ss.OnSuccess(1);

            Assert.True(cs.HasObserver());
            Assert.False(ss.HasObserver());

            to.AssertEmpty();

            cs.OnCompleted();

            to.AssertResult();
        }

        #endregion + IMaybeSource +
    }
}
