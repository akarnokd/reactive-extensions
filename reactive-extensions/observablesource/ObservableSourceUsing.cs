using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceUsing<T, S> : IObservableSource<T>
    {
        readonly Func<S> resourceSupplier;

        readonly Func<S, IObservableSource<T>> sourceSelector;

        readonly Action<S> resourceCleanup;

        readonly bool eager;

        static readonly Action<S> EmptyAction = e => { };

        public ObservableSourceUsing(Func<S> resourceSupplier, Func<S, IObservableSource<T>> sourceSelector, Action<S> resourceCleanup, bool eager)
        {
            this.resourceSupplier = resourceSupplier;
            this.sourceSelector = sourceSelector;
            this.resourceCleanup = resourceCleanup ?? EmptyAction;
            this.eager = eager;
        }

        public void Subscribe(ISignalObserver<T> observer)
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

            var source = default(IObservableSource<T>);

            try
            {
                source = ValidationHelper.RequireNonNullRef(sourceSelector(resource), "The sourceSelector returned a null IObservableSource");
            }
            catch (Exception ex)
            {
                if (eager)
                {
                    try
                    {
                        resourceCleanup?.Invoke(resource);
                    }
                    catch (Exception exc)
                    {
                        ex = new AggregateException(ex, exc);
                    }
                }

                DisposableHelper.Error(observer, ex);

                if (!eager)
                {
                    try
                    {
                        resourceCleanup?.Invoke(resource);
                    }
                    catch (Exception)
                    {
                        // what to do with these?
                    }
                }
                return;
            }

            source.Subscribe(new UsingObserver(observer, resource, resourceCleanup, eager));
        }

        sealed class UsingObserver : BasicFuseableObserver<T, T>
        {
            readonly bool eager;

            S resource;

            Action<S> resourceCleanup;

            public UsingObserver(ISignalObserver<T> downstream, S resource, Action<S> resourceCleanup, bool eager) : base(downstream)
            {
                this.resource = resource;
                this.resourceCleanup = resourceCleanup;
                this.eager = eager;
            }

            public override int RequestFusion(int mode)
            {
                var fs = base.queue;
                if (fs != null)
                {
                    var m = fs.RequestFusion(mode);
                    fusionMode = m;
                    return m;
                }
                return FusionSupport.None;
            }

            public override T TryPoll(out bool success)
            {
                var v = default(T);
                var succ = false;
                try
                {
                    v = queue.TryPoll(out succ);
                }
                catch (Exception ex)
                {
                    try
                    {
                        Cleanup();
                    }
                    catch (Exception exc)
                    {
                        ex = new AggregateException(ex, exc);
                    }
                    throw ex;
                }

                if (fusionMode == FusionSupport.Sync)
                {
                    if (!succ)
                    {
                        Cleanup();
                    }
                }
                success = succ;
                return v;
            }

            public override void OnNext(T item)
            {
                downstream.OnNext(item);
            }

            public override void OnError(Exception error)
            {
                if (eager)
                {
                    try
                    {
                        Cleanup();
                    }
                    catch (Exception ex)
                    {
                        error = new AggregateException(error, ex);
                    }
                }

                base.OnError(error);

                CleanupAfter();
            }

            void CleanupAfter()
            {
                if (!eager)
                {
                    CleanupSuppressed();
                }
            }

            void CleanupSuppressed()
            {
                try
                {
                    Cleanup();
                }
                catch (Exception)
                {
                    // TODO where to put these?
                }
            }

            void Cleanup()
            {
                var action = Interlocked.Exchange(ref resourceCleanup, null);
                if (action != null)
                {
                    var r = resource;
                    resource = default;
                    action(r);
                }
            }

            public override void OnCompleted()
            {
                if (eager)
                {
                    try
                    {
                        Cleanup();
                    }
                    catch (Exception ex)
                    {
                        base.OnError(ex);
                        return;
                    }
                }

                base.OnCompleted();

                CleanupAfter();
            }

            public override void Dispose()
            {
                CleanupSuppressed();
                base.Dispose();
            }
        }
    }
}
