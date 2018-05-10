using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A disposable contain that allows only one assignment to its <see cref="Disposable"/> propery.
    /// </summary>
    internal sealed class SingleAssignmentDisposable : IDisposable
    {
        IDisposable disposable;

        public IDisposable Disposable
        {
            get
            {
                var d = Volatile.Read(ref disposable);
                if (d == DisposableHelper.DISPOSED)
                {
                    return DisposableHelper.EMPTY;
                }
                return d;
            }
            set
            {
                if (Interlocked.CompareExchange(ref disposable, value, null) != null)
                {
                    value?.Dispose();
                    var d = Volatile.Read(ref disposable);
                    if (d != DisposableHelper.DISPOSED)
                    {
                        throw new InvalidOperationException("Disposable already set");
                    }
                }
            }
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref disposable);
        }
    }
}
