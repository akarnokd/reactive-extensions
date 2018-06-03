using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceFromActionTest
    {
        [Test]
        public void Regular_Basic()
        {
            var count = 0;

            var source = ObservableSource.FromAction<int>(() => ++count);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, count);

                source.Test().AssertResult();

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Regular_Error()
        {
            var count = 0;

            var source = ObservableSource.FromAction<int>(() =>
            {
                ++count;
                throw new InvalidOperationException();
            });

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, count);

                source.Test()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Fused_Basic()
        {
            var count = 0;

            var source = ObservableSource.FromAction<int>(() => ++count);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, count);

                source.Test(fusionMode: FusionSupport.Any)
                    .AssertFuseable()
                    .AssertFusionMode(FusionSupport.Async)
                    .AssertResult();

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Fused_Error()
        {
            var count = 0;

            var source = ObservableSource.FromAction<int>(() =>
            {
                ++count;
                throw new InvalidOperationException();
            });

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, count);

                source.Test(fusionMode: FusionSupport.Any)
                    .AssertFuseable()
                    .AssertFusionMode(FusionSupport.Async)
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Dynamic_Source()
        {
            var count = 0;

            var source = ObservableSource.FromAction<int>(() => ++count) as IDynamicValue<int>;

            for (int i = 0; i < 10; i++)
            {
                var v = source.GetValue(out var success);
                Assert.False(success);
            }
        }

        [Test]
        public void Dynamic_Source_Error()
        {
            var count = 0;

            var source = ObservableSource.FromAction<int>(() => 
            {
                count++;
                throw new InvalidOperationException();
            }) as IDynamicValue<int>;

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    source.GetValue(out var success);
                    Assert.Fail();
                }
                catch (InvalidOperationException)
                {
                    // expected
                }
            }
        }
    }
}
