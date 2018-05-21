using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeConcatTest
    {
        #region + Array +

        [Test]
        public void Array_Empty()
        {
            new IMaybeSource<int>[0]
            .ConcatAll()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Empty_DelayError()
        {
            new IMaybeSource<int>[0]
            .ConcatAll(true)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Basic()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2)
            }
            .ConcatAll()
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Basic_DelayError()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2)
            }
            .ConcatAll(true)
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Basic_Params()
        {
            MaybeSource.Concat(
                MaybeSource.Just(1),
                MaybeSource.Just(2)
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Basic_Params_DelayError()
        {
            MaybeSource.Concat(true,
                MaybeSource.Just(1),
                MaybeSource.Just(2)
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Mixed()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3)
            }
            .ConcatAll()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Array_Error_Last()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Error<int>(new InvalidOperationException())
            }
            .ConcatAll()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Array_Error_Middle()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new[]
            {
                src,
                MaybeSource.Empty<int>(),
                src,
                MaybeSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .ConcatAll()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Array_Error_Last_Delay()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Error<int>(new InvalidOperationException())
            }
            .ConcatAll(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Array_Error_Middle_Delay()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new[]
            {
                src,
                MaybeSource.Empty<int>(),
                src,
                MaybeSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .ConcatAll(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4);
        }

        [Test]
        public void Array_Null_Entry()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new[]
            {
                src,
                MaybeSource.Empty<int>(),
                src,
                null,
                src,
                src
            }
            .ConcatAll()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1, 2);
        }

        #endregion + Array +

        #region + Enumerable +

        [Test]
        public void Enumerable_Empty()
        {
            new List<IMaybeSource<int>>()
            .Concat()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Empty_DelayError()
        {
            new List<IMaybeSource<int>>()
            .Concat(true)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Basic()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2)
            }
            .Concat()
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Basic_DelayError()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2)
            }
            .Concat(true)
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Basic_Params()
        {
            MaybeSource.Concat(
                new List<IMaybeSource<int>>() {
                    MaybeSource.Just(1),
                    MaybeSource.Just(2)
                }
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Basic_Params_DelayError()
        {
            MaybeSource.Concat(
                new List<IMaybeSource<int>>() {
                    MaybeSource.Just(1),
                    MaybeSource.Just(2)
                }, true
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Mixed()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3)
            }
            .Concat()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Enumerable_Error_Last()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Error<int>(new InvalidOperationException())
            }
            .Concat()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Enumerable_Error_Middle()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Empty<int>(),
                src,
                MaybeSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .Concat()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Enumerable_Error_Last_Delay()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Error<int>(new InvalidOperationException())
            }
            .Concat(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Enumerable_Error_Middle_Delay()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Empty<int>(),
                src,
                MaybeSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .Concat(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4);
        }

        [Test]
        public void Enumerable_Null_Entry()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Empty<int>(),
                src,
                null,
                src,
                src
            }
            .Concat()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1, 2);
        }

        [Test]
        public void Enumerable_GetEnumerator_Crash()
        {
            MaybeSource.Concat(new FailingEnumerable<IMaybeSource<int>>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash()
        {
            MaybeSource.Concat(new FailingEnumerable<IMaybeSource<int>>(false, true, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + Enumerable +
    }
}
