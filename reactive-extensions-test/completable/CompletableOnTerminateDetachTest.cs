using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableOnTerminateDetachTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .OnTerminateDetach()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .OnTerminateDetach()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var cs = new CompletableSubject();

            var to = cs
                .OnTerminateDetach()
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        ICompletableEmitter subj;
        TestObserver<object> tso;

        [Test]
        //[Ignore("CI doesn't like this")]
        public void No_Leak_Consumer()
        {
            var src = CompletableSource.Create(emitter => subj = emitter);
            tso = new TestObserver<object>();

            src.OnTerminateDetach().Subscribe(tso);

            var wt = new WeakReference(tso);

            subj.OnCompleted();

            tso = null;

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
            var src = CompletableSource.Create(emitter => subj = emitter);
            tso = new TestObserver<object>();

            src.OnTerminateDetach().Subscribe(tso);

            var wt = new WeakReference(tso);

            tso.Dispose();

            tso = null;

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
            var src = CompletableSource.Create(emitter => subj = emitter);
            tso = new TestObserver<object>();

            src.OnTerminateDetach().Subscribe(tso);

            var wt = new WeakReference(tso);

            subj.OnCompleted();

            tso = null;

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
            var src = CompletableSource.Create(emitter => subj = emitter);
            tso = new TestObserver<object>();

            src.OnTerminateDetach().Subscribe(tso);

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
            var src = CompletableSource.Create(emitter => subj = emitter);
            tso = new TestObserver<object>();


            src.OnTerminateDetach().Subscribe(tso);

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
            var src = CompletableSource.Create(emitter => subj = emitter);
            tso = new TestObserver<object>();

            src.OnTerminateDetach().Subscribe(tso);

            var wt = new WeakReference(subj);

            tso.Dispose();

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
    }
}
