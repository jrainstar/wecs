// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Animancer
{
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerState
    partial class AnimancerState // AnimancerState.Awaiter.cs
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Returns an awaitable that will complete when this state passes its
        /// <see cref="NormalizedEndTime"/> or <see cref="AnimancerNode.IsInterrupted"/>.
        /// </summary>
        /// <remarks>
        /// You can directly use <c>await state;</c> or <c>yield return state;</c> instead.
        /// </remarks>
        public Awaiter GetAwaiter()
            => Awaiter.End(this);

        /// <summary>
        /// Returns an awaitable that will complete when this state passes its
        /// <see cref="NormalizedEndTime"/> or <see cref="AnimancerNode.IsInterrupted"/>.
        /// </summary>
        /// <remarks>
        /// If you aren't providing a <see cref="CancellationToken"/>,
        /// you can directly use <c>await state;</c> or <c>yield return state;</c> instead.
        /// </remarks>
        public Awaiter Await(
            CancellationToken cancel = default)
            => Awaiter.End(this, cancel);

        /// <summary>[Pro-Only]
        /// Returns an awaitable that will complete when the named event is triggered
        /// or <see cref="AnimancerNode.IsInterrupted"/>.
        /// </summary>
        public Awaiter Await(
            StringReference eventName,
            CancellationToken cancel = default)
            => Awaiter.Name(this, eventName, cancel);

        /// <summary>
        /// Returns an awaitable that will complete when the specified time passed
        /// or <see cref="AnimancerNode.IsInterrupted"/>.
        /// </summary>
        public Awaiter Await(
            float normalizedTime,
            CancellationToken cancel = default)
            => Awaiter.Time(this, normalizedTime, cancel);

        /************************************************************************************************************************/

        /// <summary>
        /// A custom <c>await</c> or <c>yield</c> instruction to wait for a particular
        /// <see cref="AnimancerState.NormalizedTime"/> to pass.
        /// </summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/Awaiter
        public class Awaiter :
            IDisposable,
            IEnumerator,
            INotifyCompletion,
            IUpdatable
        {
            /// <summary>An <see cref="ObjectPool{T}"/> for <see cref="Awaiter"/>.</summary>
            /// https://kybernetik.com.au/animancer/api/Animancer/Pool
            public class Pool : ObjectPool<Awaiter>
            {
                /************************************************************************************************************************/

                /// <summary>Singleton.</summary>
                public static Pool Instance = new();

                /************************************************************************************************************************/

                /// <inheritdoc/>
                protected override Awaiter New()
                    => new();

                /************************************************************************************************************************/
#if UNITY_EDITOR
                /************************************************************************************************************************/

                /// <inheritdoc/>
                public override Awaiter Acquire()
                {
                    AnimancerUtilities.AssertMainThread($"{nameof(Awaiter)}.{nameof(Pool)}.{nameof(Release)}");

                    return base.Acquire();
                }

                /************************************************************************************************************************/

                /// <inheritdoc/>
                public override void Release(Awaiter item)
                {
                    AnimancerUtilities.AssertMainThread($"{nameof(Awaiter)}.{nameof(Pool)}.{nameof(Release)}");

                    base.Release(item);
                }

                /************************************************************************************************************************/
#endif
                /************************************************************************************************************************/
            }

            /************************************************************************************************************************/

            /// <summary>Acquires an instance from the <see cref="Pool"/> and initializes it.</summary>
            public static Awaiter Acquire(
                AnimancerState state,
                CancellationToken cancel = default)
            {
                AnimancerUtilities.AssertMainThread($"{nameof(Awaiter)}.{nameof(Acquire)}");

                AnimancerUtilities.Assert(
                    state.Graph != null,
                    $"Unable to await state since its {nameof(Graph)} has not been set." +
                    $" The {nameof(Graph)} normally gets set when the state is first played." +
                    $" Alternatively, state.{nameof(SetGraph)} can be used to set it without playing.");

                var awaitable = Pool.Instance.Acquire();
                awaitable.Initialize(state);
                awaitable.Cancel = cancel;
                awaitable.ReleaseOnComplete = true;

                return awaitable;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Returns an awaitable that will complete when the `state` passes its
            /// <see cref="NormalizedEndTime"/> or <see cref="AnimancerNode.IsInterrupted"/>.
            /// </summary>
            /// <remarks>
            /// If you aren't providing a <see cref="CancellationToken"/>,
            /// you can directly use <c>await state;</c> or <c>yield return state;</c> instead.
            /// </remarks>
            public static Awaiter End(
                AnimancerState state,
                CancellationToken cancel = default)
            {
                var awaitable = Acquire(state, cancel);

                awaitable.NormalizedTime = float.NaN;

                return awaitable;
            }

            /************************************************************************************************************************/

            /// <summary>[Pro-Only]
            /// Returns an awaitable that will complete when the named event is triggered
            /// or <see cref="AnimancerNode.IsInterrupted"/>.
            /// </summary>
            public static Awaiter Name(
                AnimancerState state,
                StringReference eventName,
                CancellationToken cancel = default)
            {
                var awaitable = Acquire(state, cancel);

                if (eventName != null)
                {
                    var events = state.SharedEvents;
                    if (events != null)
                    {
                        var index = events.IndexOf(eventName);
                        if (index >= 0)
                        {
                            awaitable.NormalizedTime = events[index].normalizedTime;
                            return awaitable;
                        }
                    }
                }

                throw new ArgumentException($"State '{state}' has no event named {eventName}.");
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Returns an awaitable that will complete when the specified time passed
            /// or <see cref="AnimancerNode.IsInterrupted"/>.
            /// </summary>
            public static Awaiter Time(
                AnimancerState state,
                float normalizedTime,
                CancellationToken cancel = default)
            {
                var awaitable = Acquire(state, cancel);

                awaitable.NormalizedTime = normalizedTime;

                return awaitable;
            }

            /************************************************************************************************************************/

            /// <summary>The <see cref="AnimancerState"/> this object is observing.</summary>
            public AnimancerState State;

            /// <summary>The <see cref="AnimancerState.NormalizedTime"/> being awaited.</summary>
            /// <remarks><see cref="float.NaN"/> will use the <see cref="NormalizedEndTime"/>.</remarks>
            public float NormalizedTime;

            /// <summary>Should <see cref="Pool.Release"/> be called when it finishes waiting?</summary>
            public bool ReleaseOnComplete;

            /// <summary>A token which can be used to cancel this awaitable.</summary>
            public CancellationToken Cancel;

            /// <summary>Sets the callback to invoke when the <see cref="IsApproachingTime"/> returns false.</summary>
            private Action _Continuation;

            /************************************************************************************************************************/

            private const int SuccessIndex = int.MaxValue;
            private const int InterruptedIndex = int.MinValue;

            /// <summary>
            /// The <see cref="IUpdatable.UpdatableIndex"/> while this object is active,
            /// or the result code immediately after it has completed.
            /// </summary>
            private int _UpdatableIndex;

            int IUpdatable.UpdatableIndex
            {
                get => _UpdatableIndex;
                set => _UpdatableIndex = value;
            }

            /************************************************************************************************************************/

            /// <summary>Did the <see cref="State"/> successfully pass the <see cref="NormalizedTime"/>?</summary>
            /// <remarks>This method must only be called immediately after the awaitable has completed.</remarks>
            public bool GetResult()
            {
                if (_UpdatableIndex == SuccessIndex)
                    return true;
                else if (_UpdatableIndex == InterruptedIndex)
                    return false;
                else
                    throw new InvalidOperationException(
                        $"{nameof(Awaiter)}.{nameof(GetResult)}" +
                        $" must only be called immediately after the awaitable has completed.");
            }

            /// <summary>Did the <see cref="State"/> successfully pass the <see cref="NormalizedTime"/>?</summary>
            /// <remarks>This property must only be accessed immediately after the awaitable has completed.</remarks>
            public bool WasSuccessful
                => GetResult();

            /************************************************************************************************************************/

            /// <summary>Prepares this object to await the specified `state`.</summary>
            public void Initialize(AnimancerState state)
            {
                if (State != null)
                    throw new InvalidOperationException(
                        $"{nameof(Awaiter)}.{nameof(Initialize)}" +
                        $" was called on an object that already has an assigned state.");

                AnimancerUtilities.Assert(
                    _Continuation == null,
                    $"{nameof(Awaiter)}.{nameof(Initialize)}: Awaitable was not reset properly.");

                State = state;

                _UpdatableIndex = IUpdatable.List.NotInList;
            }

            /************************************************************************************************************************/

            /// <summary>Has this operation been completed?</summary>
            public bool IsCompleted
                => State == null;

            /************************************************************************************************************************/

            /// <summary>Returns <c>this</c>.</summary>
            public Awaiter GetAwaiter()
                => this;

            /************************************************************************************************************************/

            /// <summary>Sets the callback to invoke when the <see cref="IsApproachingTime"/> returns false.</summary>
            public void OnCompleted(Action continuation)
            {
                // If waiting, store the callback.

                if (State.IsApproachingTime(NormalizedTime))
                {
                    _Continuation += continuation;
                    State.Graph.RequirePostUpdate(this);
                    State.Graph.Disposables.Add(this);
                    return;
                }

                // If already complete, finish immediately.

                var success = !State.IsInterrupted;

                Reset();

                _UpdatableIndex = success
                    ? SuccessIndex
                    : InterruptedIndex;

                continuation();

                if (ReleaseOnComplete)
                    Pool.Instance.Release(this);
            }

            /************************************************************************************************************************/

            /// <summary>Unused.</summary>
            object IEnumerator.Current
                => null;

            /************************************************************************************************************************/

            /// <summary>
            /// Completes the awaitable and returns false if the <see cref="State"/>
            /// is no longer approaching the target <see cref="NormalizedTime"/>.
            /// </summary>
            public bool MoveNext()
            {
                if (Cancel.IsCancellationRequested ||
                    State.IsInterrupted)
                {
                    CompleteAndReset(false);
                    return false; // Done waiting.
                }
                else if (!State.IsApproachingTime(NormalizedTime))
                {
                    CompleteAndReset(true);
                    return false; // Done waiting.
                }

                return true; // Keep waiting.
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Completes the awaitable if the <see cref="State"/>
            /// is no longer approaching the target <see cref="NormalizedTime"/>.
            /// </summary>
            public void Update()
                => MoveNext();

            /************************************************************************************************************************/

            /// <summary>Reverts this object to its default values so it can be reused.</summary>
            public void Reset()
            {
                if (State != null)
                {
                    State.Graph.Disposables.Remove(this);
                    State.Graph.CancelPostUpdate(this);
                    State = null;
                }

                Cancel = default;
            }

            /************************************************************************************************************************/

            /// <summary>Sets the result of this operation and triggers its async continuation.</summary>
            public void CompleteAndReset(bool success)
            {
                Reset();

                _UpdatableIndex = success
                    ? SuccessIndex
                    : InterruptedIndex;

                if (_Continuation != null)
                {
                    var continuation = _Continuation;
                    _Continuation = null;
                    continuation.Invoke();
                }

                if (ReleaseOnComplete)
                    Pool.Instance.Release(this);
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            void IDisposable.Dispose()
                => CompleteAndReset(false);

            /************************************************************************************************************************/
        }
    }
}

