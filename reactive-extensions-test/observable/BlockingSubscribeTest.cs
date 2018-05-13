using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class BlockingSubscribeTest
    {
        [Test]
        public void Basic_Observer()
        {
            var to = new TestObserver<int>();

            Observable.Range(1, 5)
                .BlockingSubscribe(to);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_Observer_Error()
        {
            var to = new TestObserver<int>();

            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .BlockingSubscribe(to);

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        [Timeout(5000)]
        public void Basic_Observer_Dispose()
        {
            var to = new TestObserver<int>();
            var sad = new SingleAssignmentDisposable();
            var cdl = new CountdownEvent(1);
            var us = new UnicastSubject<int>();

            Task.Factory.StartNew(() =>
            {
                while (to.ItemCount != 5) ;
                sad.Dispose();
                cdl.Signal();
            });

            Task.Factory.StartNew(() =>
            {
                us.Emit(1, 2, 3, 4, 5);
                cdl.Signal();
            });

            us.BlockingSubscribe(to, d => sad.Disposable = d);

            to.AssertValuesOnly(1, 2, 3, 4, 5);

            Assert.True(cdl.Wait(3000));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Basic_Action()
        {
            var to = new TestObserver<int>();

            Observable.Range(1, 5)
                .BlockingSubscribe(to.OnNext, to.OnError, to.OnCompleted);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_Action_Error()
        {
            var to = new TestObserver<int>();

            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .BlockingSubscribe(to.OnNext, to.OnError, to.OnCompleted);

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        [Timeout(5000)]
        public void Basic_Action_Dispose()
        {
            var to = new TestObserver<int>();
            var sad = new SingleAssignmentDisposable();
            var cdl = new CountdownEvent(2);
            var us = new UnicastSubject<int>();

            Task.Factory.StartNew(() =>
            {
                while (to.ItemCount != 5) ;
                sad.Dispose();
                cdl.Signal();
            });

            Task.Factory.StartNew(() =>
            {
                us.Emit(1, 2, 3, 4, 5);
                cdl.Signal();
            });

            us.BlockingSubscribe(to.OnNext, to.OnError, to.OnCompleted, d => sad.Disposable = d);

            to.AssertValuesOnly(1, 2, 3, 4, 5);

            Assert.True(cdl.Wait(3000));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Basic_While()
        {
            var to = new TestObserver<int>();

            Observable.Range(1, 5)
                .BlockingSubscribeWhile(v => {
                    to.OnNext(v);
                    return v != 3;
                }, to.OnError, to.OnCompleted);

            to.AssertResult(1, 2, 3);
        }

        [Test]
        public void Basic_While_Error()
        {
            var to = new TestObserver<int>();

            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .BlockingSubscribeWhile(v => {
                    to.OnNext(v);
                    return true;
                }, to.OnError, to.OnCompleted);

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        [Timeout(5000)]
        public void Basic_While_Dispose()
        {
            var to = new TestObserver<int>();
            var sad = new SingleAssignmentDisposable();
            var cdl = new CountdownEvent(2);
            var us = new UnicastSubject<int>();

            Task.Factory.StartNew(() =>
            {
                while (to.ItemCount != 5) ;
                sad.Dispose();
                cdl.Signal();
            });

            Task.Factory.StartNew(() =>
            {
                us.Emit(1, 2, 3, 4, 5);
                cdl.Signal();
            });

            us.BlockingSubscribeWhile(v => {
                to.OnNext(v);
                return true;
            }, to.OnError, to.OnCompleted, d => sad.Disposable = d);

            to.AssertValuesOnly(1, 2, 3, 4, 5);

            Assert.True(cdl.Wait(3000));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Race_Observer()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>();

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us.OnNext(j);
                    }
                    us.OnCompleted();
                },
                () =>
                {
                    us.BlockingSubscribe(to);
                });

                to.AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_Action()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>();

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us.OnNext(j);
                    }
                    us.OnCompleted();
                },
                () =>
                {
                    us.BlockingSubscribe(to.OnNext, to.OnError, to.OnCompleted);
                });

                to.AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_While()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>();

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us.OnNext(j);
                    }
                    us.OnCompleted();
                },
                () =>
                {
                    us.BlockingSubscribeWhile(v =>
                    {
                        to.OnNext(v);
                        return true;
                    }, to.OnError, to.OnCompleted);
                });

                to.AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }
    }
}
