using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class CompletableSubjectTest
    {
        [Test]
        public void Basic()
        {
            var cs = new CompletableSubject();

            Assert.False(cs.HasObservers);
            Assert.False(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.IsNull(cs.GetException());

            var to = cs.Test();

            Assert.True(cs.HasObserver());

            to.AssertEmpty();

            var bd = new BooleanDisposable();

            cs.OnSubscribe(bd);

            cs.OnCompleted();
            cs.OnError(new IndexOutOfRangeException());

            Assert.True(bd.IsDisposed());

            to.AssertResult();

            Assert.False(cs.HasObservers);
            Assert.True(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.IsNull(cs.GetException());

            cs.Test().AssertResult();

            Assert.False(cs.HasObserver());
            Assert.True(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.IsNull(cs.GetException());
        }

        [Test]
        public void Error()
        {
            var cs = new CompletableSubject();

            Assert.False(cs.HasObserver());
            Assert.False(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.IsNull(cs.GetException());

            var to = cs.Test();

            Assert.True(cs.HasObserver());

            to.AssertEmpty();

            var bd = new BooleanDisposable();

            cs.OnSubscribe(bd);
            cs.OnError(new InvalidOperationException());
            cs.OnError(new IndexOutOfRangeException());

            Assert.True(bd.IsDisposed());

            to.AssertFailure(typeof(InvalidOperationException)); 

            Assert.False(cs.HasObserver());
            Assert.False(cs.HasCompleted());
            Assert.True(cs.HasException());
            Assert.IsNotNull(cs.GetException());
            Assert.True(typeof(InvalidOperationException).IsAssignableFrom(cs.GetException()));

            cs.Test().AssertFailure(typeof(InvalidOperationException));

            Assert.False(cs.HasObserver());
            Assert.False(cs.HasCompleted());
            Assert.True(cs.HasException());
            Assert.IsNotNull(cs.GetException());
            Assert.True(typeof(InvalidOperationException).IsAssignableFrom(cs.GetException()));
        }

        [Test]
        public void Observer_Disposes_Upfront()
        {
            var cs = new CompletableSubject();

            var to = cs.Test();

            var to1 = cs.Test(true);

            cs.OnCompleted();

            to.AssertResult();

            to1.AssertEmpty();
        }

        [Test]
        public void Race_Subscribe_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new CompletableSubject();

                var to1 = cs.Test();

                var to2 = new TestObserver<object>();

                TestHelper.Race(() => {
                    to1.Dispose();
                }, () => {
                    cs.Subscribe(to2);
                });

                cs.OnCompleted();

                to1.AssertEmpty();

                to2.AssertResult();
            }
        }

        [Test]
        public void ReferenceCount()
        {
            var cs = new CompletableSubject(true);

            var bd = new BooleanDisposable();

            cs.OnSubscribe(bd);

            Assert.False(bd.IsDisposed());

            var to1 = cs.Test();

            Assert.False(bd.IsDisposed());

            var to2 = cs.Test();

            to1.Dispose();

            Assert.False(bd.IsDisposed());

            to2.Dispose();

            Assert.True(bd.IsDisposed());

            cs.Test().AssertFailure(typeof(OperationCanceledException));
        }

        [Test]
        public void Complete_Dispose_Other()
        {
            var cs = new CompletableSubject();

            var to = new TestObserver<object>();

            cs.Subscribe(() => { to.Dispose(); });
            cs.Subscribe(to);

            cs.OnCompleted();

            to.AssertEmpty();
        }
    }
}
