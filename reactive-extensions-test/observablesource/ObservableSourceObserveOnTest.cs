using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceObserveOnTest
    {
        [Test]
        public void Regular_Regular_Basic()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Range(1, 5).Hide()
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test()
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertResult(1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Regular_Regular_Async()
        {
            for (int i = 0; i < 4; i++)
            {
                var name = -1;

                ObservableSource.Range(1, 5).Hide()
                    .ObserveOn(ThreadPoolScheduler.Instance, delayError: i / 2 == 0, fair: i % 2 == 0)
                    .DoOnNext(v => name = Thread.CurrentThread.ManagedThreadId)
                    .Test()
                    .WithTag($"delayError={i / 2}, fair={i % 2}")
                    .AwaitDone(TimeSpan.FromSeconds(5))
                    .AssertResult(1, 2, 3, 4, 5);

                Assert.AreNotEqual(-1, name);
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
            }
        }

        [Test]
        public void Regular_Regular_Error()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Error<int>(new InvalidOperationException()).Hide()
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test()
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Regular_Regular_Error_Not_Delayed()
        {
            for (int i = 0; i < 2; i++)
            {
                var ts = new TestScheduler();
                var subj = new PublishSubject<int>();

                var to = subj.ObserveOn(ts, false, fair: i == 0)
                    .Test()
                    .WithTag($"fair={i == 0}");

                Assert.True(subj.HasObservers);

                to.AssertEmpty();

                subj.OnNext(1);
                subj.OnNext(2);
                subj.OnError(new InvalidOperationException());

                to.AssertEmpty();

                ts.AdvanceTimeBy(1);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Regular_Regular_Error_Delayed()
        {
            for (int i = 0; i < 2; i++)
            {
                var ts = new TestScheduler();
                var subj = new PublishSubject<int>();

                var to = subj.ObserveOn(ts, true, fair: i == 0)
                    .Test()
                    .WithTag($"fair={i == 0}");

                Assert.True(subj.HasObservers);

                to.AssertEmpty();

                subj.OnNext(1);
                subj.OnNext(2);
                subj.OnError(new InvalidOperationException());

                to.AssertEmpty();

                ts.AdvanceTimeBy(1);

                to.AssertFailure(typeof(InvalidOperationException), 1, 2);
            }
        }

        [Test]
        public void Sync_Regular_Basic()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Range(1, 5)
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test()
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertResult(1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Sync_Regular_Async()
        {
            for (int i = 0; i < 4; i++)
            {
                var name = -1;

                ObservableSource.Range(1, 5)
                    .ObserveOn(ThreadPoolScheduler.Instance, delayError: i / 2 == 0, fair: i % 2 == 0)
                    .DoOnNext(v => name = Thread.CurrentThread.ManagedThreadId)
                    .Test()
                    .WithTag($"delayError={i / 2}, fair={i % 2}")
                    .AwaitDone(TimeSpan.FromSeconds(5))
                    .AssertResult(1, 2, 3, 4, 5);

                Assert.AreNotEqual(-1, name);
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
            }
        }

        [Test]
        public void Sync_Regular_Error()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Error<int>(new InvalidOperationException())
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test()
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Async_Regular_Basic()
        {
            for (int i = 0; i < 4; i++)
            {
                var ms = new MonocastSubject<int>();
                ms.EmitAll(1, 2, 3, 4, 5);

                ms
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test()
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertResult(1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Async_Regular_Async()
        {
            for (int i = 0; i < 4; i++)
            {
                var name = -1;

                var ms = new MonocastSubject<int>();
                ms.EmitAll(1, 2, 3, 4, 5);

                ms
                    .ObserveOn(ThreadPoolScheduler.Instance, delayError: i / 2 == 0, fair: i % 2 == 0)
                    .DoOnNext(v => name = Thread.CurrentThread.ManagedThreadId)
                    .Test()
                    .WithTag($"delayError={i / 2}, fair={i % 2}")
                    .AwaitDone(TimeSpan.FromSeconds(5))
                    .AssertResult(1, 2, 3, 4, 5);

                Assert.AreNotEqual(-1, name);
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
            }
        }

        [Test]
        public void Async_Regular_Error()
        {
            for (int i = 0; i < 4; i++)
            {
                var ms = new MonocastSubject<int>();
                ms.EmitError(new InvalidOperationException());

                ms
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test()
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Async_Regular_Error_Not_Delayed()
        {
            for (int i = 0; i < 2; i++)
            {
                var ts = new TestScheduler();
                var subj = new MonocastSubject<int>();

                var to = subj.ObserveOn(ts, false, fair: i == 0)
                    .Test()
                    .WithTag($"fair={i == 0}");

                Assert.True(subj.HasObservers);

                to.AssertEmpty();

                subj.OnNext(1);
                subj.OnNext(2);
                subj.OnError(new InvalidOperationException());

                to.AssertEmpty();

                ts.AdvanceTimeBy(1);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Async_Regular_Error_Delayed()
        {
            for (int i = 0; i < 2; i++)
            {
                var ts = new TestScheduler();
                var subj = new MonocastSubject<int>();

                var to = subj.ObserveOn(ts, true, fair: i == 0)
                    .Test()
                    .WithTag($"fair={i == 0}");

                Assert.True(subj.HasObservers);

                to.AssertEmpty();

                subj.OnNext(1);
                subj.OnNext(2);
                subj.OnError(new InvalidOperationException());

                to.AssertEmpty();

                ts.AdvanceTimeBy(1);

                to.AssertFailure(typeof(InvalidOperationException), 1, 2);
            }
        }

        [Test]
        public void Regular_Async_Basic()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Range(1, 5).Hide()
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertResult(1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Regular_Async_Async()
        {
            for (int i = 0; i < 4; i++)
            {
                var name = -1;

                ObservableSource.Range(1, 5).Hide()
                    .ObserveOn(ThreadPoolScheduler.Instance, delayError: i / 2 == 0, fair: i % 2 == 0)
                    .Map(v => { name = Thread.CurrentThread.ManagedThreadId; return v; })
                    .Test(fusionMode: FusionSupport.Any)
                    .AssertFuseable()
                    .AssertFusionMode(FusionSupport.Async)
                    .WithTag($"delayError={i / 2}, fair={i % 2}")
                    .AwaitDone(TimeSpan.FromSeconds(5))
                    .AssertResult(1, 2, 3, 4, 5);

                Assert.AreNotEqual(-1, name);
                Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
            }
        }

        [Test]
        public void Regular_Async_Error()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Error<int>(new InvalidOperationException()).Hide()
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Sync_Rejected()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Range(1, 5)
                .Map(v => v + 1)
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test()
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertResult(2, 3, 4, 5, 6);
            }
        }

        [Test]
        public void Async_Rejected()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Range(1, 5)
                .ObserveOn(reactive_extensions.ImmediateScheduler.INSTANCE, delayError: i / 2 == 0, fair: i % 2 == 0)
                .Test(fusionMode: FusionSupport.Sync)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .WithTag($"delayError={i / 2}, fair={i % 2}")
                .AssertResult(1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Lots_Of_Items()
        {
            for (int i = 0; i < 4; i++)
            {
                ObservableSource.Range(1, 1000).Hide()
                    .ObserveOn(ThreadPoolScheduler.Instance, delayError: i / 2 == 0, fair: i % 2 == 0)
                    .Test()
                    .AwaitDone(TimeSpan.FromSeconds(5))
                    .AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }
    }
}
