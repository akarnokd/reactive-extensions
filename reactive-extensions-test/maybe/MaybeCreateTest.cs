using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeCreateTest
    {
        [Test]
        public void Success()
        {
            var resource = new SingleAssignmentDisposable();

            var before = -1;
            var after = -1;

            var source = MaybeSource.Create<int>(e =>
            {
                e.SetResource(resource);
                before = e.IsDisposed() ? 1 : 0;
                e.OnSuccess(1);
                after = e.IsDisposed() ? 1 : 0;
            });

            source.Test()
                .AssertResult(1);

            Assert.AreEqual(0, before);
            Assert.AreEqual(1, after);

            Assert.True(resource.IsDisposed());
        }

        [Test]
        public void Empty()
        {
            var resource = new SingleAssignmentDisposable();

            var before = -1;
            var after = -1;

            var source = MaybeSource.Create<int>(e =>
            {
                e.SetResource(resource);
                before = e.IsDisposed() ? 1 : 0;
                e.OnCompleted();
                after = e.IsDisposed() ? 1 : 0;
            });

            source.Test()
                .AssertResult();

            Assert.AreEqual(0, before);
            Assert.AreEqual(1, after);

            Assert.True(resource.IsDisposed());
        }

        [Test]
        public void Error()
        {
            var resource = new SingleAssignmentDisposable();

            var before = -1;
            var after = -1;

            var source = MaybeSource.Create<int>(e =>
            {
                e.SetResource(resource);
                before = e.IsDisposed() ? 1 : 0;
                e.OnError(new InvalidOperationException());
                after = e.IsDisposed() ? 1 : 0;
            });

            source.Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, before);
            Assert.AreEqual(1, after);

            Assert.True(resource.IsDisposed());
        }

        [Test]
        public void Dispose()
        {
            var resource = new SingleAssignmentDisposable();

            var source = MaybeSource.Create<int>(e =>
            {
                e.SetResource(resource);
            });

            source.Test().Dispose();

            Assert.True(resource.IsDisposed());
        }

        [Test]
        public void Change_Resource_Disposes_Old()
        {
            var resource1 = new SingleAssignmentDisposable();
            var resource2 = new SingleAssignmentDisposable();

            var source = MaybeSource.Create<int>(e =>
            {
                e.SetResource(resource1);
                e.SetResource(resource2);
            });

            source.Test();

            Assert.True(resource1.IsDisposed());
            Assert.False(resource2.IsDisposed());
        }
    }
}
