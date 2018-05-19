using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Threading;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableBlockingSubscribeTest
    {
        #region + ICompletableObserver +

        [Test]
        public void Observer_Basic()
        {
            var to = new TestObserver<object>();

            CompletableSource.Empty()
                .BlockingSubscribe(to);

            to.AssertResult();
        }

        [Test]
        public void Observer_Error()
        {
            var to = new TestObserver<object>();

            CompletableSource.Error(new InvalidOperationException())
                .BlockingSubscribe(to);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        [Timeout(5000)]
        public void Observer_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new CompletableSubject();

                var to = new TestObserver<object>();

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
                var cs = new CompletableSubject();

                var to = new TestObserver<object>();

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
                var cs = new CompletableSubject();

                var to = new TestObserver<object>();

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

        #endregion + ICompletableObserver +

        #region + Action +

        [Test]
        public void Action_Block()
        {
            CompletableSource.Empty()
                .BlockingSubscribe();
        }

        [Test]
        public void Action_Block_Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .BlockingSubscribe();
        }

        [Test]
        public void Action_Basic()
        {
            var to = new TestObserver<object>();

            CompletableSource.Empty()
                .BlockingSubscribe(to.OnCompleted, to.OnError);

            to.AssertResult();
        }

        [Test]
        public void Action_Error()
        {
            var to = new TestObserver<object>();

            CompletableSource.Error(new InvalidOperationException())
                .BlockingSubscribe(to.OnCompleted, to.OnError);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        [Timeout(5000)]
        public void Action_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new CompletableSubject();

                var to = new TestObserver<object>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    to.Dispose();
                    cdl.Signal();
                });

                cs.BlockingSubscribe(to.OnCompleted, to.OnError, to.OnSubscribe);

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
                var cs = new CompletableSubject();

                var to = new TestObserver<object>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnCompleted();
                });

                cs.BlockingSubscribe(to.OnCompleted, to.OnError);

                to.AssertResult();
            }
        }

        [Test]
        [Timeout(5000)]
        public void Action_Error_Async()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new CompletableSubject();

                var to = new TestObserver<object>();

                var cdl = new CountdownEvent(1);

                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;
                    cs.OnError(new InvalidOperationException());
                });

                cs.BlockingSubscribe(to.OnCompleted, to.OnError);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        #endregion + Action +
    }
}
