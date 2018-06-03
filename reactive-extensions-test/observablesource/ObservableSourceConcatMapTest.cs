using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceConcatMapTest
    {
        [Test]
        public void Regular_Basic()
        {
            ObservableSource.Range(1, 5).Hide()
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Test()
                .AssertResult(10, 11, 12, 13, 14);
        }

        [Test]
        public void Regular_Take()
        {
            ObservableSource.Range(1, 5).Hide()
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Take(3)
                .Test()
                .AssertResult(10, 11, 12);
        }

        [Test]
        public void Regular_Empty()
        {
            ObservableSource.Empty<int>().Hide()
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Test()
                .AssertResult();
        }

        [Test]
        public void Regular_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException()).Hide()
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Regular_Inner_Error()
        {
            ObservableSource.Range(1, 5).Hide()
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(9 + v);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11);
        }

        [Test]
        public void Regular_Inner_Error_Delayed()
        {
            ObservableSource.Range(1, 5).Hide()
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(9 + v);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11, 13, 14);
        }

        [Test]
        public void Regular_Mapper_Crash()
        {
            ObservableSource.Range(1, 5).Hide()
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return ObservableSource.Just(9 + v);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11);
        }

        [Test]
        public void Regular_Mapper_Crash_Delayed()
        {
            ObservableSource.Range(1, 5).Hide()
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return ObservableSource.Just(9 + v);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11);
        }

        [Test]
        public void Fused_Basic()
        {
            ObservableSource.Range(1, 5)
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Test()
                .AssertResult(10, 11, 12, 13, 14);
        }

        [Test]
        public void Fused_Take()
        {
            ObservableSource.Range(1, 5)
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Take(3)
                .Test()
                .AssertResult(10, 11, 12);
        }

        [Test]
        public void Fused_Empty()
        {
            ObservableSource.Empty<int>()
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Test()
                .AssertResult();
        }

        [Test]
        public void Fused_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .ConcatMap(v => ObservableSource.Just(9 + v))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused_Inner_Error()
        {
            ObservableSource.Range(1, 5)
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(9 + v);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11);
        }

        [Test]
        public void Fused_Inner_Error_Delayed()
        {
            ObservableSource.Range(1, 5)
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Just(9 + v);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11, 13, 14);
        }

        [Test]
        public void Fused_Mapper_Crash()
        {
            ObservableSource.Range(1, 5)
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return ObservableSource.Just(9 + v);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11);
        }

        [Test]
        public void Fused_Mapper_Crash_Delayed()
        {
            ObservableSource.Range(1, 5)
                .ConcatMap(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return ObservableSource.Just(9 + v);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 10, 11);
        }

        [Test]
        public void Fused_Sync_TryPoll_Crash()
        {
            ObservableSource.Range(1, 5)
                .Map<int, int>(v => throw new InvalidOperationException())
                .ConcatMap(v =>
                {
                    return ObservableSource.Just(9 + v);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused_Async_TryPoll_Crash()
        {
            var ms = new MonocastSubject<int>();
            var to = ms
                .Map<int, int>(v => throw new InvalidOperationException())
                .ConcatMap(v =>
                {
                    return ObservableSource.Just(9 + v);
                })
                .Test();

            ms.OnNext(1);

            Assert.False(ms.HasObservers);

            to
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Concat_Basic()
        {
            ObservableSource.FromArray(
                    ObservableSource.Just(1),
                    ObservableSource.Just(2),
                    ObservableSource.Just(3),
                    ObservableSource.Just(4),
                    ObservableSource.Just(5)
                )
                .Concat()
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
