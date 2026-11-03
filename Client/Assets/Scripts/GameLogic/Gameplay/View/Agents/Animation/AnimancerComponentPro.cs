using Animancer;
using UnityEngine;

/// <summary>
/// 基于 Animancer 的“根骨骼锁定”播放器。
/// 用法：把本组件挂在原本挂 AnimancerComponent 的物体上（替换掉它），Animator 上不要拖 Controller。
/// 播放方式完全一样：Play(clip) / TryPlay("名字")，额外可以按次决定是否锁住根骨骼：
///     Play(attack1Clip, lockRoot: true);
///     Play(attack1Clip, lockRoot: false);
///     PlayNamed("Attack1", lockRoot: false);
/// 也可以直接改 LockRoot 属性来临时开关。
/// </summary>
[AddComponentMenu("Gameplay/AnimancerComponentPro")]
public class AnimancerComponentPro : NamedAnimancerComponent
{
    [Tooltip("要锁定的根骨骼")]
    [SerializeField] private Transform _RootBone;

    [Tooltip("默认播放时是否锁住根骨骼")]
    [SerializeField] private bool _DefaultLockRoot = true;

    private Vector3 _InitPosition;
    private Quaternion _InitRotation;
    private Vector3 _InitScale;
    private bool _HasInitPose;

    /// <summary>当前帧是否锁住根骨骼。</summary>
    public bool LockRoot { get; set; }

    public Transform RootBone
    {
        get => _RootBone;
        set
        {
            _RootBone = value;
            _HasInitPose = false;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        LockRoot = _DefaultLockRoot;

        // 记录初始姿态，作为锁定的还原目标
        if (_RootBone != null)
        {
            _InitPosition = _RootBone.localPosition;
            _InitRotation = _RootBone.localRotation;
            _InitScale = _RootBone.localScale;
            _HasInitPose = true;
        }
    }

    private void LateUpdate()
    {
        if (!LockRoot || _RootBone == null)
            return;

        if (!_HasInitPose)
        {
            _InitPosition = _RootBone.localPosition;
            _InitRotation = _RootBone.localRotation;
            _InitScale = _RootBone.localScale;
            _HasInitPose = true;
        }

        _RootBone.localPosition = _InitPosition;
        _RootBone.localRotation = _InitRotation;
        _RootBone.localScale = _InitScale;
    }

    // 播放时指定是否锁定（本次播放期间生效，直到下次调用） //
    public AnimancerState PlayNamed(object key, float fadeDuration, bool lockRoot)
    {
        LockRoot = lockRoot;
        return TryPlay(key, fadeDuration);
    }

    public AnimancerState PlayNamed(object key, bool lockRoot)
    {
        LockRoot = lockRoot;
        return TryPlay(key);
    }

    public AnimancerState Play(AnimationClip clip, bool lockRoot)
    {
        LockRoot = lockRoot;
        return base.Play(clip);
    }

    public AnimancerState Play(AnimancerState state, bool lockRoot)
    {
        LockRoot = lockRoot;
        return base.Play(state);
    }

    public AnimancerState Play(ITransition transition, bool lockRoot)
    {
        LockRoot = lockRoot;
        return base.Play(transition);
    }
}
