using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class CreateTest
    {
        [Test]
        public void Basic()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);

                for (int i = 1; i < 6; i++)
                {
                    emitter.OnNext(i);
                }
                emitter.OnCompleted();
            })
            .Test()
            .AssertResult(1, 2, 3, 4, 5);

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Basic_Serialized()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);

                for (int i = 1; i < 6; i++)
                {
                    emitter.OnNext(i);
                }
                emitter.OnCompleted();
            }, true)
            .Test()
            .AssertResult(1, 2, 3, 4, 5);

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Error()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);

                for (int i = 1; i < 6; i++)
                {
                    emitter.OnNext(i);
                }
                emitter.OnError(new InvalidOperationException());
            })
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Error_Serialized()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);

                for (int i = 1; i < 6; i++)
                {
                    emitter.OnNext(i);
                }
                emitter.OnError(new InvalidOperationException());
            }, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Handler_Crash()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);

                for (int i = 1; i < 6; i++)
                {
                    emitter.OnNext(i);
                }
                throw new InvalidOperationException();
            })
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Handler_Crash_Serialized()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);

                for (int i = 1; i < 6; i++)
                {
                    emitter.OnNext(i);
                }
                throw new InvalidOperationException();
            }, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Disposed()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);
            })
            .Test()
            .Cancel()
            .AssertEmpty();

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Disposed_Serialized()
        {
            var sad = new SingleAssignmentDisposable();

            ReactiveExtensions.Create<int>(emitter =>
            {
                emitter.SetResource(sad);
            }, true)
            .Test()
            .Cancel()
            .AssertEmpty();

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Race()
        {
            for (int j = 0; j < TestHelper.RACE_LOOPS; j++)
            {
                var sad = new SingleAssignmentDisposable();

                var to = ReactiveExtensions.Create<int>(emitter =>
                {
                    emitter.SetResource(sad);

                    TestHelper.Race(() =>
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            if (emitter.IsDisposed())
                            {
                                return;
                            }
                            emitter.OnNext(i);
                        }
                        emitter.OnCompleted();
                    },
                    () =>
                    {
                        for (int i = 1000; i < 2000; i++)
                        {
                            if (emitter.IsDisposed())
                            {
                                return;
                            }
                            emitter.OnNext(i);
                        }
                        emitter.OnCompleted();
                    });
                }, true)
                .Test()
                ;

                Assert.True(to.ItemCount >= 500, "" + to.ItemCount);
                to.AssertCompleted()
                    .AssertNoError();

                Assert.True(sad.IsDisposed());
            }
        }
    }
}
