using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal static class AsyncExecutor
    {

        internal static Task ThenSchedule(this Task source, CancellationToken ct, IAsyncExecutor executor)
        {
            TaskCompletionSource<object> cts = new TaskCompletionSource<object>();

            source.ContinueWith(t =>
            {
                var cancel = executor.ScheduleAsync(async ct1 =>
                {
                    if (t.IsCanceled)
                    {
                        cts.TrySetCanceled();
                    }
                    else if (t.IsFaulted)
                    {
                        cts.TrySetException(t.Exception);
                    }
                    cts.TrySetResult(null);
                });
                ct.Register(() =>
                {
                    cancel.ContinueWith(c => c.Result.CloseAsync());
                });
            }, ct);
            return cts.Task;
        }
    }
}
