using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class ValidationHelperTest
    {
        [Test]
        public void RequireNonNull()
        {
            try
            {
                ValidationHelper.RequireNonNull((string)null, "string");
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {

            }
        }

        [Test]
        public void RequireNonNullRef()
        {
            try
            {
                ValidationHelper.RequireNonNullRef((string)null, "string");
                Assert.Fail();
            }
            catch (NullReferenceException)
            {

            }
        }

        [Test]
        public void RequirePositive()
        {
            try
            {
                ValidationHelper.RequirePositive(-1, "maxConcurrency");
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }

        [Test]
        public void RequirePositiveLong()
        {
            try
            {
                ValidationHelper.RequirePositive(-1L, "maxConcurrency");
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }

        [Test]
        public void RequireNonNegative()
        {
            try
            {
                ValidationHelper.RequireNonNegative(-1, "index");
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }

        [Test]
        public void RequireNonNegativeLong()
        {
            try
            {
                ValidationHelper.RequireNonNegative(-1L, "index");
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }
    }
}
