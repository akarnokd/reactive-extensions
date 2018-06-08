using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceWithLatestFromTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                      foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, ObservableSource.Just(10), ObservableSource.Just(100))
                .Test()
                .AssertResult(111, 112, 113, 114, 115);
        }

        [Test]
        public void Basic_SourceFirst()
        {
            ObservableSource.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, false, true, ObservableSource.Just(10), ObservableSource.Just(100))
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error_Other()
        {
            ObservableSource.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, ObservableSource.Just(10).ConcatError(new InvalidOperationException())
                , ObservableSource.Just(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Main()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, ObservableSource.Just(10)
                , ObservableSource.Just(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 111, 112, 113, 114, 115);
        }

        [Test]
        public void Error_Main_Main_First()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, false, true, ObservableSource.Just(10)
                , ObservableSource.Just(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Other_Delayed()
        {
            ObservableSource.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, true, ObservableSource.Just(10).ConcatError(new InvalidOperationException())
                , ObservableSource.Just(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 
                    111, 112, 113, 114, 115);
        }

        [Test]
        public void Main_Disposes_Others_On_Completion()
        {
            var us1 = new MonocastSubject<int>();
            var us2 = new MonocastSubject<int>();

            us1.Emit(10);
            us2.Emit(100);

            var source = new PublishSubject<int>();

            var to = source.WithLatestFrom((a, bs) =>
            {
                foreach (var i in bs)
                {
                    a += i;
                }
                return a;
            }, us1, us2).Test();

            Assert.True(us1.HasObserver());
            Assert.True(us2.HasObserver());

            to.AssertEmpty();

            source.EmitAll(1, 2, 3, 4, 5);

            Assert.False(us1.HasObserver());
            Assert.False(us2.HasObserver());

            to.AssertResult(111, 112, 113, 114, 115);
        }

        [Test]
        public void Main_Disposes_Others_On_Error()
        {
            var us1 = new MonocastSubject<int>();
            var us2 = new MonocastSubject<int>();

            us1.Emit(10);
            us2.Emit(100);

            var source = new PublishSubject<int>();

            var to = source.WithLatestFrom((a, bs) =>
            {
                foreach (var i in bs)
                {
                    a += i;
                }
                return a;
            }, us1, us2).Test();

            Assert.True(us1.HasObserver());
            Assert.True(us2.HasObserver());

            to.AssertEmpty();

            source.EmitError(new InvalidOperationException(), 1, 2, 3, 4, 5);

            Assert.False(us1.HasObserver());
            Assert.False(us2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException), 111, 112, 113, 114, 115);
        }
    }
}
