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
        , IMaybeObserver<T>, ISingleObserver<T>, ICompletableObserver
        , ISignalObserver<T>
    {
        readonly CountdownEvent cdl;

        readonly bool requireOnSubscribe;

        readonly int fusionRequested;

        readonly List<T> items;

        int itemCount;

        readonly List<Exception> errors;

        int errorCount;

        int completions;

        IDisposable upstream;

        bool timeout;

        int once;

        string tag;

        int fusionEstablished;

        IFuseableDisposable<T> queue;

        /// <summary>
        /// Constructs an empty TestObserver.
        /// </summary>
        public TestObserver() : this(false)
        {
        }

        /// <summary>
        /// Constructs an empty TestObserver.
        /// </summary>
        /// <param name="requireOnSubscribe">Should the OnSubscribe be
        /// called before any of the other OnXXX methods?</param>
        /// <param name="fusionRequested">The fusion mode to request, <see cref="FusionSupport"/> constants.</param>
        public TestObserver(bool requireOnSubscribe, int fusionRequested = 0)
        {
            this.items = new List<T>();
            this.errors = new List<Exception>();
            this.cdl = new CountdownEvent(1);
            this.requireOnSubscribe = requireOnSubscribe;
            this.fusionRequested = fusionRequested;
            this.fusionEstablished = -1;
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
            else
            {
                if (fusionRequested > FusionSupport.None)
                {
                    if (d is IFuseableDisposable<T> fd)
                    {
                        queue = fd;
                        fusionEstablished = fd.RequestFusion(fusionRequested);

                        if (fusionEstablished == FusionSupport.Sync)
                        {
                            for (; ; )
                            {
                                if (IsDisposed())
                                {
                                    break;
                                }
                                var v = default(T);
                                var success = false;
                                try
                                {
                                    v = queue.TryPoll(out success);
                                }
                                catch (Exception ex)
                                {
                                    Dispose();
                                    OnError(ex);
                                    break;
                                }

                                if (!success)
                                {
                                    OnCompleted();
                                    break;
                                }

                                items.Add(v);
                                Volatile.Write(ref itemCount, items.Count);
                            }
                        }
                    }
                }
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
            if (fusionEstablished == FusionSupport.Sync)
            {
                OnError(new InvalidOperationException("OnNext called in Sync fusion mode!"));
            }
            else
            if (fusionEstablished == FusionSupport.Async)
            {
                for (; ;)
                {
                    if (IsDisposed())
                    {
                        break;
                    }

                    var v = default(T);
                    var success = false;
                    try
                    {
                        v = queue.TryPoll(out success);
                    }
                    catch (Exception ex)
                    {
                        Dispose();
                        OnError(ex);
                        break;
                    }

                    if (!success)
                    {
                        break;
                    }

                    items.Add(v);
                    Volatile.Write(ref itemCount, items.Count);
                }
            }
            else
            {
                items.Add(value);
                Volatile.Write(ref itemCount, items.Count);
                if (Volatile.Read(ref once) != 0)
                {
                    OnError(new InvalidOperationException("OnNext called after termination"));
                }
            }
        }

        /// <summary>
        /// Called when the upstream succeeded.
        /// </summary>
        /// <param name="value">The new item available.</param>
        public virtual void OnSuccess(T value)
        {
            items.Add(value);
            Volatile.Write(ref itemCount, items.Count);
            if (Volatile.Read(ref once) != 0)
            {
                OnError(new InvalidOperationException("OnSuccess called after termination"));
                return;
            }
            OnCompleted();
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
        /// Disposes the connection to the upstream if any or
        /// makes sure the upstream is immediately disposed upon
        /// subscription.
        /// </summary>
        /// <returns>this</returns>
        public TestObserver<T> Cancel()
        {
            Dispose();
            return this;
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
        /// <param name="message">The message to use as prefix to the state</param>
        /// <returns>The exception to be thrown.</returns>
        Exception Fail(String message)
        {
            var count = Volatile.Read(ref itemCount);
            var err = Volatile.Read(ref errorCount);
            var compl = Volatile.Read(ref completions);
            var timeout = this.timeout;
            var subs = Volatile.Read(ref upstream) != null;
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

            if (subs)
            {
                msg += ", subscribed!";
            }
            if (timeout)
            {
                msg += ", timeout!";
            }

            if (disposed)
            {
                msg += ", disposed!";
            }
            if (queue != null)
            {
                msg += ", fuseable!";
            }
            if (fusionRequested > FusionSupport.None)
            {
                msg += ", fusion-requested: " + FusionModeToString(fusionRequested);
            }
            if (fusionEstablished > FusionSupport.None)
            {
                msg += ", fusion-established: " + FusionModeToString(fusionEstablished);
            }

            if (tag != null)
            {
                msg += ", tag=" + tag;
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
        /// specified exception type and via exactly one <see cref="OnError(Exception)"/>
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
                    throw Fail("Exception type present but not with the specified message" + (messageContains ? " part: " : ": ") + message);
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
            if (requireOnSubscribe)
            {
                AssertSubscribed();
            }
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
        /// <param name="expectedError">The expected error type</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertFailure(Type expectedError, params T[] expected)
        {
            if (requireOnSubscribe)
            {
                AssertSubscribed();
            }
            AssertValues(expected);
            AssertNotCompleted();
            AssertError(expectedError);
            return this;
        }

        /// <summary>
        /// Assert that there were no events signaled to this TestObserver.
        /// </summary>
        /// <returns>this</returns>
        public TestObserver<T> AssertEmpty()
        {
            if (requireOnSubscribe)
            {
                AssertSubscribed();
            }
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
        public int ItemCount => Volatile.Read(ref itemCount);

        /// <summary>
        /// Returns the number of errors received by the TestObserver.
        /// </summary>
        /// <remarks>Since 0.0.3</remarks>
        public int ErrorCount => Volatile.Read(ref errorCount);

        /// <summary>
        /// Assert that the TestObserver is not terminated and contains only the
        /// specified items.
        /// </summary>
        /// <param name="expected">The expected items.</param>
        /// <returns>this</returns>
        public TestObserver<T> AssertValuesOnly(params T[] expected)
        {
            if (requireOnSubscribe)
            {
                AssertSubscribed();
            }
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
        public List<T> Items => items;

        /// <summary>
        /// The list of received errors so far. Use
        /// <see cref="ErrorCount"/> for the number of
        /// items to be read safely.
        /// </summary>
        /// <remarks>Since 0.0.3</remarks>
        public List<Exception> Errors => errors;

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
            if (index < 0 || index >= exs.Count)
            {
                throw Fail("The AggregateException index out of bounds. Expected: " + index + ", Actual: " + exs.Count);
            }
            if (errorType.GetTypeInfo().IsAssignableFrom(exs[index].GetType().GetTypeInfo()))
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
                return this;
            }
            throw Fail("Wrong error type @ " + index + ". Expected: " + errorType + ", Actual: " + exs[index].GetType());
        }

        /// <summary>
        /// Assert that there is exactly one <see cref="AggregateException"/>
        /// and it contains an error that is assignable to the given
        /// <paramref name="errorType"/>.
        /// </summary>
        /// <param name="errorType">The expected error type</param>
        /// <param name="message">The expected message (or part of it if <paramref name="messageContains"/> is true).</param>
        /// <param name="messageContains">If true and <paramref name="message"/> is not null, the error messages are compared as well.</param>
        /// <returns>this</returns>
        /// <remarks>Since 0.0.6</remarks>
        public TestObserver<T> AssertCompositeError(Type errorType, string message = null, bool messageContains = false)
        {
            AssertError(typeof(AggregateException));
            var exs = (errors[0] as AggregateException).InnerExceptions;

            TypeInfo typeInfo = errorType.GetTypeInfo();

            foreach (var ex in exs)
            {
                if (typeInfo.IsAssignableFrom(ex.GetType().GetTypeInfo()))
                {
                    if (message != null)
                    {
                        if (messageContains)
                        {
                            if (!ex.Message.Contains(message))
                            {
                                throw Fail("Error found with a different message part. Expected: " + message + ", Actual: " + ex.Message);
                            }
                        }
                        else
                        {
                            if (!ex.Message.Equals(message))
                            {
                                throw Fail("Error found with a different message. Expected: " + message + ", Actual: " + ex.Message);
                            }
                        }
                    }
                    return this;
                }
            }
            throw Fail("Error not found inside the AggregateException: " + errorType);
        }

        /// <summary>
        /// Assert that there is an AggregateException and it has
        /// the given number of inner exceptions.
        /// </summary>
        /// <param name="count">The expected count of inner exceptions</param>
        /// <returns>this</returns>
        /// <remarks>Since 0.0.8</remarks>
        public TestObserver<T> AssertCompositeErrorCount(int count)
        {
            AssertError(typeof(AggregateException));
            var exs = (errors[0] as AggregateException).InnerExceptions.Count;

            if (count != exs)
            {
                throw Fail("The AggregateException has different number of exceptions. Expected: " + count + ", Actual: " + exs);
            }
            return this;
        }

        /// <summary>
        /// Assert that OnSubscribe was called.
        /// </summary>
        /// <returns>this</returns>
        public TestObserver<T> AssertSubscribed()
        {
            if (Volatile.Read(ref upstream) == null)
            {
                throw Fail("OnSubscribe not called");
            }
            return this;
        }

        /// <summary>
        /// Tags this TestObserver which shows up in the
        /// failure message.
        /// </summary>
        /// <param name="tag">The tag to use, null clears any tag</param>
        /// <returns>this</returns>
        /// <remarks>Since 0.0.8</remarks>
        public TestObserver<T> WithTag(string tag)
        {
            this.tag = tag;
            return this;
        }

        /// <summary>
        /// The current tag string or null if not set.
        /// </summary>
        /// <remarks>Since 0.0.8</remarks>
        public string Tag { get { return tag; } }

        /// <summary>
        /// The number of OnCompleted events received.
        /// </summary>
        /// <remarks>Since 0.0.15</remarks>
        public int CompletionCount { get { return Volatile.Read(ref completions); } }

        /// <summary>
        /// Assert that the upstream called <see cref="OnSubscribe(IDisposable)"/>
        /// with a fuseable <see cref="IFuseableDisposable{T}"/> instance.
        /// </summary>
        /// <returns>this</returns>
        /// <remarks>Since 0.0.17</remarks>
        public TestObserver<T> AssertFuseable()
        {
            if (queue == null)
            {
                throw Fail("Upstream not fuseable");
            }
            return this;
        }

        /// <summary>
        /// Assert that the upstream called <see cref="OnSubscribe(IDisposable)"/>
        /// with a non-fuseable <see cref="IDisposable"/> instance.
        /// </summary>
        /// <returns>this</returns>
        /// <remarks>Since 0.0.17</remarks>
        public TestObserver<T> AssertNotFuseable()
        {
            if (queue != null)
            {
                throw Fail("Upstream is fuseable");
            }
            return this;
        }

        private string FusionModeToString(int mode)
        {
            switch (mode)
            {
                case 0: return "None";
                case 1: return "Sync";
                case 2: return "Async";
                case 3: return "Any";
                case 5: return "Sync+Boundary";
                case 6: return "Async+Boundary";
                case 7: return "Any+Boundary";
                default: return "Unknown: " + mode;
            }
        }

        /// <summary>
        /// Assert that the specified fusion mode has been established.
        /// See the <see cref="FusionSupport"/> constants.
        /// </summary>
        /// <param name="mode">The expected established fusion mode.</param>
        /// <returns>this</returns>
        /// <remarks>Since 0.0.17</remarks>
        public TestObserver<T> AssertFusionMode(int mode)
        {
            if (fusionEstablished != mode)
            {
                throw Fail("Wrong fusion mode. Expected: " + FusionModeToString(mode) + ", Actual: " + FusionModeToString(fusionEstablished));
            }
            return this;
        }
    }
}
