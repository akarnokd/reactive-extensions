using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableAmbTest
    {
        [Test]
        public void Basic_Empty()
        {
            CompletableSource.Amb()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Basic_One_Source()
        {
            CompletableSource.Amb(CompletableSource.Empty())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Basic_One_Source_Error()
        {
            CompletableSource.Amb(CompletableSource.Error(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Basic_First_Wins()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Amb(cs1, cs2).Test();

            to.AssertEmpty();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnCompleted();

            Assert.False(cs2.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Basic_First_Wins_Error()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Amb(cs1, cs2).Test();

            to.AssertEmpty();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Basic_Second_Wins()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Amb(cs1, cs2).Test();

            to.AssertEmpty();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnCompleted();

            Assert.False(cs1.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Basic_Second_Wins_Error()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Amb(cs1, cs2).Test();

            to.AssertEmpty();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Empty()
        {
            CompletableSource.Amb(new List<ICompletableSource>() {
                CompletableSource.Never(),
                CompletableSource.Never(),
                CompletableSource.Never(),
                CompletableSource.Empty()
            })
                .Test()
                .AssertResult();
        }

        [Test]
        public void Enumerable_Many()
        {
            var list = new List<ICompletableSource>();
            for (int i = 0; i < 32; i++)
            {
                list.Add(CompletableSource.Never());
            }
            list.Add(CompletableSource.Error(new InvalidOperationException()));

            CompletableSource.Amb(list)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs1 = new CompletableSubject();
                var cs2 = new CompletableSubject();

                var to = CompletableSource.Amb(cs1, cs2).Test();

                to.AssertEmpty();

                TestHelper.Race(() => {
                    cs1.OnCompleted();
                }, () => {
                    cs2.OnCompleted();
                });

                Assert.False(cs1.HasObserver());
                Assert.False(cs2.HasObserver());

                to.AssertResult();
            }
        }
    }
}
