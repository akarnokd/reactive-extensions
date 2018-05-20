﻿using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeFilterTest
    {
        [Test]
        public void Basic()
        {
            MaybeSource.Just(1)
                .Filter(v => true)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Basic_Filtered()
        {
            MaybeSource.Just(1)
                .Filter(v => false)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Filter(v => true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Empty()
        {
            MaybeSource.Empty<int>()
                .Filter(v => true)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Predicate_Crash()
        {
            MaybeSource.Just(1)
                .Filter(v =>
                {
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.Filter(v => true));
        }
    }
}
