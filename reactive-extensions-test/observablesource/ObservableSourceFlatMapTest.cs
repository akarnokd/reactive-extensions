using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceFlatMapTest
    {
        [Test]
        public void Max_Basic()
        {
            ObservableSource.Range(1, 5)
                .FlatMap(v => ObservableSource.Just(v).Hide())
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Max_Basic_MaxConcurrency()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5)
                    .FlatMap(v => ObservableSource.Just(v).Hide(), maxConcurrency: i)
                    .Test()
                    .WithTag($"maxConcurrency: {i}")
                    .AssertResult(1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Max_Error_Outer()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => ObservableSource.Just(v).Hide())
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Max_Error_Inner()
        {
            ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException()).Hide();
                    }
                    return ObservableSource.Just(v).Hide();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Max_Basic_Scalar()
        {
            ObservableSource.Range(1, 5)
                .FlatMap(v => ObservableSource.Just(v))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Max_Error_Outer_Scalar()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => ObservableSource.Just(v))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Max_Error_Inner_Scalar()
        {
            ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(v);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Max_Error_Inner_DelayError()
        {
            ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException()).Hide();
                    }
                    return ObservableSource.Just(v).Hide();
                }, delayErrors: true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 4, 5);
        }

        [Test]
        public void Max_Error_Inner_Scalar_DelayError()
        {
            ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(v);
                }, delayErrors: true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 4, 5);
        }

        [Test]
        public void Max_Basic_Threaded()
        {
            ObservableSource.Range(1, 5)
                .FlatMap(v => ObservableSource.Just(v).SubscribeOn(ThreadPoolScheduler.Instance))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(5)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Max_Basic_Threaded_Long()
        {
            ObservableSource.Range(1, 100)
                .FlatMap(v => ObservableSource.Range(v * 100, 5).SubscribeOn(ThreadPoolScheduler.Instance))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(500)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Max_Fused_Long()
        {
            ObservableSource.Range(1, 100)
                .FlatMap(v => ObservableSource.Range(v * 100, 100))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(10000)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Max_Fused_Threaded_Long()
        {
            ObservableSource.Range(1, 100)
                .FlatMap(v => ObservableSource.Range(v * 1000, 100).ObserveOn(ThreadPoolScheduler.Instance))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(10000)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Max_Mixed_Long()
        {
            ObservableSource.Range(0, 1200)
            .FlatMap(v => {
                var type = v % 6;
                switch (type)
                {
                    case 0: return ObservableSource.Just(v).Hide();
                    case 1: return ObservableSource.Just(v);
                    case 2: return ObservableSource.Range(v, 2).Hide();
                    case 3: return ObservableSource.Range(v, 2);
                    case 4: return ObservableSource.Empty<int>().Hide();
                    case 5: return ObservableSource.Empty<int>();
                }
                throw new InvalidOperationException();
            })
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertSubscribed()
            .AssertValueCount(1200)
            .AssertNoError()
            .AssertCompleted();
        }

        [Test]
        public void Max_Mixed_Long_Thread()
        {
            ObservableSource.Range(0, 1200)
            .FlatMap(v => {
                var type = v % 12;
                switch (type)
                {
                    case 0: return ObservableSource.Just(v).Hide();
                    case 1: return ObservableSource.Just(v);
                    case 2: return ObservableSource.Range(v, 2).Hide();
                    case 3: return ObservableSource.Range(v, 2);
                    case 4: return ObservableSource.Empty<int>().Hide();
                    case 5: return ObservableSource.Empty<int>();
                    case 6: return ObservableSource.Just(v).SubscribeOn(ThreadPoolScheduler.Instance);
                    case 7: return ObservableSource.Just(v).ObserveOn(ThreadPoolScheduler.Instance);
                    case 8: return ObservableSource.Empty<int>().SubscribeOn(ThreadPoolScheduler.Instance);
                    case 9: return ObservableSource.Empty<int>().ObserveOn(ThreadPoolScheduler.Instance);
                    case 10: return ObservableSource.Range(v, 2);//.SubscribeOn(ThreadPoolScheduler.Instance);
                    case 11: return ObservableSource.Range(v, 2);//.ObserveOn(ThreadPoolScheduler.Instance);
                    }
                throw new InvalidOperationException();
            })
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertSubscribed()
            .AssertValueCount(1200)
            .AssertNoError()
            .AssertCompleted();
        }

        [Test]
        public void Max_Dispose()
        {
            var subj = new PublishSubject<int>();
            var inner = new PublishSubject<int>();

            var to = subj.FlatMap(v => inner).Test();

            to.AssertEmpty();

            Assert.True(subj.HasObservers);
            Assert.False(inner.HasObservers);

            subj.OnNext(1);

            Assert.True(subj.HasObservers);
            Assert.True(inner.HasObservers);

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(inner.HasObservers);
        }

        [Test]
        public void Max_Mapper_Crash_Disposes()
        {
            var subj = new PublishSubject<int>();

            var to = subj.FlatMap<int, int>(v => throw new InvalidOperationException()).Test();

            to.AssertEmpty();

            subj.OnNext(1);

            Assert.False(subj.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Scalar_Error_Disposes()
        {
            var subj = new PublishSubject<int>();

            var to = subj.FlatMap<int, int>(v => ObservableSource.Error<int>(new InvalidOperationException())).Test();

            to.AssertEmpty();

            subj.OnNext(1);

            Assert.False(subj.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Limited_Basic()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5)
                    .FlatMap(v => ObservableSource.Just(v).Hide(), maxConcurrency: i)
                    .Test()
                    .WithTag($"maxConcurrency: {i}")
                    .AssertResult(1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Limited_Basic_Threaded()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5)
                .FlatMap(v => ObservableSource.Just(v).SubscribeOn(ThreadPoolScheduler.Instance), maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(5)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Basic_Threaded_Long()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 100)
                .FlatMap(v => ObservableSource.Range(v + 100, 5).SubscribeOn(ThreadPoolScheduler.Instance), maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(500)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Switching_1()
        {
            var subj = new[]
            {
                new PublishSubject<int>(),
                new PublishSubject<int>(),
                new PublishSubject<int>()
            };

            var to = ObservableSource.Range(0, 3)
                .FlatMap(v => subj[v], maxConcurrency: 1)
                .Test();

            Assert.True(subj[0].HasObservers);
            Assert.False(subj[1].HasObservers);
            Assert.False(subj[2].HasObservers);

            to.AssertEmpty();

            subj[0].OnNext(1);

            to.AssertValuesOnly(1);

            subj[0].OnCompleted();

            Assert.False(subj[0].HasObservers);
            Assert.True(subj[1].HasObservers);
            Assert.False(subj[2].HasObservers);

            subj[1].OnNext(2);

            to.AssertValuesOnly(1, 2);

            subj[1].OnCompleted();

            Assert.False(subj[0].HasObservers);
            Assert.False(subj[1].HasObservers);
            Assert.True(subj[2].HasObservers);

            subj[2].OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            subj[2].OnCompleted();

            to.AssertResult(1, 2, 3);

            Assert.False(subj[0].HasObservers);
            Assert.False(subj[1].HasObservers);
            Assert.False(subj[2].HasObservers);
        }

        [Test]
        public void Limited_Fused_Switching_1()
        {
            var subj = new[]
            {
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
                new MonocastSubject<int>()
            };

            var to = ObservableSource.Range(0, 3)
                .FlatMap(v => subj[v], maxConcurrency: 1)
                .Test();

            Assert.True(subj[0].HasObservers);
            Assert.False(subj[1].HasObservers);
            Assert.False(subj[2].HasObservers);

            to.AssertEmpty();

            subj[0].OnNext(1);

            to.AssertValuesOnly(1);

            subj[0].OnCompleted();

            Assert.False(subj[0].HasObservers);
            Assert.True(subj[1].HasObservers);
            Assert.False(subj[2].HasObservers);

            subj[1].OnNext(2);

            to.AssertValuesOnly(1, 2);

            subj[1].OnCompleted();

            Assert.False(subj[0].HasObservers);
            Assert.False(subj[1].HasObservers);
            Assert.True(subj[2].HasObservers);

            subj[2].OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            subj[2].OnCompleted();

            to.AssertResult(1, 2, 3);

            Assert.False(subj[0].HasObservers);
            Assert.False(subj[1].HasObservers);
            Assert.False(subj[2].HasObservers);
        }

        [Test]
        public void Limited_Fused_Long()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 100)
                .FlatMap(v => ObservableSource.Range(v * 100, 100), maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(10000)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Fused_Threaded_Long()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 100)
                .FlatMap(v => ObservableSource.Range(v * 1000, 100).ObserveOn(ThreadPoolScheduler.Instance), maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(10000)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Fused_Threaded_Long_Offline()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 100)
                .FlatMap(v => {
                    var ms = new MonocastSubject<int>();
                    ObservableSource.Range(v * 1000, 100).Subscribe(ms);
                    return ms;
                }, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(10000)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Fused_Threaded_Long_Online()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 100)
                .FlatMap(v => {
                    var ms = new MonocastSubject<int>();
                    ObservableSource.Range(v * 1000, 100).SubscribeOn(ThreadPoolScheduler.Instance).Subscribe(ms);
                    return ms;
                }, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(10000)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Mixed_Long_Thread()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(0, 1200)
                .FlatMap(v => {
                    var type = v % 12;
                    switch (type)
                    {
                        case 0: return ObservableSource.Just(v).Hide();
                        case 1: return ObservableSource.Just(v);
                        case 2: return ObservableSource.Range(v, 2).Hide();
                        case 3: return ObservableSource.Range(v, 2);
                        case 4: return ObservableSource.Empty<int>().Hide();
                        case 5: return ObservableSource.Empty<int>();
                        case 6: return ObservableSource.Just(v).SubscribeOn(ThreadPoolScheduler.Instance);
                        case 7: return ObservableSource.Just(v).ObserveOn(ThreadPoolScheduler.Instance);
                        case 8: return ObservableSource.Empty<int>().SubscribeOn(ThreadPoolScheduler.Instance);
                        case 9: return ObservableSource.Empty<int>().ObserveOn(ThreadPoolScheduler.Instance);
                        case 10: return ObservableSource.Range(v, 2);//.SubscribeOn(ThreadPoolScheduler.Instance);
                        case 11: return ObservableSource.Range(v, 2);//.ObserveOn(ThreadPoolScheduler.Instance);
                    }
                    throw new InvalidOperationException();
                }, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(1200)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Mixed_Long()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(0, 1200)
                .FlatMap(v => {
                    var type = v % 6;
                    switch (type)
                    {
                        case 0: return ObservableSource.Just(v).Hide();
                        case 1: return ObservableSource.Just(v);
                        case 2: return ObservableSource.Range(v, 2).Hide();
                        case 3: return ObservableSource.Range(v, 2);
                        case 4: return ObservableSource.Empty<int>().Hide();
                        case 5: return ObservableSource.Empty<int>();
                    }
                    throw new InvalidOperationException();
                }, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertSubscribed()
                .AssertValueCount(1200)
                .AssertNoError()
                .AssertCompleted();
            }
        }

        [Test]
        public void Limited_Error_Outer()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => ObservableSource.Just(v).Hide(), maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Limited_Error_Inner()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException()).Hide();
                    }
                    return ObservableSource.Just(v).Hide();
                }, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
            }
        }

        [Test]
        public void Limited_Error_Inner_DelayError()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException()).Hide();
                    }
                    return ObservableSource.Just(v).Hide();
                }, delayErrors: true, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 4, 5);
            }
        }

        [Test]
        public void Limited_Error_Outer_Scalar()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => ObservableSource.Just(v), maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Limited_Error_Inner_Scalar()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(v);
                }, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
            }
        }

        [Test]
        public void Limited_Error_Inner_DelayError_Scalar()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(v);
                }, delayErrors: true, maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 4, 5);
            }
        }

        [Test]
        public void Limited_Dispose()
        {
            for (int i = 1; i < 7; i++)
            {
                var subj = new PublishSubject<int>();
                var inner = new PublishSubject<int>();

                var to = subj.FlatMap(v => inner, maxConcurrency: i).Test();

                to.WithTag($"maxConcurrency: {i}")
                    .AssertEmpty();

                Assert.True(subj.HasObservers);
                Assert.False(inner.HasObservers);

                subj.OnNext(1);

                Assert.True(subj.HasObservers);
                Assert.True(inner.HasObservers);

                to.Dispose();

                Assert.False(subj.HasObservers);
                Assert.False(inner.HasObservers);
            }
        }

        [Test]
        public void Limited_Mapper_Crash_Disposes()
        {
            for (int i = 1; i < 7; i++)
            {
                var subj = new PublishSubject<int>();

                var to = subj.FlatMap<int, int>(v => throw new InvalidOperationException(), maxConcurrency: i).Test();

                to.WithTag($"maxConcurrency: {i}")
                    .AssertEmpty();

                subj.OnNext(1);

                Assert.False(subj.HasObservers);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limited_Scalar_Error_Disposes()
        {
            for (int i = 1; i < 7; i++)
            {
                var subj = new PublishSubject<int>();

                var to = subj.FlatMap<int, int>(v => ObservableSource.Error<int>(new InvalidOperationException()), maxConcurrency: i).Test();

                to.WithTag($"maxConcurrency: {i}")
                    .AssertEmpty();

                subj.OnNext(1);

                Assert.False(subj.HasObservers);

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }
    }
}
