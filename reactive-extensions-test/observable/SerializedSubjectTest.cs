using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class SerializedSubjectTest
    {
        [Test]
        public void Basic()
        {
            var us = new UnicastSubject<int>().ToSerialized();

            var to = us.Test();

            us.EmitAll(1, 2, 3, 4, 5);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            var us = new UnicastSubject<int>().ToSerialized();

            var to = us.Test();

            us.EmitError(new InvalidOperationException(), 1, 2, 3, 4, 5);

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void OnNext_Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>().ToSerialized();

                var to = us.Test();

                Action emit = () => {
                    for (int j = 0; j < 500; j++)
                    {
                        us.OnNext(j);
                    }
                };

                TestHelper.Race(emit, emit);

                to.AssertValueCount(1000);
            }
        }
    }
}
