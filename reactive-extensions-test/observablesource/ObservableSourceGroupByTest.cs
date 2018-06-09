using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceGroupByTest
    {
        [Test]
        public void Basic()
        {
            var to = ObservableSource.Range(1, 10)
                .GroupBy(v => v % 2)
                .Test();

            to.AssertSubscribed()
                .AssertValueCount(2)
                .AssertCompleted()
                .AssertNoError();

            to.Items[0].Test().AssertResult(1, 3, 5, 7, 9);
            to.Items[1].Test().AssertResult(2, 4, 6, 8, 10);
        }

        [Test]
        public void Basic_ValueSelector()
        {
            var to = ObservableSource.Range(1, 10)
                .GroupBy(v => v % 2, v => v + 100)
                .Test();

            to.AssertSubscribed()
                .AssertValueCount(2)
                .AssertCompleted()
                .AssertNoError();

            to.Items[0].Test().AssertResult(101, 103, 105, 107, 109);
            to.Items[1].Test().AssertResult(102, 104, 106, 108, 110);
        }

        [Test]
        public void Error()
        {
            var to = ObservableSource.Range(1, 10).ConcatError(new InvalidOperationException())
                .GroupBy(v => v % 2)
                .Test();

            to.AssertSubscribed()
                .AssertValueCount(2)
                .AssertNotCompleted()
                .AssertError(typeof(InvalidOperationException));

            to.Items[0].Test().AssertFailure(typeof(InvalidOperationException), 1, 3, 5, 7, 9);
            to.Items[1].Test().AssertFailure(typeof(InvalidOperationException), 2, 4, 6, 8, 10);
        }

        [Test]
        public void Basic_KeySelectorCrash()
        {
            var to = ObservableSource.Range(1, 10)
                .GroupBy(v => {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return v % 2;
                }, v => v + 100)
                .Test();

            to.AssertError(typeof(InvalidOperationException))
                .AssertValueCount(2)
                .AssertNotCompleted();

            to.Items[0].Test().AssertFailure(typeof(InvalidOperationException), 101);
            to.Items[1].Test().AssertFailure(typeof(InvalidOperationException), 102);
        }

        [Test]
        public void Basic_ValueSelectorCrash()
        {
            var to = ObservableSource.Range(1, 10)
                .GroupBy(v => v % 2, v => 
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }

                    return v + 100;
                })
                .Test();

            to.AssertError(typeof(InvalidOperationException))
                .AssertValueCount(2)
                .AssertNotCompleted();

            to.Items[0].Test().AssertFailure(typeof(InvalidOperationException), 101);
            to.Items[1].Test().AssertFailure(typeof(InvalidOperationException), 102);
        }
        [Test]
        public void Dispose_NoGroups()
        {
            TestHelper.VerifyDisposeObservableSource<int, IObservableSource<int>>(o => o.GroupBy(v => v, v => v));
        }

        [Test]
        public void Take_One_Group()
        {
            var subj = new PublishSubject<int>();

            var group = new List<IGroupedObservableSource<int, int>>();

            var d = subj.GroupBy(v => 1).Subscribe(v => group.Add(v));

            Assert.True(subj.HasObservers);

            Assert.AreEqual(0, group.Count);

            subj.OnNext(1);

            Assert.AreEqual(1, group.Count);

            d.Dispose();

            Assert.True(subj.HasObservers);

            subj.OnNext(2);

            Assert.AreEqual(1, group.Count);

            var to = group[0].Test();

            to.AssertValuesOnly(1, 2);

            to.Dispose();

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Group_Take()
        {
            ObservableSource.Range(1, 5)
                .GroupBy(k => k)
                .Map(g => g.Take(1))
                .Merge()
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
