using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test
{
    /// <summary>
    /// An enumerable/enumerator that is empty but can fail at
    /// various calls.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    public sealed class FailingEnumerable<T> : IEnumerable<T>, IEnumerator<T>
    {
        readonly bool failGetEnumerable;

        readonly bool failMoveNext;

        readonly bool failOnDispose;

        public FailingEnumerable(bool failGetEnumerable, bool failMoveNext, bool failOnDispose)
        {
            this.failGetEnumerable = failGetEnumerable;
            this.failMoveNext = failMoveNext;
            this.failOnDispose = failOnDispose;
        }

        public T Current => default(T);

        object IEnumerator.Current => null;

        public void Dispose()
        {
            if (failOnDispose)
            {
                throw new InvalidOperationException();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (failGetEnumerable)
            {
                throw new InvalidOperationException();
            }
            return this;
        }

        public bool MoveNext()
        {
            if (failMoveNext)
            {
                throw new InvalidOperationException();
            }
            return false;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
