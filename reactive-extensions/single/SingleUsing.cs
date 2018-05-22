using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Generates a resource and a dependent single source
    /// for each single observer and cleans up the resource
    /// just before or just after the single source terminated
    /// or the observer has disposed the setup.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="S">The resource type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleUsing<T, S> : ISingleSource<T>
    {
        readonly Func<S> resourceSupplier;

        readonly Func<S, ISingleSource<T>> sourceSelector;

        readonly Action<S> resourceCleanup;

        readonly bool eagerCleanup;

        public SingleUsing(Func<S> resourceSupplier, Func<S, ISingleSource<T>> sourceSelector, Action<S> resourceCleanup, bool eagerCleanup)
        {
            this.resourceSupplier = resourceSupplier;
            this.sourceSelector = sourceSelector;
            this.resourceCleanup = resourceCleanup;
            this.eagerCleanup = eagerCleanup;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var resource = default(S);

            try
            {
                resource = resourceSupplier();
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            var c = default(ISingleSource<T>);

            try
            {
                c = RequireNonNullRef(sourceSelector(resource), "The sourceSelector returned an null ISingleSource");
            }
            catch (Exception ex)
            {
                if (eagerCleanup)
                {
                    try
                    {
                        resourceCleanup(resource);
                    }
                    catch (Exception exc)
                    {
                        ex = new AggregateException(ex, exc);
                    }

                    DisposableHelper.Error(observer, ex);
                }
                else
                {
                    DisposableHelper.Error(observer, ex);
                    try
                    {
                        resourceCleanup(resource);
                    }
                    catch (Exception)
                    {
                        // TODO where could this go???
                    }
                }
                return;
            }

            c.Subscribe(new UsingObserver(observer, resource, resourceCleanup, eagerCleanup));
        }

        sealed class UsingObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly bool eagerCleanup;

            S resource;

            Action<S> resourceCleanup;

            IDisposable upstream;

            public UsingObserver(ISingleObserver<T> downstream, S resource, Action<S> resourceCleanup, bool eagerCleanup)
            {
                this.downstream = downstream;
                this.eagerCleanup = eagerCleanup;
                this.resource = resource;
                this.resourceCleanup = resourceCleanup;
            }

            void CleanupAfter()
            {
                var a = Volatile.Read(ref resourceCleanup);
                if (a != null)
                {
                    a = Interlocked.Exchange(ref resourceCleanup, null);
                    if (a != null)
                    {
                        var r = resource;
                        resource = default(S);
                        try
                        {
                            a(r);
                        }
                        catch (Exception)
                        {
                            // where should these go?
                        }
                    }
                }
            }

            public void Dispose()
            {
                upstream.Dispose();
                CleanupAfter();
            }

            public void OnSuccess(T item)
            {
                if (eagerCleanup)
                {
                    var a = Interlocked.Exchange(ref resourceCleanup, null);
                    if (a != null)
                    {
                        var r = resource;
                        resource = default(S);
                        try
                        {
                            a(r);
                        }
                        catch (Exception ex)
                        {
                            downstream.OnError(ex);
                            return;
                        }
                    }

                    downstream.OnSuccess(item);
                }
                else
                {
                    downstream.OnSuccess(item);

                    CleanupAfter();
                }
            }

            public void OnError(Exception error)
            {
                if (eagerCleanup)
                {
                    var a = Interlocked.Exchange(ref resourceCleanup, null);
                    if (a != null)
                    {
                        var r = resource;
                        resource = default(S);
                        try
                        {
                            a(r);
                        }
                        catch (Exception ex)
                        {
                            error = new AggregateException(error, ex);
                        }
                    }

                    downstream.OnError(error);
                }
                else
                {
                    downstream.OnError(error);
                    CleanupAfter();
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
