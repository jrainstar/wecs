using UnityEngine;

/// <summary>
/// AnimationMeshBaker 的烘焙产物：顶点位置贴图（*.position.asset）+ 顶点法线贴图（*.normal.asset），
/// 再带上播放所需的动画时长、帧数等信息。
/// 直接拖给 AnimationMeshPlayer 的 data 字段即可，不用再逐个手填。
/// </summary>
[CreateAssetMenu(fileName = "NewMeshAnimationData", menuName = "GPU Animation/Mesh Animation Data", order = 1)]
public class MeshAnimationData : ScriptableObject
{
    [Header("烘焙产物")]
    [Tooltip("顶点位置贴图，AnimationMeshBaker 生成")]
    public Texture2D positionTexture;

    [Tooltip("顶点法线贴图，AnimationMeshBaker 生成")]
    public Texture2D normalTexture;

    [Tooltip("可选：烘焙时使用的网格，Player 的 MeshFilter 为空时会自动填上")]
    public Mesh sourceMesh;

    [Header("播放参数")]
    [Tooltip("动画时长（秒），对应 AnimationClip.length")]
    [Min(0.01f)] public float clipLength = 1f;

    [Header("信息")]
    [Tooltip("贴图高度方向采样的帧数")]
    [Min(1)] public int frameCount = 30;

    [Min(0.01f)] public float frameRate = 30f;

    public string characterName;
    public string clipName;

    private void OnValidate()
    {
        clipLength = Mathf.Max(0.01f, clipLength);
        frameCount = Mathf.Max(1, frameCount);
        frameRate = Mathf.Max(0.01f, frameRate);
    }
}
