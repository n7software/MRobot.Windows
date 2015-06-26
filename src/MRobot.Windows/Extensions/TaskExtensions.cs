using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MRobot.Windows.Extensions
{

    public static class TaskExtensions
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
        {
            return source.ForEachAsync(Environment.ProcessorCount, body);
        }
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }


        /// <summary>
        /// Useful for executing some code after a certain delay, and cancelling that previous task if this is called again.
        /// </summary>
        /// <example>
        /// This is an example of how to use this method
        /// <code>
        /// class TestClass
        /// {
        ///     CancellationTokenSource refreshItemsCts = null;
        /// 
        ///     public void OnRefreshItems()
        ///     {
        ///         // Delays execution of RefreshItems() for 1 second, cancelling the original call if this code is
        ///         // run again in within that time span
        ///         refreshItemsCts = TaskUtilities.PerformAfterDelayWithThrottling(RefreshItems, TimeSpan.FromSeconds(1), refreshItemsCts);
        ///     }
        /// 
        ///     private void RefreshItems()
        ///     {
        ///         // Some expensive code...
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="workToPerform">The method you want called to perform some work.</param>
        /// <param name="delay">The amount of time to wait before performing work.</param>
        /// <param name="existingToken">Any existing <see cref="CancellationTokenSource"/> returned from the last call to this method. Defaults to null.</param>
        /// <returns></returns>
        public static CancellationTokenSource PerformAfterDelayWithThrottling(Action workToPerform, TimeSpan delay, CancellationTokenSource existingToken = null)
        {
            if (existingToken != null && !existingToken.IsCancellationRequested)
            {
                existingToken.Cancel();
            }

            var cancelTokenSource = new CancellationTokenSource();

            Task.Delay(delay, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        workToPerform();
                    }
                }, cancelTokenSource.Token);

            return cancelTokenSource;
        }
    }
}
