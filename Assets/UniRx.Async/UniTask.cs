﻿#if CSHARP_7_OR_LATER || (UNITY_2018_3_OR_NEWER && (NET_STANDARD_2_0 || NET_4_6))
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0436

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using UniRx.Async.CompilerServices;
using UniRx.Async.Internal;

namespace UniRx.Async
{
    internal static class AwaiterActions
    {
        internal static readonly Action<object> InvokeActionDelegate = InvokeAction;

        static void InvokeAction(object state)
        {
            ((Action)state).Invoke();
        }
    }

    /// <summary>
    /// Lightweight unity specified task-like object.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncUniTask2MethodBuilder))]
    public readonly partial struct UniTask2
    {
        readonly IUniTaskSource source;
        readonly short token;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask2(IUniTaskSource source, short token)
        {
            this.source = source;
            this.token = token;
        }

        public AwaiterStatus Status
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (source == null) return AwaiterStatus.Succeeded;
                return source.GetStatus(token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        /// <summary>
        /// returns (bool IsCanceled) instead of throws OperationCanceledException.
        /// </summary>
        public UniTask2<bool> SuppressCancellationThrow()
        {
            var status = Status;
            if (status == AwaiterStatus.Succeeded) return CompletedTasks2.False;
            if (status == AwaiterStatus.Canceled) return CompletedTasks2.True;
            return new UniTask2<bool>(new IsCanceledSource(source), token);
        }

        public override string ToString()
        {
            if (source == null) return "()";
            return "(" + source.UnsafeGetStatus() + ")";
        }

        /// <summary>
        /// Memoizing inner IValueTaskSource. The result UniTask can await multiple.
        /// </summary>
        public UniTask2 Preserve()
        {
            if (source == null)
            {
                return this;
            }
            else
            {
                return new UniTask2(new MemoizeSource(source), token);
            }
        }

        public static implicit operator UniTask2<AsyncUnit>(UniTask2 task)
        {
            if (task.source == null) return CompletedTasks2.AsyncUnit;

            var status = task.source.GetStatus(task.token);
            if (status.IsCompletedSuccessfully())
            {
                return CompletedTasks2.AsyncUnit;
            }

            return new UniTask2<AsyncUnit>(new AsyncUnitSource(task.source), task.token);
        }

        class AsyncUnitSource : IUniTaskSource<AsyncUnit>
        {
            readonly IUniTaskSource source;

            public AsyncUnitSource(IUniTaskSource source)
            {
                this.source = source;
            }

            public AsyncUnit GetResult(short token)
            {
                source.GetResult(token);
                return AsyncUnit.Default;
            }

            public AwaiterStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }

            public AwaiterStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            void IUniTaskSource.GetResult(short token)
            {
                GetResult(token);
            }
        }

        class IsCanceledSource : IUniTaskSource<bool>
        {
            readonly IUniTaskSource source;

            public IsCanceledSource(IUniTaskSource source)
            {
                this.source = source;
            }

            public bool GetResult(short token)
            {
                if (source.GetStatus(token) == AwaiterStatus.Canceled)
                {
                    return true;
                }

                source.GetResult(token);
                return false;
            }

            void IUniTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public AwaiterStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            public AwaiterStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }
        }

        class MemoizeSource : IUniTaskSource
        {
            IUniTaskSource source;
            ExceptionDispatchInfo exception;
            AwaiterStatus status;

            public MemoizeSource(IUniTaskSource source)
            {
                this.source = source;
            }

            public void GetResult(short token)
            {
                if (source == null)
                {
                    if (exception != null)
                    {
                        exception.Throw();
                    }
                }
                else
                {
                    try
                    {
                        source.GetResult(token);
                        status = AwaiterStatus.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                        if (ex is OperationCanceledException)
                        {
                            status = AwaiterStatus.Canceled;
                        }
                        else
                        {
                            status = AwaiterStatus.Faulted;
                        }
                        throw;
                    }
                    finally
                    {
                        source = null;
                    }
                }
            }

            public AwaiterStatus GetStatus(short token)
            {
                if (source == null)
                {
                    return status;
                }

                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                if (source == null)
                {
                    continuation(state);
                }
                else
                {
                    source.OnCompleted(continuation, state, token);
                }
            }

            public AwaiterStatus UnsafeGetStatus()
            {
                if (source == null)
                {
                    return status;
                }

                return source.UnsafeGetStatus();
            }
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly UniTask2 task;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in UniTask2 task)
            {
                this.task = task;
            }

            public bool IsCompleted
            {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return task.Status.IsCompleted();
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult()
            {
                if (task.source == null) return;
                task.source.GetResult(task.token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                if (task.source == null)
                {
                    continuation();
                }
                else
                {
                    task.source.OnCompleted(AwaiterActions.InvokeActionDelegate, continuation, task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (task.source == null)
                {
                    continuation();
                }
                else
                {
                    task.source.OnCompleted(AwaiterActions.InvokeActionDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            public void SourceOnCompleted(Action<object> continuation, object state)
            {
                if (task.source == null)
                {
                    continuation(state);
                }
                else
                {
                    task.source.OnCompleted(continuation, state, task.token);
                }
            }
        }
    }

    /// <summary>
    /// Lightweight unity specified task-like object.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncUniTask2MethodBuilder<>))]
    public readonly struct UniTask2<T>
    {
        readonly IUniTaskSource<T> source;
        readonly T result;
        readonly short token;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask2(T result)
        {
            this.source = default;
            this.token = default;
            this.result = result;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask2(IUniTaskSource<T> source, short token)
        {
            this.source = source;
            this.token = token;
            this.result = default;
        }

        public AwaiterStatus Status
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (source == null) ? AwaiterStatus.Succeeded : source.GetStatus(token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        /// <summary>
        /// Memoizing inner IValueTaskSource. The result UniTask can await multiple.
        /// </summary>
        public UniTask2<T> Preserve()
        {
            if (source == null)
            {
                return this;
            }
            else
            {
                return new UniTask2<T>(new MemoizeSource(source), token);
            }
        }

        public static implicit operator UniTask2(UniTask2<T> task)
        {
            if (task.source == null) return UniTask2.CompletedTask;

            var status = task.source.GetStatus(task.token);
            if (status.IsCompletedSuccessfully())
            {
                return UniTask2.CompletedTask;
            }

            return new UniTask2(task.source, task.token);
        }

        /// <summary>
        /// returns (bool IsCanceled, T Result) instead of throws OperationCanceledException.
        /// </summary>
        public UniTask2<(bool IsCanceled, T Result)> SuppressCancellationThrow()
        {
            if (source == null)
            {
                return new UniTask2<(bool IsCanceled, T Result)>((false, result));
            }

            return new UniTask2<(bool, T)>(new IsCanceledSource(source), token);
        }

        public override string ToString()
        {
            return (this.source == null) ? result?.ToString()
                 : "(" + this.source.UnsafeGetStatus() + ")";
        }

        class IsCanceledSource : IUniTaskSource<(bool, T)>
        {
            readonly IUniTaskSource<T> source;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IsCanceledSource(IUniTaskSource<T> source)
            {
                this.source = source;
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (bool, T) GetResult(short token)
            {
                if (source.GetStatus(token) == AwaiterStatus.Canceled)
                {
                    return (true, default);
                }

                var result = source.GetResult(token);
                return (false, result);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IUniTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AwaiterStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AwaiterStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }
        }

        class MemoizeSource : IUniTaskSource<T>
        {
            IUniTaskSource<T> source;
            T result;
            ExceptionDispatchInfo exception;
            AwaiterStatus status;

            public MemoizeSource(IUniTaskSource<T> source)
            {
                this.source = source;
            }

            public T GetResult(short token)
            {
                if (source == null)
                {
                    if (exception != null)
                    {
                        exception.Throw();
                    }
                    return result;
                }
                else
                {
                    try
                    {
                        result = source.GetResult(token);
                        status = AwaiterStatus.Succeeded;
                        return result;
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                        if (ex is OperationCanceledException)
                        {
                            status = AwaiterStatus.Canceled;
                        }
                        else
                        {
                            status = AwaiterStatus.Faulted;
                        }
                        throw;
                    }
                    finally
                    {
                        source = null;
                    }
                }
            }

            void IUniTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public AwaiterStatus GetStatus(short token)
            {
                if (source == null)
                {
                    return status;
                }

                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                if (source == null)
                {
                    continuation(state);
                }
                else
                {
                    source.OnCompleted(continuation, state, token);
                }
            }

            public AwaiterStatus UnsafeGetStatus()
            {
                if (source == null)
                {
                    return status;
                }

                return source.UnsafeGetStatus();
            }
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly UniTask2<T> task;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in UniTask2<T> task)
            {
                this.task = task;
            }

            public bool IsCompleted
            {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return task.Status.IsCompleted();
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetResult()
            {
                var s = task.source;
                if (s == null)
                {
                    return task.result;
                }
                else
                {
                    return s.GetResult(task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation();
                }
                else
                {
                    s.OnCompleted(AwaiterActions.InvokeActionDelegate, continuation, task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation();
                }
                else
                {
                    s.OnCompleted(AwaiterActions.InvokeActionDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            public void SourceOnCompleted(Action<object> continuation, object state)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation(state);
                }
                else
                {
                    s.OnCompleted(continuation, state, task.token);
                }
            }
        }
    }


    /// <summary>
    /// Lightweight unity specified task-like object.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
    public partial struct UniTask : IEquatable<UniTask>
    {
        static readonly UniTask<AsyncUnit> DefaultAsyncUnitTask = new UniTask<AsyncUnit>(AsyncUnit.Default);

        readonly IAwaiter awaiter;

        [DebuggerHidden]
        public UniTask(IAwaiter awaiter)
        {
            this.awaiter = awaiter;
        }

        [DebuggerHidden]
        public UniTask(Func<UniTask> factory)
        {
            this.awaiter = new LazyPromise(factory);
        }

        [DebuggerHidden]
        public AwaiterStatus Status
        {
            get
            {
                return awaiter == null ? AwaiterStatus.Succeeded : awaiter.Status;
            }
        }

        [DebuggerHidden]
        public bool IsCompleted
        {
            get
            {
                return awaiter == null ? true : awaiter.IsCompleted;
            }
        }

        [DebuggerHidden]
        public void GetResult()
        {
            if (awaiter != null)
            {
                awaiter.GetResult();
            }
        }

        [DebuggerHidden]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        /// <summary>
        /// returns (bool IsCanceled) instead of throws OperationCanceledException.
        /// </summary>
        public UniTask<bool> SuppressCancellationThrow()
        {
            var status = Status;
            if (status == AwaiterStatus.Succeeded) return CompletedTasks.False;
            if (status == AwaiterStatus.Canceled) return CompletedTasks.True;
            return new UniTask<bool>(new IsCanceledAwaiter(awaiter));
        }

        public bool Equals(UniTask other)
        {
            if (this.awaiter == null && other.awaiter == null)
            {
                return true;
            }
            else if (this.awaiter != null && other.awaiter != null)
            {
                return this.awaiter == other.awaiter;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            if (this.awaiter == null)
            {
                return 0;
            }
            else
            {
                return this.awaiter.GetHashCode();
            }
        }

        public override string ToString()
        {
            return (this.awaiter == null) ? "()"
                 : (this.awaiter.Status == AwaiterStatus.Succeeded) ? "()"
                 : "(" + this.awaiter.Status + ")";
        }

        public static implicit operator UniTask<AsyncUnit>(UniTask task)
        {
            if (task.awaiter != null)
            {
                if (task.awaiter.IsCompleted)
                {
                    return DefaultAsyncUnitTask;
                }
                else
                {
                    // UniTask<T> -> UniTask is free but UniTask -> UniTask<T> requires wrapping cost.
                    return new UniTask<AsyncUnit>(new AsyncUnitAwaiter(task.awaiter));
                }
            }
            else
            {
                return DefaultAsyncUnitTask;
            }
        }

        class AsyncUnitAwaiter : IAwaiter<AsyncUnit>
        {
            readonly IAwaiter awaiter;

            public AsyncUnitAwaiter(IAwaiter awaiter)
            {
                this.awaiter = awaiter;
            }

            public bool IsCompleted => awaiter.IsCompleted;

            public AwaiterStatus Status => awaiter.Status;

            public AsyncUnit GetResult()
            {
                awaiter.GetResult();
                return AsyncUnit.Default;
            }

            public void OnCompleted(Action continuation)
            {
                awaiter.OnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                awaiter.UnsafeOnCompleted(continuation);
            }

            void IAwaiter.GetResult()
            {
                awaiter.GetResult();
            }
        }

        class IsCanceledAwaiter : IAwaiter<bool>
        {
            readonly IAwaiter awaiter;

            public IsCanceledAwaiter(IAwaiter awaiter)
            {
                this.awaiter = awaiter;
            }

            public bool IsCompleted => awaiter.IsCompleted;

            public AwaiterStatus Status => awaiter.Status;

            public bool GetResult()
            {
                if (awaiter.Status == AwaiterStatus.Canceled)
                {
                    return true;
                }
                awaiter.GetResult();
                return false;
            }

            public void OnCompleted(Action continuation)
            {
                awaiter.OnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                awaiter.UnsafeOnCompleted(continuation);
            }

            void IAwaiter.GetResult()
            {
                awaiter.GetResult();
            }
        }

        public struct Awaiter : IAwaiter
        {
            readonly UniTask task;

            [DebuggerHidden]
            public Awaiter(UniTask task)
            {
                this.task = task;
            }

            [DebuggerHidden]
            public bool IsCompleted => task.IsCompleted;

            [DebuggerHidden]
            public AwaiterStatus Status => task.Status;

            [DebuggerHidden]
            public void GetResult() => task.GetResult();

            [DebuggerHidden]
            public void OnCompleted(Action continuation)
            {
                if (task.awaiter != null)
                {
                    task.awaiter.OnCompleted(continuation);
                }
                else
                {
                    continuation();
                }
            }

            [DebuggerHidden]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (task.awaiter != null)
                {
                    task.awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    continuation();
                }
            }
        }
    }

    /// <summary>
    /// Lightweight unity specified task-like object.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder<>))]
    public struct UniTask<T> : IEquatable<UniTask<T>>
    {
        readonly T result;
        readonly IAwaiter<T> awaiter;

        [DebuggerHidden]
        public UniTask(T result)
        {
            this.result = result;
            this.awaiter = null;
        }

        [DebuggerHidden]
        public UniTask(IAwaiter<T> awaiter)
        {
            this.result = default(T);
            this.awaiter = awaiter;
        }

        [DebuggerHidden]
        public UniTask(Func<UniTask<T>> factory)
        {
            this.result = default(T);
            this.awaiter = new LazyPromise<T>(factory);
        }

        [DebuggerHidden]
        public AwaiterStatus Status
        {
            get
            {
                return awaiter == null ? AwaiterStatus.Succeeded : awaiter.Status;
            }
        }

        [DebuggerHidden]
        public bool IsCompleted
        {
            get
            {
                return awaiter == null ? true : awaiter.IsCompleted;
            }
        }

        [DebuggerHidden]
        public T Result
        {
            get
            {
                if (awaiter == null)
                {
                    return result;
                }
                else
                {
                    return awaiter.GetResult();
                }
            }
        }

        [DebuggerHidden]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        /// <summary>
        /// returns (bool IsCanceled, T Result) instead of throws OperationCanceledException.
        /// </summary>
        public UniTask<(bool IsCanceled, T Result)> SuppressCancellationThrow()
        {
            var status = Status;
            if (status == AwaiterStatus.Succeeded)
            {
                return new UniTask<(bool, T)>((false, Result));
            }
            else if (status == AwaiterStatus.Canceled)
            {
                return new UniTask<(bool, T)>((true, default(T)));
            }
            return new UniTask<(bool, T)>(new IsCanceledAwaiter(awaiter));
        }

        public bool Equals(UniTask<T> other)
        {
            if (this.awaiter == null && other.awaiter == null)
            {
                return EqualityComparer<T>.Default.Equals(this.result, other.result);
            }
            else if (this.awaiter != null && other.awaiter != null)
            {
                return this.awaiter == other.awaiter;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            if (this.awaiter == null)
            {
                if (result == null) return 0;
                return result.GetHashCode();
            }
            else
            {
                return this.awaiter.GetHashCode();
            }
        }

        public override string ToString()
        {
            return (this.awaiter == null) ? result.ToString()
                 : (this.awaiter.Status == AwaiterStatus.Succeeded) ? this.awaiter.GetResult().ToString()
                 : "(" + this.awaiter.Status + ")";
        }

        public static implicit operator UniTask(UniTask<T> task)
        {
            if (task.awaiter != null)
            {
                return new UniTask(task.awaiter);
            }
            else
            {
                return new UniTask();
            }
        }

        class IsCanceledAwaiter : IAwaiter<(bool, T)>
        {
            readonly IAwaiter<T> awaiter;

            public IsCanceledAwaiter(IAwaiter<T> awaiter)
            {
                this.awaiter = awaiter;
            }

            public bool IsCompleted => awaiter.IsCompleted;

            public AwaiterStatus Status => awaiter.Status;

            public (bool, T) GetResult()
            {
                if (awaiter.Status == AwaiterStatus.Canceled)
                {
                    return (true, default(T));
                }
                return (false, awaiter.GetResult());
            }

            public void OnCompleted(Action continuation)
            {
                awaiter.OnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                awaiter.UnsafeOnCompleted(continuation);
            }

            void IAwaiter.GetResult()
            {
                awaiter.GetResult();
            }
        }

        public struct Awaiter : IAwaiter<T>
        {
            readonly UniTask<T> task;

            [DebuggerHidden]
            public Awaiter(in UniTask<T> task)
            {
                this.task = task;
            }

            [DebuggerHidden]
            public bool IsCompleted => task.IsCompleted;

            [DebuggerHidden]
            public AwaiterStatus Status => task.Status;

            [DebuggerHidden]
            void IAwaiter.GetResult() => GetResult();

            [DebuggerHidden]
            public T GetResult() => task.Result;

            [DebuggerHidden]
            public void OnCompleted(Action continuation)
            {
                if (task.awaiter != null)
                {
                    task.awaiter.OnCompleted(continuation);
                }
                else
                {
                    continuation();
                }
            }

            [DebuggerHidden]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (task.awaiter != null)
                {
                    task.awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    continuation();
                }
            }
        }
    }
}

#endif
