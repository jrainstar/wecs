using UnityEngine;

/// <summary>
/// AnimationBoneBaker 的烘焙产物：一张骨骼矩阵贴图（*.BoneMatrix.asset）
/// 加一个写入了骨骼索引 / 权重的 Mesh（*.mesh），
/// 再带上播放所需的骨骼数、帧数、帧率等参数。
/// 直接拖给 AnimationBonePlayer 的 data 字段即可，不用再逐个手填。
/// </summary>
[CreateAssetMenu(fileName = "NewBoneAnimationData", menuName = "GPU Animation/Bone Animation Data", order = 0)]
public class BoneAnimationData : ScriptableObject
{
    [Header("烘焙产物")]
    [Tooltip("骨骼矩阵贴图，AnimationBoneBaker 生成")]
    public Texture2D boneMatrixTexture;

    [Tooltip("已写入 UV2=骨骼索引 / UV3=权重的 Mesh")]
    public Mesh bakedMesh;

    [Header("播放参数")]
    [Min(1)] public int boneCount = 50;

    [Tooltip("单个 clip 的帧数（不含 0 帧那一行）")]
    [Min(1)] public int frameCount = 30;

    [Min(0.01f)] public float frameRate = 30f;

    [Tooltip("使用合并后的 *.ALL.asset 时，当前 clip 的起始行；单个 clip 的贴图填 0")]
    [Min(0)] public int startRow = 0;

    [Header("信息")]
    public string characterName;
    public string clipName;

    /// <summary>动画时长（秒）。</summary>
    public float Length => frameCount / frameRate;

    private void OnValidate()
    {
        boneCount = Mathf.Max(1, boneCount);
        frameCount = Mathf.Max(1, frameCount);
        frameRate = Mathf.Max(0.01f, frameRate);
        startRow = Mathf.Max(0, startRow);
    }
}
