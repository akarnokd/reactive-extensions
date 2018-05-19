using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableFlatMapObservableTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            new [] { source, source, source }
            .ToObservable()
            .FlatMap(v => v)
            .Test()
            .AssertResult();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Basic_Delay_Errors()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            new[] { source, source, source }
            .ToObservable()
            .FlatMap(v => v, true)
            .Test()
            .AssertResult();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Basic_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);

                new[] { source, source, source }
                .ToObservable()
                .FlatMap(v => v, maxConcurrency: i)
                .Test()
                .WithTag("maxConcurrency=" + i)
                .AssertResult();

                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void Basic_Max_Concurrency_Delay_Errors()
        {
            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);

                new[] { source, source, source }
                .ToObservable()
                .FlatMap(v => v, true, i)
                .Test()
                .WithTag("maxConcurrency=" + i)
                .AssertResult();

                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void Error()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);
            var err = CompletableSource.Error(new InvalidOperationException());

            new [] { source, err, source }
                .ToObservable()
                .FlatMap(v => v)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);
                var err = CompletableSource.Error(new InvalidOperationException());

                new[] { source, err, source }
                    .ToObservable()
                    .FlatMap(v => v, maxConcurrency: i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(1, count);
            }
        }

        [Test]
        public void Error_Delayed()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);
            var err = CompletableSource.Error(new InvalidOperationException());

            new[] { source, err, source }
                .ToObservable()
                .FlatMap(v => v, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Error_Delayed_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);
                var err = CompletableSource.Error(new InvalidOperationException());

                new[] { source, err, source }
                    .ToObservable()
                    .FlatMap(v => v, true, i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void Dispose()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = new [] { cs1, cs2 }
                .ToObservable()
                .FlatMap(v => v)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
        }

        [Test]
        public void Dispose_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = new[] { cs1, cs2 }
                .ToObservable()
                .FlatMap(v => v, maxConcurrency: 2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
        }

        [Test]
        public void Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = new[] { cs1, cs2 }
                .ToObservable()
                .FlatMap(v => v, maxConcurrency: 1)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            cs1.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnCompleted();

            to.AssertResult();
        }

        [Test]
        public void Error_Disposes_Other()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = new[] { cs1, cs2 }
                .ToObservable()
                .FlatMap(v => v)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Disposes_Other_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = new[] { cs1, cs2 }
                .ToObservable()
                .FlatMap(v => v, maxConcurrency: 2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs1 = new CompletableSubject();
                var cs2 = new CompletableSubject();

                var to = new[] { cs1, cs2 }
                    .ToObservable()
                    .Merge()
                    .Test();

                TestHelper.Race(() => {
                    cs1.OnCompleted();
                }, () => {
                    cs2.OnCompleted();
                });

                to.AssertResult();
            }
        }

        [Test]
        public void Race_MaxConcurrent()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var cs1 = new CompletableSubject();
                    var cs2 = new CompletableSubject();

                    var to = new[] { cs1, cs2 }
                        .ToObservable()
                        .Merge(maxConcurrency: k)
                                .Test();

                    TestHelper.Race(() =>
                    {
                        cs1.OnCompleted();
                    }, () =>
                    {
                        cs2.OnCompleted();
                    });

                    to.AssertResult();
                }
            }
        }

        [Test]
        public void Mapper_Crash()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            var u = 0;

            new[] { source, source, source }
            .ToObservable()
            .FlatMap(v =>
            {
                if (++u == 2)
                {
                    throw new InvalidOperationException();
                }
                return v;
            })
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Mapper_Crash_Max_Concurrency()
        {
            for (int k = 1; k < 5; k++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);

                var u = 0;

                new[] { source, source, source }
                .ToObservable()
                .FlatMap(v =>
                {
                    if (++u == 2)
                    {
                        throw new InvalidOperationException();
                    }
                    return v;
                }, maxConcurrency: k)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(1, count);
            }
        }
    }
}
