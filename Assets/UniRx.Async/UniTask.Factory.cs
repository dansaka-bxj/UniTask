﻿#if CSHARP_7_OR_LATER || (UNITY_2018_3_OR_NEWER && (NET_STANDARD_2_0 || NET_4_6))
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using UnityEngine.Events;

namespace UniRx.Async
{
    public partial struct UniTask2
    {
        static readonly UniTask2 CanceledUniTask = new Func<UniTask2>(() =>
        {
            var promise = new UniTaskCompletionSource2();
            promise.SetCanceled(CancellationToken.None);
            promise.MarkHandled();
            return promise.Task;
        })();

        static class CanceledUniTaskCache<T>
        {
            public static readonly UniTask2<T> Task;

            static CanceledUniTaskCache()
            {
                var promise = new UniTaskCompletionSource2<T>();
                promise.SetCanceled(CancellationToken.None);
                promise.MarkHandled();
                Task = promise.Task;
            }
        }

        public static readonly UniTask2 CompletedTask = new UniTask2();

        public static UniTask2 FromException(Exception ex)
        {
            var promise = new UniTaskCompletionSource2();
            promise.SetException(ex);
            promise.MarkHandled();
            return promise.Task;
        }

        public static UniTask2<T> FromException<T>(Exception ex)
        {
            var promise = new UniTaskCompletionSource2<T>();
            promise.SetException(ex);
            promise.MarkHandled();
            return promise.Task;
        }

        public static UniTask2<T> FromResult<T>(T value)
        {
            return new UniTask2<T>(value);
        }

        public static UniTask2 FromCanceled(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledUniTask;
            }
            else
            {
                var promise = new UniTaskCompletionSource2();
                promise.SetCanceled(cancellationToken);
                promise.MarkHandled();
                return promise.Task;
            }
        }

        public static UniTask2<T> FromCanceled<T>(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledUniTaskCache<T>.Task;
            }
            else
            {
                var promise = new UniTaskCompletionSource2<T>();
                promise.SetCanceled(cancellationToken);
                promise.MarkHandled();
                return promise.Task;
            }
        }

        // TODO:...

        /// <summary>shorthand of new UniTask[T](Func[UniTask[T]] factory)</summary>
        public static UniTask<T> Lazy<T>(Func<UniTask<T>> factory)
        {
            return new UniTask<T>(factory);
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// For example: FooEvent += () => UniTask.Void(async () => { /* */ })
        /// </summary>
        public static void Void(Func<UniTask> asyncAction)
        {
            asyncAction().Forget();
        }

        public static Action VoidAction(Func<UniTask> asyncAction)
        {
            return () => Void(asyncAction);
        }

        public static UnityAction VoidUnityAction(Func<UniTask> asyncAction)
        {
            return () => Void(asyncAction);
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// For example: FooEvent += (sender, e) => UniTask.Void(async arg => { /* */ }, (sender, e))
        /// </summary>
        public static void Void<T>(Func<T, UniTask> asyncAction, T state)
        {
            asyncAction(state).Forget();
        }
    }


    // TODO:remove
    public partial struct UniTask
    {
        static readonly UniTask CanceledUniTask = new Func<UniTask>(() =>
        {
            var promise = new UniTaskCompletionSource<AsyncUnit>();
            promise.TrySetCanceled();
            promise.MarkHandled();
            return new UniTask(promise);
        })();

        public static UniTask CompletedTask
        {
            get
            {
                return new UniTask();
            }
        }

        public static UniTask FromException(Exception ex)
        {
            var promise = new UniTaskCompletionSource<AsyncUnit>();
            promise.TrySetException(ex);
            promise.MarkHandled();
            return new UniTask(promise);
        }

        public static UniTask<T> FromException<T>(Exception ex)
        {
            var promise = new UniTaskCompletionSource<T>();
            promise.TrySetException(ex);
            promise.MarkHandled();
            return new UniTask<T>(promise);
        }

        public static UniTask<T> FromResult<T>(T value)
        {
            return new UniTask<T>(value);
        }

        public static UniTask FromCanceled()
        {
            return CanceledUniTask;
        }

        public static UniTask<T> FromCanceled<T>()
        {
            return CanceledUniTaskCache<T>.Task;
        }

        public static UniTask FromCanceled(CancellationToken token)
        {
            var promise = new UniTaskCompletionSource<AsyncUnit>();
            promise.TrySetException(new OperationCanceledException(token));
            promise.MarkHandled();
            return new UniTask(promise);
        }

        public static UniTask<T> FromCanceled<T>(CancellationToken token)
        {
            var promise = new UniTaskCompletionSource<T>();
            promise.TrySetException(new OperationCanceledException(token));
            promise.MarkHandled();
            return new UniTask<T>(promise);
        }

        /// <summary>shorthand of new UniTask[T](Func[UniTask[T]] factory)</summary>
        public static UniTask<T> Lazy<T>(Func<UniTask<T>> factory)
        {
            return new UniTask<T>(factory);
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// For example: FooEvent += () => UniTask.Void(async () => { /* */ })
        /// </summary>
        public static void Void(Func<UniTask> asyncAction)
        {
            asyncAction().Forget();
        }

        public static Action VoidAction(Func<UniTask> asyncAction)
        {
            return () => Void(asyncAction);
        }

        public static UnityAction VoidUnityAction(Func<UniTask> asyncAction)
        {
            return () => Void(asyncAction);
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// For example: FooEvent += (sender, e) => UniTask.Void(async arg => { /* */ }, (sender, e))
        /// </summary>
        public static void Void<T>(Func<T, UniTask> asyncAction, T state)
        {
            asyncAction(state).Forget();
        }

        static class CanceledUniTaskCache<T>
        {
            public static readonly UniTask<T> Task;

            static CanceledUniTaskCache()
            {
                var promise = new UniTaskCompletionSource<T>();
                promise.TrySetCanceled();
                promise.MarkHandled();
                Task = new UniTask<T>(promise);
            }
        }
    }


    // TODO:remove
    internal static class CompletedTasks
    {
        public static readonly UniTask<bool> True = UniTask.FromResult(true);
        public static readonly UniTask<bool> False = UniTask.FromResult(false);
        public static readonly UniTask<int> Zero = UniTask.FromResult(0);
        public static readonly UniTask<int> MinusOne = UniTask.FromResult(-1);
        public static readonly UniTask<int> One = UniTask.FromResult(1);
    }


    // TODO:rename
    internal static class CompletedTasks2
    {
        public static readonly UniTask2 Completed = new UniTask2();
        public static readonly UniTask2<AsyncUnit> AsyncUnit = UniTask2.FromResult(UniRx.Async.AsyncUnit.Default);
        public static readonly UniTask2<bool> True = UniTask2.FromResult(true);
        public static readonly UniTask2<bool> False = UniTask2.FromResult(false);
        public static readonly UniTask2<int> Zero = UniTask2.FromResult(0);
        public static readonly UniTask2<int> MinusOne = UniTask2.FromResult(-1);
        public static readonly UniTask2<int> One = UniTask2.FromResult(1);
    }
}
#endif