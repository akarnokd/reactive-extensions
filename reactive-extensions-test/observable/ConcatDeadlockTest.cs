using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class ConcatDeadlockTest
    {
        [Test]
        public void TestMethod1_Loop()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                TestMethod1();
            }
        }

        [Test]
        public void TestMethod1()
        {
            CountdownEvent printed = null;
            var nextInnerQuery = new Subject<Unit>();
            IEnumerable<object> e = null;
            Action<string> afterSelectManyButBeforeConcat = _ => { };

            (from _ in nextInnerQuery
             select (from __ in Observable.Return(Unit.Default)
                     from completeSynchronously in e
                     from x in SyncOrAsync(completeSynchronously as string, completeSynchronously as IObservable<string>)
                     select x)
                     .Do(x => afterSelectManyButBeforeConcat(x)))
            .ConcatMany()
            .Subscribe(x =>
            {
                Console.WriteLine(x);
                printed.Signal();
            });


            Console.WriteLine("No deadlock...");

            printed = new CountdownEvent(2);
            e = new[] { "Sync 1", "Sync 2" };
            nextInnerQuery.OnNext(Unit.Default);

            printed.Wait();


            Console.WriteLine("Also no deadlock...");

            printed = new CountdownEvent(2);
            var first = new Subject<string>();
            var second = new Subject<string>();
            e = new[] { first, second };
            nextInnerQuery.OnNext(Unit.Default);
            first.OnNext("Async 1");
            second.OnNext("Async 2");

            printed.Wait();


            Console.WriteLine("DEADLOCK");

            printed = new CountdownEvent(2);
            var waitForAsyncOnThreadB = new ManualResetEventSlim();
            var asyncValue = new Subject<string>();

            // Thread B
            afterSelectManyButBeforeConcat = _ => waitForAsyncOnThreadB.Set();

            e = AsyncThenSync(
              asyncValue,
              "Sync 2",
              () =>
              {
            // Thread A
            asyncValue.OnNext("Async 1");
            // Thread B: SelectMany.OnNext acquires a lock
            //           (waitForAsyncOnThreadB is set)
            //           Concat blocks awaiting the lock acquired initially by Thread A.
            //           Order of locks: SelectMany -> Concat (DEADLOCK)

            // Thread A
            waitForAsyncOnThreadB.Wait();
              });

            // Thread A
            nextInnerQuery.OnNext(Unit.Default);   // Thread A: Concat (technically, Merge(1)) acquires and holds a lock while iterating 'e'

            // This line is never reached, and nothing is ever printed.
            printed.Wait();
        }

        private static IEnumerable<object> AsyncThenSync(IObservable<string> asyncValue, string syncValue, Action waitForAsyncOnThreadB)
        {
            // Thread A
            yield return asyncValue;

            waitForAsyncOnThreadB();

            // Thread A
            yield return syncValue;
            // Thread A: SelectMany.OnNext blocks awaiting the lock acquired previously by Thread B.
            //           Order of locks: Concat -> SelectMany (DEADLOCK)
        }

        private async Task<string> SyncOrAsync(string completeSynchronously, IObservable<string> onCompleted)
             => completeSynchronously ?? await onCompleted.Take(1);
    }
}
