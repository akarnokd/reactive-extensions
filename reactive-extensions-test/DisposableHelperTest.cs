using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class DisposableHelperTest
    {
        [Test]
        public void Set()
        {
            var bd1 = new BooleanDisposable();
            var bd2 = new BooleanDisposable();

            var field = default(IDisposable);

            Assert.True(DisposableHelper.Set(ref field, bd1));

            Assert.False(bd1.IsDisposed());
            Assert.False(bd2.IsDisposed());

            Assert.True(DisposableHelper.Set(ref field, bd2));

            Assert.True(bd1.IsDisposed());
            Assert.False(bd2.IsDisposed());

            Assert.True(DisposableHelper.Dispose(ref field));

            Assert.True(bd1.IsDisposed());
            Assert.True(bd2.IsDisposed());

            var bd3 = new BooleanDisposable();

            Assert.False(DisposableHelper.Set(ref field, bd3));

            Assert.True(bd3.IsDisposed());
        }

        [Test]
        public void SetOnce_Null()
        {
            try
            {
                var field = default(IDisposable);

                DisposableHelper.SetOnce(ref field, null);
                Assert.Fail("Should have thrown");
            }
            catch (ArgumentNullException)
            {
                // expected
            } 
        }

        [Test]
        public void SetIfNull_Null()
        {
            try
            {
                var field = default(IDisposable);

                DisposableHelper.SetIfNull(ref field, null);
                Assert.Fail("Should have thrown");
            }
            catch (ArgumentNullException)
            {
                // expected
            }
        }

        [Test]
        public void ReplaceIfNull_Null()
        {
            try
            {
                var field = default(IDisposable);

                DisposableHelper.ReplaceIfNull(ref field, null);
                Assert.Fail("Should have thrown");
            }
            catch (ArgumentNullException)
            {
                // expected
            }
        }

        [Test]
        public void ReplaceIfNull()
        {
            var bd1 = new BooleanDisposable();
            var bd2 = new BooleanDisposable();

            var field = default(IDisposable);

            Assert.True(DisposableHelper.ReplaceIfNull(ref field, bd1));

            Assert.False(bd1.IsDisposed());
            Assert.False(bd2.IsDisposed());

            Assert.False(DisposableHelper.ReplaceIfNull(ref field, bd2));

            Assert.False(bd1.IsDisposed());
            Assert.False(bd2.IsDisposed());

            Assert.True(DisposableHelper.Dispose(ref field));

            Assert.True(bd1.IsDisposed());
            Assert.False(bd2.IsDisposed());

            var bd3 = new BooleanDisposable();

            Assert.False(DisposableHelper.SetIfNull(ref field, bd3));

            Assert.True(bd3.IsDisposed());
        }

        [Test]
        public void EmptyDisposable_TryOffer_Throws()
        {

            try
            {
                (DisposableHelper.Empty<int>() as IFuseableDisposable<int>)?.TryOffer(1);
                Assert.Fail("Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }
    }
}
