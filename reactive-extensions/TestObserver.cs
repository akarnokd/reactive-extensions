using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

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

        int once;

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

        /// <summary>
        /// Called when the upstream completes normally.
        /// </summary>
        public virtual void OnCompleted()
        {
            Volatile.Write(ref completions, completions + 1);
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                cdl.Signal();
            }
        }

        /// <summary>
        /// Called when the upstream completes with an exception.
        /// </summary>
        /// <param name="error">The terminal exception.</param>
        public virtual void OnError(Exception error)
        {
            errors.Add(error ?? new NullReferenceException("OnError(null)"));
            Volatile.Write(ref errorCount, errors.Count);
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                cdl.Signal();
            }
        }

        /// <summary>
        /// Called when a new item is available for consumption.
        /// </summary>
        /// <param name="value">The new item available.</param>
        public virtual void OnNext(T value)
        {
            items.Add(value);
            Volatile.Write(ref itemCount, items.Count);
            if (Volatile.Read(ref once) != 0)
            {
                OnError(new InvalidOperationException("OnNext called after termination"));
            }
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

        /// <summary>
        /// Returns true if this TestObserver has been explicitly
        /// disposed via <see cref="Dispose"/> method.
        /// </summary>
        /// <returns>True if this TestObserver has been explicitly disposed.</returns>
        public bool IsDisposed()
        {
            return DisposableHelper.IsDisposed(ref upstream);
        }

        /// <summary>
        /// Wait for the upstream to terminate within the specified timespan,
        /// or else dispose the sequence and mark this TestObserver as timed-out.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the terminal signal.</param>
        /// <returns>this</returns>
        public TestObserver<T> AwaitDone(TimeSpan timeout)
        {
            if (!cdl.Wait(timeout))
            {
                this.timeout = true;
                Dispose();
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

        string ToStr(object item)
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
                    if (!Equals(exp.Current, act.Current))
                    {
                        throw Fail("Item @ " + index + "/" + j + " differ. Expected: " + ToStr(exp.Current) + ", Actual: " + ToStr(act.Current));
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
                        throw Fail("Item @ " + i + " differ. Expected: " + ToStr(expect) + ", Actual: " + ToStr(actual));
                    }
                }
                else
                {
                    if (!EqualityComparer<T>.Default.Equals(actual, expect))
                    {
                        throw Fail("Item @ " + i + " differ. Expected: " + ToStr(expect) + ", Actual: " + ToStr(actual));
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Assert that the upstream has completed: it called <see cref="OnCompleted"/>
        /// exactly once and didn't call <see cref="OnError(Exception)"/> at all.
        /// </summary>
        /// <returns>this</returns>
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

        /// <summary>
        /// Assert that the upstream has not yet completed normally
        /// (but it could have failed, which is not asserted here).
        /// </summary>
        /// <returns>this</returns>
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

        /// <summary>
        /// Assert that the upstream has not yet terminated with an exception
        /// (but it could have completed normally, which is not asserted here).
        /// </summary>
        /// <returns>this</returns>
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

        /// <summary>
        /// Assert that the upstream terminated with a (subtype) of the
        /// specified exception type and via exaclty one <see cref="OnError(Exception)"/>
        /// call.
        /// </summary>
        /// <param name="expectedType">The expected exception type, 
        /// such as <code>typeof(InvalidOperationException)</code> for example.</param>
        /// <param name="message">The expected message (or part of it if <paramref name="messageContains"/> is true).</param>
        /// <param name="messageContains">If true and <paramref name="message"/> is not null, the error messages are compared as well.</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertError(Type expectedType, string message = null, bool messageContains = false)
        {
            var c = Volatile.Read(ref errorCount);
            if (c == 0)
            {
                throw Fail("No error.");
            }
            var members = expectedType.GetTypeInfo();
            var found = 0;
            var foundRaw = 0;
            for (int i = 0; i < c; i++)
            {
                if (members.IsAssignableFrom(errors[i].GetType().GetTypeInfo()))
                {
                    if (message != null)
                    {
                        if (messageContains)
                        {
                            if (errors[i].Message.Contains(message))
                            {
                                found++;
                            }
                        }
                        else
                        {
                            if (errors[i].Message.Equals(message))
                            {
                                found++;
                            }
                        }
                    } else {
                        found++;
                    }
                    foundRaw++;
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
                if (foundRaw != 0)
                {
                    throw Fail("Exception type present but not with the specified message" + (messageContains ? " part:" : ":") + message);
                }
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
            AssertValueCount(0);
            AssertNoError();
            AssertNotCompleted();
            return this;
        }

        /// <summary>
        /// Assert that the TestObserver received the specified number of items.
        /// </summary>
        /// <param name="expected">The expected number of items</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertValueCount(int expected)
        {
            var c = Volatile.Read(ref itemCount);

            if (c != expected)
            {
                throw Fail("Number of items differ. Expected: " + expected + ", Actual: " + c);
            }

            return this;
        }

        /// <summary>
        /// Returns the number of items received by the TestObserver.
        /// </summary>
        public int ItemCount { get { return Volatile.Read(ref itemCount); } }

        /// <summary>
        /// Returns the number of errors received by the TestObserver.
        /// </summary>
        /// <remarks>Since 0.0.3</remarks>
        public int ErrorCount { get { return Volatile.Read(ref errorCount); } }

        /// <summary>
        /// Assert that the TestObserver is not terminated and contains only the
        /// specified items.
        /// </summary>
        /// <param name="expected">The expected items.</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertValuesOnly(params T[] expected)
        {
            AssertValues(expected);
            AssertNoError();
            AssertNotCompleted();
            return this;
        }

        /// <summary>
        /// The list of received items so far. Use
        /// <see cref="ItemCount"/> for the number of
        /// items to be read safely.
        /// </summary>
        /// <remarks>Since 0.0.3</remarks>
        public List<T> Items { get { return items; } }

        /// <summary>
        /// The list of received errors so far. Use
        /// <see cref="ErrorCount"/> for the number of
        /// items to be read safely.
        /// </summary>
        /// <remarks>Since 0.0.3</remarks>
        public List<Exception> Errors { get { return errors; } }

        /// <summary>
        /// Assert that there is exactly one <see cref="AggregateException"/>
        /// and its <paramref name="index"/>th error is assignable to the
        /// <paramref name="errorType"/>.
        /// </summary>
        /// <param name="index">The index of the error inside the composite.</param>
        /// <param name="errorType">The expected error type</param>
        /// <param name="message">The expected message (or part of it if <paramref name="messageContains"/> is true).</param>
        /// <param name="messageContains">If true and <paramref name="message"/> is not null, the error messages are compared as well.</param>
        /// <returns>this</returns>
        /// <remarks>Since 0.0.3</remarks>
        public TestObserver<T> AssertCompositeError(int index, Type errorType, string message = null, bool messageContains = false)
        {
            AssertError(typeof(AggregateException));
            var exs = (errors[0] as AggregateException).InnerExceptions;
            if (exs.Count <= index)
            {
                throw Fail("The AggregateException index out of bounds. Expected: " + index + ", Actual: " + exs.Count);
            }
            if (!errorType.GetTypeInfo().IsAssignableFrom(exs[index].GetType().GetTypeInfo()))
            {
                if (message != null)
                {
                    if (messageContains)
                    {
                        if (!exs[index].Message.Contains(message))
                        {
                            throw Fail("Error found with a different message part. Expected: " + message + ", Actual: " + exs[index].Message);
                        }
                    }
                    else
                    {
                        if (!exs[index].Message.Equals(message))
                        {
                            throw Fail("Error found with a different message. Expected: " + message + ", Actual: " + exs[index].Message);
                        }
                    }
                }
                throw Fail("Wrong error type @ " + index + ". Expected: " + errorType + ", Actual: " + exs[index].GetType());
            }
            return this;
        }
    }
}
