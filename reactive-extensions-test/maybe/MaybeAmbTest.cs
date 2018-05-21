using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeAmbTest
    {
        #region + Array +

        [Test]
        public void Array_Basic_Empty()
        {
            new IMaybeSource<int>[]
            {

            }
            .AmbAll()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Basic_Single()
        {
            new []
            {
                MaybeSource.Just(1)
            }
            .AmbAll()
            .Test()
            .AssertResult(1);
        }

        [Test]
        public void Array_First_Wins_Success()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            new[]
            {
                MaybeSource.Just(0),
                m
            }
            .AmbAll()
            .Test()
            .AssertResult(0);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_First_Wins_Empty()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            new[]
            {
                MaybeSource.Empty<int>(),
                m
            }
            .AmbAll()
            .Test()
            .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_First_Wins_Error()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            MaybeSource.Amb(
                MaybeSource.Error<int>(new InvalidOperationException()),
                m
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_Second_Wins_Success()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            new[]
            {
                MaybeSource.Never<int>(),
                m
            }
            .AmbAll()
            .Test()
            .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Second_Wins_Empty()
        {
            var count = 0;

            var m = MaybeSource.FromAction<int>(() => ++count);

            new[]
            {
                MaybeSource.Never<int>(),
                m
            }
            .AmbAll()
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Second_Wins_Error()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => {
                count++;
                throw new InvalidOperationException();
            });

            MaybeSource.Amb(
                MaybeSource.Never<int>(),
                m
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Success_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();
                var ms3 = new MaybeSubject<int>();

                var srcs = new[]
                {
                    ms1,
                    ms2,
                    ms3
                };

                var to = srcs
                .AmbAll()
                .Test();

                to.AssertEmpty();

                srcs[i].OnSuccess(i);

                to.AssertResult(i);

                for (int j = 0; j < 3; j++)
                {
                    Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
                }
            }
        }

        [Test]
        public void Array_Complete_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();
                var ms3 = new MaybeSubject<int>();

                var srcs = new[]
                {
                    ms1,
                    ms2,
                    ms3
                };

                var to = srcs
                .AmbAll()
                .Test();

                to.AssertEmpty();

                srcs[i].OnCompleted(); ;

                to.AssertResult();

                for (int j = 0; j < 3; j++)
                {
                    Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
                }
            }
        }

        [Test]
        public void Array_Error_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();
                var ms3 = new MaybeSubject<int>();

                var srcs = new[]
                {
                    ms1,
                    ms2,
                    ms3
                };

                var to = srcs
                .AmbAll()
                .Test();

                to.AssertEmpty();

                srcs[i].OnError(new InvalidOperationException("" + i));

                to.AssertFailure(typeof(InvalidOperationException))
                    .AssertError(typeof(InvalidOperationException), "" + i);

                for (int j = 0; j < 3; j++)
                {
                    Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
                }
            }
        }

        [Test]
        public void Array_Dispose()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();
            var ms3 = new MaybeSubject<int>();

            var srcs = new[]
            {
                    ms1,
                    ms2,
                    ms3
                };

            var to = srcs
            .AmbAll()
            .Test();

            to.AssertSubscribed();

            to.Dispose();

            for (int j = 0; j < 3; j++)
            {
                Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
            }
        }


        [Test]
        public void Array_Null_Entry()
        {
            var src = MaybeSource.Never<int>();

            new[]
            {
                src,
                src,
                null,
                src,
                src
            }
            .Amb()
            .Test()
            .AssertFailure(typeof(NullReferenceException));
        }


        #endregion + Array +

        #region + Enumerable +

        [Test]
        public void Enumerable_Basic_Empty()
        {
            new List<IMaybeSource<int>>()
            {
            }
            .Amb()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Basic_Single()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1)
            }
            .Amb()
            .Test()
            .AssertResult(1);
        }

        [Test]
        public void Enumerable_First_Wins_Success()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(0),
                m
            }
            .Amb()
            .Test()
            .AssertResult(0);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_First_Wins_Empty()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Empty<int>(),
                m
            }
            .Amb()
            .Test()
            .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_First_Wins_Error()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            MaybeSource.Amb(
                new List<IMaybeSource<int>>() {
                    MaybeSource.Error<int>(new InvalidOperationException()),
                    m
                }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_Second_Wins_Success()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => ++count);

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Never<int>(),
                m
            }
            .Amb()
            .Test()
            .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Second_Wins_Empty()
        {
            var count = 0;

            var m = MaybeSource.FromAction<int>(() => ++count);

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Never<int>(),
                m
            }
            .Amb()
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Second_Wins_Error()
        {
            var count = 0;

            var m = MaybeSource.FromFunc<int>(() => {
                count++;
                throw new InvalidOperationException();
            });

            MaybeSource.Amb(
                new List<IMaybeSource<int>>()
                {
                    MaybeSource.Never<int>(),
                    m
                }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Success_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();
                var ms3 = new MaybeSubject<int>();

                var srcs = new List<MaybeSubject<int>>()
                {
                    ms1,
                    ms2,
                    ms3
                };

                var to = srcs
                .Amb()
                .Test();

                to.AssertEmpty();

                srcs[i].OnSuccess(i);

                to.AssertResult(i);

                for (int j = 0; j < 3; j++)
                {
                    Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
                }
            }
        }

        [Test]
        public void Enumerable_Complete_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();
                var ms3 = new MaybeSubject<int>();

                var srcs = new List<MaybeSubject<int>>()
                {
                    ms1,
                    ms2,
                    ms3
                };

                var to = srcs
                .Amb()
                .Test();

                to.AssertEmpty();

                srcs[i].OnCompleted(); ;

                to.AssertResult();

                for (int j = 0; j < 3; j++)
                {
                    Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
                }
            }
        }

        [Test]
        public void Enumerable_Error_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();
                var ms3 = new MaybeSubject<int>();

                var srcs = new List<MaybeSubject<int>>()
                {
                    ms1,
                    ms2,
                    ms3
                };

                var to = srcs
                .Amb()
                .Test();

                to.AssertEmpty();

                srcs[i].OnError(new InvalidOperationException("" + i));

                to.AssertFailure(typeof(InvalidOperationException))
                    .AssertError(typeof(InvalidOperationException), "" + i);

                for (int j = 0; j < 3; j++)
                {
                    Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
                }
            }
        }

        [Test]
        public void Enumerable_Dispose()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();
            var ms3 = new MaybeSubject<int>();

            var srcs = new List<MaybeSubject<int>>()
            {
                    ms1,
                    ms2,
                    ms3
                };

            var to = srcs
            .Amb()
            .Test();

            to.AssertSubscribed();

            to.Dispose();

            for (int j = 0; j < 3; j++)
            {
                Assert.False(srcs[j].HasObserver(), $"{j} still has observers");
            }
        }

        [Test]
        public void Enumerable_Null_Entry()
        {
            var src = MaybeSource.Never<int>();

            new List<IMaybeSource<int>>()
            {
                src,
                src,
                null,
                src,
                src
            }
            .Amb()
            .Test()
            .AssertFailure(typeof(NullReferenceException));
        }

        [Test]
        public void Enumerable_GetEnumerator_Crash()
        {
            MaybeSource.Amb(new FailingEnumerable<IMaybeSource<int>>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash()
        {
            MaybeSource.Amb(new FailingEnumerable<IMaybeSource<int>>(false, true, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + Enumerable +
    }
}
