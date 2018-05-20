using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Threading;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeBlockingSubscribeTest
    {
        #region + IMaybeObserver +

        [Test]
        public void Observer_Success()
        {
            var to = new TestObserver<int>();

            MaybeSource.Just(1)
                .BlockingSubscribe(to);

            to.AssertResult(1);
        }

        [Test]
        public void Observer_Empty()
        {
            var to = new TestObserver<int>();

            MaybeSource.Empty<int>()
                .BlockingSubscribe(to);

            to.AssertResult();
        }

        [Test]
        public void Observer_Error()
        {
            var to = new TestObserver<int>();

            MaybeSource.Error<int>(new InvalidOperationException())
                .BlockingSubscribe(to);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        [Timeout(5000)]
        public void Observer_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new MaybeSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    to.Dispose();
                    cdl.Signal();
                });

                cs.BlockingSubscribe(to);

                cdl.Wait();

                Assert.False(cs.HasObserver());
            }
        }

        [Test]
        [Timeout(5000)]
        public void Observer_Complete_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new MaybeSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnCompleted();
                });

                cs.BlockingSubscribe(to);

                to.AssertResult();
            }
        }

        [Test]
        [Timeout(5000)]
        public void Observer_Error_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new MaybeSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnError(new InvalidOperationException());
                });

                cs.BlockingSubscribe(to);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        #endregion + IMaybeObserver +

        #region + Action +

        [Test]
        public void Action_Empty_Ignored()
        {
            var count = 0;

            MaybeSource.FromAction<int>(() => count++)
                .BlockingSubscribe();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Action_Just_Ignored()
        {
            var count = 0;

            MaybeSource.FromFunc(() => ++count)
                .BlockingSubscribe();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Action_Block_Error_Ignored()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .BlockingSubscribe();
        }

        [Test]
        public void Action_Empty()
        {
            var to = new TestObserver<int>();

            MaybeSource.Empty<int>()
                .BlockingSubscribe(to.OnSuccess, to.OnError, to.OnCompleted);

            to.AssertResult();
        }

        [Test]
        public void Action_Error()
        {
            var to = new TestObserver<int>();

            MaybeSource.Error<int>(new InvalidOperationException())
                .BlockingSubscribe(to.OnSuccess, to.OnError, to.OnCompleted);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        [Timeout(5000)]
        public void Action_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new MaybeSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    to.Dispose();
                    cdl.Signal();
                });

                cs.BlockingSubscribe(to.OnSuccess, to.OnError, to.OnCompleted, to.OnSubscribe);

                cdl.Wait();

                Assert.False(cs.HasObserver());
            }
        }

        [Test]
        [Timeout(5000)]
        public void Action_Complete_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new MaybeSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnCompleted();
                });

                cs.BlockingSubscribe(to.OnSuccess, to.OnError, to.OnCompleted);

                to.AssertResult();
            }
        }

        [Test]
        [Timeout(5000)]
        public void Action_Error_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new MaybeSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnError(new InvalidOperationException());
                });

                cs.BlockingSubscribe(to.OnSuccess, to.OnError, to.OnCompleted);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        #endregion + Action +
    }
}
