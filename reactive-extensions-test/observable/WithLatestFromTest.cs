using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class WithLatestFromTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                      foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, Observable.Return(10), Observable.Return(100))
                .Test()
                .AssertResult(111, 112, 113, 114, 115);
        }

        [Test]
        public void Basic_SourceFirst()
        {
            Observable.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, false, true, Observable.Return(10), Observable.Return(100))
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error_Other()
        {
            Observable.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, Observable.Return(10).ConcatError(new InvalidOperationException())
                , Observable.Return(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Main()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, Observable.Return(10)
                , Observable.Return(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 111, 112, 113, 114, 115);
        }

        [Test]
        public void Error_Main_Main_First()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, false, true, Observable.Return(10)
                , Observable.Return(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Other_Delayed()
        {
            Observable.Range(1, 5)
                .WithLatestFrom<int, int, int>((a, bs) => {
                    foreach (var i in bs)
                    {
                        a += i;
                    }
                    return a;
                }, true, Observable.Return(10).ConcatError(new InvalidOperationException())
                , Observable.Return(100))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 
                    111, 112, 113, 114, 115);
        }

        [Test]
        public void Main_Disposes_Others_On_Completion()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            us1.Emit(10);
            us2.Emit(100);

            var source = new Subject<int>();

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
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            us1.Emit(10);
            us2.Emit(100);

            var source = new Subject<int>();

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
