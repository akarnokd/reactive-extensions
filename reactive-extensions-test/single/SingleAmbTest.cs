using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleAmbTest
    {
        #region + Array +

        [Test]
        public void Array_Basic_Empty()
        {
            new ISingleSource<int>[]
            {

            }
            .AmbAll()
            .Test()
            .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Array_Basic_Single()
        {
            new []
            {
                SingleSource.Just(1)
            }
            .AmbAll()
            .Test()
            .AssertResult(1);
        }

        [Test]
        public void Array_First_Wins_Success()
        {
            var count = 0;

            var m = SingleSource.FromFunc<int>(() => ++count);

            new[]
            {
                SingleSource.Just(0),
                m
            }
            .AmbAll()
            .Test()
            .AssertResult(0);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_First_Wins_Error()
        {
            var count = 0;

            var m = SingleSource.FromFunc<int>(() => ++count);

            SingleSource.Amb(
                SingleSource.Error<int>(new InvalidOperationException()),
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

            var m = SingleSource.FromFunc<int>(() => ++count);

            new[]
            {
                SingleSource.Never<int>(),
                m
            }
            .AmbAll()
            .Test()
            .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Second_Wins_Error()
        {
            var count = 0;

            var m = SingleSource.FromFunc<int>(() => {
                count++;
                throw new InvalidOperationException();
            });

            SingleSource.Amb(
                SingleSource.Never<int>(),
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
                var ms1 = new SingleSubject<int>();
                var ms2 = new SingleSubject<int>();
                var ms3 = new SingleSubject<int>();

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
        public void Array_Error_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new SingleSubject<int>();
                var ms2 = new SingleSubject<int>();
                var ms3 = new SingleSubject<int>();

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
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();
            var ms3 = new SingleSubject<int>();

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
            var src = SingleSource.Never<int>();

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
            new List<ISingleSource<int>>()
            {
            }
            .Amb()
            .Test()
            .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Enumerable_Basic_Single()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1)
            }
            .Amb()
            .Test()
            .AssertResult(1);
        }

        [Test]
        public void Enumerable_First_Wins_Success()
        {
            var count = 0;

            var m = SingleSource.FromFunc<int>(() => ++count);

            new List<ISingleSource<int>>()
            {
                SingleSource.Just(0),
                m
            }
            .Amb()
            .Test()
            .AssertResult(0);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_First_Wins_Error()
        {
            var count = 0;

            var m = SingleSource.FromFunc<int>(() => ++count);

            SingleSource.Amb(
                new List<ISingleSource<int>>() {
                    SingleSource.Error<int>(new InvalidOperationException()),
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

            var m = SingleSource.FromFunc<int>(() => ++count);

            new List<ISingleSource<int>>()
            {
                SingleSource.Never<int>(),
                m
            }
            .Amb()
            .Test()
            .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Second_Wins_Error()
        {
            var count = 0;

            var m = SingleSource.FromFunc<int>(() => {
                count++;
                throw new InvalidOperationException();
            });

            SingleSource.Amb(
                new List<ISingleSource<int>>()
                {
                    SingleSource.Never<int>(),
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
                var ms1 = new SingleSubject<int>();
                var ms2 = new SingleSubject<int>();
                var ms3 = new SingleSubject<int>();

                var srcs = new List<SingleSubject<int>>()
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
        public void Enumerable_Error_Dispose_Others()
        {
            for (int i = 0; i < 3; i++)
            {
                var ms1 = new SingleSubject<int>();
                var ms2 = new SingleSubject<int>();
                var ms3 = new SingleSubject<int>();

                var srcs = new List<SingleSubject<int>>()
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
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();
            var ms3 = new SingleSubject<int>();

            var srcs = new List<SingleSubject<int>>()
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
            var src = SingleSource.Never<int>();

            new List<ISingleSource<int>>()
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
            SingleSource.Amb(new FailingEnumerable<ISingleSource<int>>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash()
        {
            SingleSource.Amb(new FailingEnumerable<ISingleSource<int>>(false, true, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + Enumerable +
    }
}
