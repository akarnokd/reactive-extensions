using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class SerializedSubjectTest
    {
        #region + Identity +

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

        [Test]
        public void Reentrance()
        {
            var serialized = new Subject<IEnumerable<int>>()
                .ToSerialized();

            var to = serialized
                .SelectMany(items =>
                {
                    bool empty = true;
                    foreach (var item in items)
                    {
                        if (item == 0)
                        {
                            empty = true;
                            break;
                        }
                        empty = false;
                        serialized.OnNext(new List<int>() { item - 1 });
                    }
                    if (empty)
                    {
                        serialized.OnCompleted();
                    }
                    return items;
                })
                .Test();

            serialized.OnNext(new List<int>() { 10 });

            to
                .AssertResult(10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
        }

        #endregion + Identity +

        #region + Transforming +

        sealed class TransformSubject : ISubject<int, string>
        {
            IObserver<string> downstream;

            public void OnCompleted()
            {
                downstream?.OnCompleted();
                downstream = null;
            }

            public void OnError(Exception error)
            {
                downstream?.OnError(error);
                downstream = null;
            }

            public void OnNext(int value)
            {
                downstream?.OnNext($"{value}");
            }

            public IDisposable Subscribe(IObserver<string> observer)
            {
                this.downstream = observer;
                return Disposable.Create(() => downstream = null);
            }
        }

        [Test]
        public void Basic_TR()
        {
            var subj = new TransformSubject();

            var serialized = subj.ToSerialized();

            var to = serialized.Test();

            serialized.EmitAll(1, 2, 3, 4, 5);

            to.AssertResult("1", "2", "3", "4", "5");
        }

        [Test]
        public void Error_TR()
        {
            var subj = new TransformSubject();

            var serialized = subj.ToSerialized();

            var to = serialized.Test();

            serialized.EmitError(new InvalidOperationException(), 1, 2, 3, 4, 5);

            to.AssertFailure(typeof(InvalidOperationException), "1", "2", "3", "4", "5");
        }

        #endregion + Transforming +
    }
}
