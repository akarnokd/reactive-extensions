using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;
using System.Threading;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class SpscLinkedArrayQueueTest
    {
        [Test]
        public void Basic_SameIsland()
        {
            var q = new SpscLinkedArrayQueue<int>(8);

            for (int i = 0; i < 16; i++)
            {
                Assert.True(q.IsEmpty());
                q.Offer(i);

                Assert.False(q.IsEmpty());

                Assert.AreEqual(i, q.TryPoll(out var success));

                Assert.True(success);
                Assert.True(q.IsEmpty());
            }

            q.TryPoll(out var success2);
            Assert.False(success2);
            Assert.True(q.IsEmpty());
        }

        [Test]
        public void Basic_Multi_Island()
        {
            var q = new SpscLinkedArrayQueue<int>(8);

            for (int i = 0; i < 33; i++)
            {
                q.Offer(i);
            }

            Assert.False(q.IsEmpty());

            for (int i = 0; i < 33; i++)
            {
                Assert.AreEqual(i, q.TryPoll(out var success));
                Assert.True(success);
            }

            q.TryPoll(out var success2);
            Assert.False(success2);
            Assert.True(q.IsEmpty());
        }

        [Test]
        public void Basic_Clear()
        {
            var q = new SpscLinkedArrayQueue<int>(8);

            Assert.True(q.IsEmpty());

            for (int i = 0; i < 33; i++)
            {
                q.Offer(i);
            }

            Assert.False(q.IsEmpty());

            q.Clear();

            Assert.True(q.IsEmpty());

            q.Clear();

            Assert.True(q.IsEmpty());
        }

        [Test]
        public void Async_Short()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var q = new SpscLinkedArrayQueue<int>(8);

                var n = 1000;

                var stop = new bool[1];

                Action a1 = () =>
                {
                    for (int j = 0; j < n; j++)
                    {
                        q.Offer(j);
                    }
                };

                Action a2 = () =>
                {
                    int v = 0;
                    for (int j = 0; j < n; j++)
                    {
                        for (; ;)
                        {
                            v = q.TryPoll(out var success);
                            if (success || Volatile.Read(ref stop[0]))
                            {
                                break;
                            }
                        }

                        Assert.AreEqual(j, v);
                    }
                };

                try
                {
                    TestHelper.Race(a1, a2);
                }
                finally
                {
                    Volatile.Write(ref stop[0], true);
                }
            }
        }
    }
}
