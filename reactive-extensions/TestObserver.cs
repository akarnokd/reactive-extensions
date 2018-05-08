using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Linq;

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

        /// <summary>
        /// Constructs an empty TestObserver.
        /// </summary>
        public TestObserver()
        {
            this.items = new List<T>();
            this.errors = new List<Exception>();
            this.cdl = new CountdownEvent(1);
        }

        /// <summary>
        /// Called at most once with the upstream's IDisposable instance.
        /// Further calls will dispose the IDisposable provided.
        /// </summary>
        /// <param name="d">The upstream's IDisposable instance.</param>
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
            errors.Add(error ?? new NullReferenceException("The OnError(null)"));
            Volatile.Write(ref errorCount, errors.Count);
            cdl.Signal();
        }

        public virtual void OnNext(T value)
        {
            items.Add(value);
            Volatile.Write(ref itemCount, items.Count);
        }

        /// <summary>
        /// Disposes the connection to the upstream if any or
        /// makes sure the upstream is immediately disposed upon
        /// subscription.
        /// </summary>
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

            msg += ")";

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

        string toStr(object item)
        {
            string result = "";
            string typestr = item != null ? item.GetType().Name : typeof(T).Name;
            if (item is IEnumerable en)
            {
                result += string.Join(", ", en.Cast<object>());
            }
            else
            {
                result += "" + item;
            }
            return result + " (" + typestr + ")";
        }

        void compareEnums(IEnumerable expected, IEnumerable actual, int index)
        {
            IEnumerator exp = expected.GetEnumerator();
            IEnumerator act = actual.GetEnumerator();

            int j = 0;
            for (; ; )
            {
                bool expNext = exp.MoveNext();
                bool actNext = act.MoveNext();

                if (!expNext && !actNext)
                {
                    break;
                }
                else
                if (expNext && actNext)
                {
                    if (!object.Equals(exp.Current, act.Current))
                    {
                        throw Fail("Item @ " + index + "/" + j + " differ. Expected: " + toStr(exp.Current) + ", Actual: " + toStr(act.Current));
                    }
                }
                else
                if (expNext && !actNext)
                {
                    throw Fail("Item @ " + index + ", more items expected from the IEnumerable");
                }
                else
                {
                    throw Fail("Item @ " + index + ", less items expected from the IEnumerable");
                }

                j++;
            }
        }

        /// <summary>
        /// Assert that this TestObserver received only the expected items.
        /// </summary>
        /// <param name="expected">The array of expected items.</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertValues(params T[] expected)
        {
            var c = Volatile.Read(ref itemCount);

            if (c != expected.Length)
            {
                throw Fail("Number of items differ. Expected: " + expected.Length + ", Actual: " + c);
            }

            for (int i = 0; i < c; i++)
            {
                var actual = items[i];
                var expect = expected[i];

                if (expect is IEnumerable en)
                {
                    if (actual is IEnumerable an)
                    {
                        compareEnums(en, an, i);
                    }
                    else
                    {
                        throw Fail("Item @ " + i + " differ. Expected: " + toStr(expect) + ", Actual: " + toStr(actual));
                    }
                }
                if (!EqualityComparer<T>.Default.Equals(actual, expect))
                {
                    throw Fail("Item @ " + i + " differ. Expected: " + toStr(expect) + ", Actual: " + toStr(actual));
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

        /// <summary>
        /// Assert that this TestObserver received the given <paramref name="expected"/> only
        /// and in the given order, followed by no errors and a normal completion.
        /// </summary>
        /// <param name="expected">The expected values that has been received.</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertResult(params T[] expected)
        {
            AssertValues(expected);
            AssertNoError();
            AssertCompleted();
            return this;
        }

        /// <summary>
        /// Assert that this TestObserver received the given <paramref name="expected"/> only
        /// and in the given order, followed by an error of the specified type.
        /// </summary>
        /// <param name="expected">The expected values that has been received.</param>
        /// <param name="expectedError">The expected error tyoe</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertFailure(Type expectedError, params T[] expected)
        {
            AssertValues(expected);
            AssertNotCompleted();
            AssertError(expectedError);
            return this;
        }

        /// <summary>
        /// Assert that there were no events signalled to this TestObserver.
        /// </summary>
        /// <returns>this</returns>
        public TestObserver<T> AssertEmpty()
        {
            AssertValues();
            AssertNoError();
            AssertNotCompleted();
            return this;
        }
    }
}
