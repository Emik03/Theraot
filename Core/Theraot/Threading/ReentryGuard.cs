﻿using System;
using System.Collections.Generic;
using Theraot.Threading.Needles;

namespace Theraot.Threading
{
    /// <summary>
    /// Represents a context to execute operationg without reentry.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCode]
    public sealed class ReentryGuard
    {
        private StructNeedle<NoTrackingThreadLocal<Tuple<Queue<Action>, Guard>>> _workQueue;

        /// <summary>
        /// Creates a new instance of <see cref="ReentryGuard"/>.
        /// </summary>
        public ReentryGuard()
        {
            _workQueue = new StructNeedle<NoTrackingThreadLocal<Tuple<Queue<Action>, Guard>>>
                (
                    new NoTrackingThreadLocal<Tuple<Queue<Action>, Guard>>
                    (
                        () => new Tuple<Queue<Action>, Guard>(new Queue<Action>(), new Guard())
                    )
                );
        }

        /// <summary>
        /// Returns whatever or not the current thread did enter.
        /// </summary>
        public bool IsTaken
        {
            get
            {
                var local = _workQueue.Value.Value;
                return local.Item2.IsTaken;
            }
        }

        /// <summary>
        /// Executes an operation-
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>Returns a promise to finish the execution.</returns>
        public IPromise Execute(Action operation)
        {
            var local = _workQueue.Value.Value;
            var result = AddExecution(operation, local);
            ExecutePending(local);
            return result;
        }

        /// <summary>
        /// Executes an operation-
        /// </summary>
        /// <typeparam name="T">The return value of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>Returns a promise to finish the execution.</returns>
        public IPromise<T> Execute<T>(Func<T> operation)
        {
            var local = _workQueue.Value.Value;
            var result = AddExecution(operation, local);
            ExecutePending(local);
            return result;
        }

        private static IPromise AddExecution(Action action, Tuple<Queue<Action>, Guard> local)
        {
            PromiseNeedle.Promised promised;
            // TODO: waiting on the returned promise will cause the thread to lock - replace with Tasks
            var result = new PromiseNeedle(out promised, false);
            local.Item1.Enqueue
            (
                () =>
                {
                    try
                    {
                        action.Invoke();
                        promised.OnCompleted();
                    }
                    catch (Exception exception)
                    {
                        promised.OnError(exception);
                    }
                }
            );
            return result;
        }

        private static IPromise<T> AddExecution<T>(Func<T> action, Tuple<Queue<Action>, Guard> local)
        {
            PromiseNeedle<T>.Promised promised;
            // TODO: waiting on the returned promise will cause the thread to lock - replace with Tasks
            var result = new PromiseNeedle<T>(out promised, false);
            local.Item1.Enqueue
            (
                () =>
                {
                    try
                    {
                        promised.OnNext(action.Invoke());
                    }
                    catch (Exception exception)
                    {
                        promised.OnError(exception);
                    }
                }
            );
            return result;
        }

        private static void ExecutePending(Tuple<Queue<Action>, Guard> local)
        {
            var guard = local.Item2;
            var queue = local.Item1;
            while (queue.Count > 0)
            {
                IDisposable engagement;
                if (guard.Enter(out engagement))
                {
                    using (engagement)
                    {
                        var action = queue.Dequeue();
                        action.Invoke();
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}