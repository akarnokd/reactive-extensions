using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeOnTerminateDetachTest
    {
        [Test]
        public void Basic()
        {
            MaybeSource.Empty<int>()
                .OnTerminateDetach()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Success()
        {
            MaybeSource.Just(1)
                .OnTerminateDetach()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .OnTerminateDetach()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.OnTerminateDetach());
        }

        /*
         * These do not work on Travis-CI, perhaps different GC than locally?
         
        IMaybeEmitter subj;
        TestObserver<object> testObserver;

        [Test]
        public void No_Leak_Consumer()
        {
            var source = MaybeSource.Create(emitter => subj = emitter);
            testObserver = new TestObserver<object>();

            source.OnTerminateDetach().Subscribe(testObserver);

            var wt = new WeakReference(testObserver);

            subj.OnCompleted();

            testObserver = null;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);

                GC.Collect();

                Thread.Sleep(100);

                if (wt.Target == null)
                {
                    return;
                }
            }
            Assert.IsNull(wt.Target);
        }

        [Test]
        public void No_Leak_Dispose_Consumer()
        {
            var source = MaybeSource.Create(emitter => subj = emitter);
            testObserver = new TestObserver<object>();

            source.OnTerminateDetach().Subscribe(testObserver);

            var wt = new WeakReference(testObserver);

            testObserver.Dispose();

            testObserver = null;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);

                GC.Collect();

                Thread.Sleep(100);

                if (wt.Target == null)
                {
                    return;
                }
            }
            Assert.IsNull(wt.Target);
        }

        [Test]
        public void No_Leak_Consumer_Error()
        {
            var source = MaybeSource.Create(emitter => subj = emitter);
            testObserver = new TestObserver<object>();

            source.OnTerminateDetach().Subscribe(testObserver);

            var wt = new WeakReference(testObserver);

            subj.OnCompleted();

            testObserver = null;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);

                GC.Collect();

                Thread.Sleep(100);

                if (wt.Target == null)
                {
                    return;
                }
            }
            Assert.IsNull(wt.Target);
        }

        [Test]
        public void No_Leak_Producer()
        {
            var source = MaybeSource.Create(emitter => subj = emitter);
            testObserver = new TestObserver<object>();

            source.OnTerminateDetach().Subscribe(testObserver);

            var wt = new WeakReference(subj);

            subj.OnCompleted();

            subj = null;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);

                GC.Collect();

                Thread.Sleep(100);

                if (wt.Target == null)
                {
                    return;
                }
            }
            Assert.IsNull(wt.Target);
        }

        [Test]
        public void No_Leak_Producer_Error()
        {
            var source = MaybeSource.Create(emitter => subj = emitter);
            testObserver = new TestObserver<object>();


            source.OnTerminateDetach().Subscribe(testObserver);

            var wt = new WeakReference(subj);

            subj.OnError(new InvalidOperationException());

            subj = null;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);

                GC.Collect();

                Thread.Sleep(100);

                if (wt.Target == null)
                {
                    return;
                }
            }
            Assert.IsNull(wt.Target);
        }

        [Test]
        public void No_Leak_Dispose_Producer()
        {
            var source = MaybeSource.Create(emitter => subj = emitter);
            testObserver = new TestObserver<object>();

            source.OnTerminateDetach().Subscribe(testObserver);

            var wt = new WeakReference(subj);

            testObserver.Dispose();

            subj = null;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);

                GC.Collect();

                Thread.Sleep(100);

                if (wt.Target == null)
                {
                    return;
                }
            }
            Assert.IsNull(wt.Target);
        }
        */
    }
}
