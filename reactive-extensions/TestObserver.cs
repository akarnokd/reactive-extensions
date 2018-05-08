using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Observer that stores the events it receives and allows
    /// asserting its state in an imperative manner.
    /// </summary>
    /// <typeparam name="T">The value consumed.</typeparam>
    public class TestObserver<T> : IObserver<T>, IDisposable
    {
        readonly CountdownEvent cdl;

        readonly List<T> items;

        int itemCount;

        readonly List<Exception> errors;

        int errorCount;

        int completions;

        IDisposable upstream;

        bool timeout;

        public TestObserver()
        {
            this.items = new List<T>();
            this.errors = new List<Exception>();
            this.cdl = new CountdownEvent(1);
        }

        public virtual void OnSubscribe(IDisposable d)
        {
            if (Interlocked.CompareExchange(ref this.upstream, d, null) != null)
            {
                d?.Dispose();
            }
        }

        public virtual void OnCompleted()
        {
            Volatile.Write(ref completions, completions + 1);
            cdl.Signal();
        }

        public virtual void OnError(Exception error)
        {
            errors.Add(error);
            Volatile.Write(ref errorCount, errors.Count);
            cdl.Signal();
        }

        public virtual void OnNext(T value)
        {
            items.Add(value);
            Volatile.Write(ref itemCount, errors.Count);
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public bool IsDisposed()
        {
            return DisposableHelper.IsDisposed(ref upstream);
        }

        public TestObserver<T> AwaitDone(TimeSpan timeout)
        {
            if (!cdl.Wait(timeout))
            {
                this.timeout = true;
            }
            return this;
        }

        /// <summary>
        /// Compose a failure Exception with all relevant state information
        /// and any Exception(s) received.
        /// </summary>
        /// <param name="message">The message to use as perfix to the state</param>
        /// <returns>The exception to be thrown.</returns>
        Exception Fail(String message)
        {
            var count = Volatile.Read(ref itemCount);
            var err = Volatile.Read(ref errorCount);
            var compl = Volatile.Read(ref completions);
            var timeout = this.timeout;
            var disposed = DisposableHelper.IsDisposed(ref upstream);

            var errList = new List<Exception>();
            for (int i = 0; i < err; i++)
            {
                errList.Add(errors[i]);
            }

            var msg = message
                + " ("
                + "items=" + count
                + ", errors=" + err
                + ", completions=" + compl;

            if (timeout)
            {
                msg += ", timeout!";
            }

            if (disposed)
            {
                msg += ", disposed!";
            }

            if (err > 1)
            {
                return new Exception(msg, new AggregateException(errList));
            }
            if (err == 1)
            {
                return new Exception(msg, errList[0]);
            }
            return new Exception(msg);
        }

        string toStr(T item)
        {
            return "" + item;
        }

        public TestObserver<T> AssertValues(params T[] expected)
        {
            var c = Volatile.Read(ref itemCount);

            if (c != expected.Length)
            {
                throw Fail("Number of items differ. Expected: " + expected.Length);
            }

            for (int i = 0; i < c; i++)
            {
                var actual = items[i];
                var expect = expected[i];

                if (!EqualityComparer<T>.Default.Equals(actual, expect))
                {
                    throw Fail("Items @ " + i + " differ. Expected: " + toStr(expect) + ", Actual: " + toStr(actual));
                }
            }

            return this;
        }

        public TestObserver<T> AssertCompleted()
        {
            var c = Volatile.Read(ref completions);
            if (c == 0)
            {
                throw Fail("Not completed.");
            }
            if (c > 1)
            {
                throw Fail("Multiple completions.");
            }
            return this;
        }

        public TestObserver<T> AssertNotCompleted()
        {
            var c = Volatile.Read(ref completions);
            if (c == 1)
            {
                throw Fail("Unexpected completion.");
            }
            if (c > 1)
            {
                throw Fail("Multiple completions.");
            }
            return this;
        }

        public TestObserver<T> AssertNoError()
        {
            var c = Volatile.Read(ref errorCount);
            if (c == 1)
            {
                throw Fail("Unexpected error.");
            }
            if (c > 1)
            {
                throw Fail("Multiple errors.");
            }
            return this;
        }

        public TestObserver<T> AssertError(Type expectedType)
        {
            var c = Volatile.Read(ref errorCount);
            if (c == 0)
            {
                throw Fail("No error.");
            }
            var members = expectedType.GetTypeInfo();
            var found = 0;
            for (int i = 0; i < c; i++)
            {
                if (members.IsAssignableFrom(errors[i].GetType().GetTypeInfo()))
                {
                    found++;
                }
            }

            if (found == 1 && c > 1)
            {
                throw Fail("Exception present but others as well");
            }

            if (found > 1)
            {
                throw Fail("The Exception appears multiple times");
            }

            if (found == 0)
            {
                throw Fail("Exception not present");
            }

            return this;
        }

        public TestObserver<T> AssertResult(params T[] values)
        {
            AssertValues(values);
            AssertNoError();
            AssertCompleted();
            return this;
        }
    }
}
