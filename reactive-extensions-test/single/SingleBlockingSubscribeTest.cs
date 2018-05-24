using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Threading;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleBlockingSubscribeTest
    {
        #region + ISingleObserver +

        [Test]
        public void Observer_Success()
        {
            var to = new TestObserver<int>();

            SingleSource.Just(1)
                .BlockingSubscribe(to);

            to.AssertResult(1);
        }

        [Test]
        public void Observer_Error()
        {
            var to = new TestObserver<int>();

            SingleSource.Error<int>(new InvalidOperationException())
                .BlockingSubscribe(to);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
#if NETFRAMEWORK
        [Timeout(10000)]
#endif
        public void Observer_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new SingleSubject<int>();

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
#if NETFRAMEWORK
        [Timeout(10000)]
#endif
        public void Observer_Success_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new SingleSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnSuccess(1);
                });

                cs.BlockingSubscribe(to);

                to.AssertResult(1);
            }
        }

        [Test]
#if NETFRAMEWORK
        [Timeout(10000)]
#endif
        public void Observer_Error_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new SingleSubject<int>();

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

        #endregion + ISingleObserver +

        #region + Action +

        [Test]
        public void Action_Just_Ignored()
        {
            var count = 0;

            SingleSource.FromFunc(() => ++count)
                .BlockingSubscribe();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Action_Block_Error_Ignored()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .BlockingSubscribe();
        }

        [Test]
        public void Action_Error()
        {
            var to = new TestObserver<int>();

            SingleSource.Error<int>(new InvalidOperationException())
                .BlockingSubscribe(to.OnSuccess, to.OnError);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
#if NETFRAMEWORK
        [Timeout(10000)]
#endif
        public void Action_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new SingleSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    to.Dispose();
                    cdl.Signal();
                });

                cs.BlockingSubscribe(to.OnSuccess, to.OnError, to.OnSubscribe);

                cdl.Wait();

                Assert.False(cs.HasObserver());
            }
        }

        [Test]
#if NETFRAMEWORK
        [Timeout(10000)]
#endif
        public void Action_Success_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new SingleSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnSuccess(1);
                });

                cs.BlockingSubscribe(to.OnSuccess, to.OnError);

                to.AssertResult(1);
            }
        }

        [Test]
#if NETFRAMEWORK
        [Timeout(10000)]
#endif
        public void Action_Error_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new SingleSubject<int>();

                var to = new TestObserver<int>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnError(new InvalidOperationException());
                });

                cs.BlockingSubscribe(to.OnSuccess, to.OnError);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        #endregion + Action +
    }
}
