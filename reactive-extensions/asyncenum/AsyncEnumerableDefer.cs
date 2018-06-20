using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableDefer<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEnumerable<T>> factory;

        public AsyncEnumerableDefer(Func<IAsyncEnumerable<T>> factory)
        {
            this.factory = factory;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var result = default(IAsyncEnumerator<T>);
            try
            {
                result = ValidationHelper.RequireNonNullRef(factory(), "The factory returned a null IAsyncEnumerable").GetAsyncEnumerator();
            }
            catch (Exception ex)
            {
                return new AsyncEnumerableError<T>.ErrorAsyncEnumerator(ex);
            }

            return result;
        }
    }
}
