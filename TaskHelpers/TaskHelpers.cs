using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskHelpers
{
    public static class TaskHelpersExtensions
    {
        /// <summary>
        /// Safely await a potentially null task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task SafeAwait(this Task? task)
        {
            if (task is not null)
                await task;
        }
        /// <summary>
        /// Safely await a potentially null Task<typeparamref name="T"/> with an optional default return value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="defaultReturn"></param>
        /// <returns></returns>
        public static async Task<T?> SafeAwait<T>(this Task<T>? task, T? defaultReturn = default)
        {
            if (task is not null)
                return await task;
            return defaultReturn;
        }
        /// <summary>
        /// Safely await a potentially null Task<typeparamref name="T"/> with an optional default return value computed by a function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="defaultReturn"></param>
        /// <returns>Task return value or result of <paramref name="defaultReturn"/> function</returns>
        public static async Task<T?> SafeAwait<T>(this Task<T>? task, Func<T?> defaultReturn)
        {
            T? rv = default;
            if (task is not null)
                rv = await task;
            return rv ?? defaultReturn();
        }
        /// <summary>
        /// Safely await a potentially null Task<typeparamref name="T"/> with an optional default return value computed by an async function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="defaultReturn"></param>
        /// <returns>Task return value or result of <paramref name="defaultReturn"/> async function</returns>
        public static async Task<T?> SafeAwait<T>(this Task<T>? task, Func<CancellationToken, Task<T?>> defaultReturn) => await SafeAwait(task, default, defaultReturn);

        /// <summary>
        /// Safely await a potentially null Task<typeparamref name="T"/> with an optional default return value computed by an async function with a cancellation token
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="defaultReturn"></param>
        /// <returns>Task return value or result of <paramref name="defaultReturn"/> async function</returns>
        public static async Task<T?> SafeAwait<T>(this Task<T>? task, CancellationToken cancellationToken, Func<CancellationToken, Task<T?>> defaultReturn)
        {
            if (task is not null)
                return await task;
            return await defaultReturn(cancellationToken);
        }
        /// <summary>
        /// Await all, possibly null, tasks and store results in a value tuple (easy destructuring on return)
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>ValueTuple of results from all awaited tasks</returns>
        public static async Task<(A?, B?)> WhenAll<A, B>(Task<A>? a, Task<B>? b)
        {
            await Task.WhenAll(a.SafeAwait(), b.SafeAwait());
            return (a is null ? default : a.Result, b is null ? default : b.Result);
        }
        public static async Task<(A?, B?, C?)> WhenAll<A, B, C>(Task<A>? a, Task<B>? b, Task<C>? c)
        {
            await Task.WhenAll(a.SafeAwait(), b.SafeAwait(), c.SafeAwait());
            return (a is null ? default : a.Result, b is null ? default : b.Result, c is null ? default : c.Result);
        }
        public static async Task<(A?, B?, C?, D?)> WhenAll<A, B, C, D>(Task<A>? a, Task<B>? b, Task<C>? c, Task<D>? d)
        {
            await Task.WhenAll(a.SafeAwait(), b.SafeAwait(), c.SafeAwait(), d.SafeAwait());
            return (a is null ? default : a.Result, b is null ? default : b.Result, c is null ? default : c.Result, d is null ? default : d.Result);
        }
        public static async Task<(A?, B?, C?, D?, E?)> WhenAll<A, B, C, D, E>(Task<A>? a, Task<B>? b, Task<C>? c, Task<D>? d, Task<E>? e)
        {
            await Task.WhenAll(a.SafeAwait(), b.SafeAwait(), c.SafeAwait(), d.SafeAwait(), e.SafeAwait());
            return (a is null ? default : a.Result, b is null ? default : b.Result, c is null ? default : c.Result, d is null ? default : d.Result, e is null ? default : e.Result);
        }

        //////////////////////////////////

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tasks"></param>
        /// <returns></returns>
        /// <remarks>From: https://devblogs.microsoft.com/pfxteam/processing-tasks-as-they-complete/</remarks>
        // List<Task<T>> tasks = …;
        // foreach(var bucket in Interleaved(tasks)) {
        //     var t = await bucket;
        //     try { Process(await t); }
        //     catch(OperationCanceledException) {}
        //     catch(Exception exc) { Handle(exc); }
        // }
        public static Task<Task<T>>[] Interleaved<T>(IEnumerable<Task<T>> tasks)
        {
            var inputTasks = tasks.ToList();

            var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
            var results = new Task<Task<T>>[buckets.Length];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new TaskCompletionSource<Task<T>>();
                results[i] = buckets[i].Task;
            }

            int nextTaskIndex = -1;

            foreach (var inputTask in inputTasks)
                inputTask.ContinueWith(Continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return results;

            void Continuation(Task<T> completed)
            {
                var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
                bucket.TrySetResult(completed);
            }
        }
    }
}
