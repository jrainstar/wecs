using UnityEngine;

/// <summary>
/// 配合 AnimationMeshBaker 使用：把烘焙好的顶点位置 / 法线贴图（MeshAnimationData）
/// 交给 Custom/VertexAnimationShader，直接按顶点索引和时间采样顶点数据，
/// 完全不需要骨骼与蒙皮计算。
/// 注意：位置 / 法线贴图的 Wrap Mode 需要设置为 Repeat，否则循环会失效。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationMeshPlayer : MonoBehaviour
{
    [Header("烘焙数据")]
    [Tooltip("AnimationMeshBaker 生成的 *.MeshAnimationData.asset，包含位置贴图 / 法线贴图 / 时长")]
    public MeshAnimationData data;

    [Header("播放")]
    [Tooltip("随机初始相位，让多个实例不同步播放")]
    public bool randomStart = true;

    [Range(0f, 1f)] public float startOffset = 0f;

    [Header("材质")]
    [Tooltip("留空则使用 MeshRenderer 上当前的材质，需为 Custom/VertexAnimationShader")]
    public Material targetMaterial;

    [Tooltip("把位置 / 法线贴图写入材质。会修改共享材质，建议仅在运行时使用")]
    public bool applyStaticProperties = true;

    private Texture2D _positionTexture;
    private Texture2D _normalTexture;
    private float _clipLength = 1f;

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();

        if (data == null)
        {
            Debug.LogWarning(
                $"[{nameof(AnimationMeshPlayer)}] 未指定 MeshAnimationData，组件已禁用", this);
            enabled = false;
            return;
        }

        _positionTexture = data.positionTexture;
        _normalTexture = data.normalTexture;
        _clipLength = data.clipLength;

        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null && data.sourceMesh != null)
            meshFilter.sharedMesh = data.sourceMesh;

        if (applyStaticProperties)
            ApplyStaticProperties();

        WarnIfNotRepeatable(_positionTexture, nameof(data.positionTexture));
        WarnIfNotRepeatable(_normalTexture, nameof(data.normalTexture));
    }

    private void Start()
    {
        var normalized = randomStart ? Random.value : startOffset;
        PushToMaterialPropertyBlock(normalized);
    }

    /// <summary>
    /// 写入整批实例共用的材质属性：位置贴图与法线贴图。
    /// </summary>
    public void ApplyStaticProperties()
    {
        var mat = targetMaterial != null ? targetMaterial : _renderer.sharedMaterial;
        if (mat == null)
        {
            Debug.LogWarning(
                $"[{nameof(AnimationMeshPlayer)}] 未找到材质，请给 MeshRenderer 指定使用 Custom/VertexAnimationShader 的材质",
                this);
            return;
        }

        if (_positionTexture != null)
            mat.SetTexture("_PosTex", _positionTexture);

        if (_normalTexture != null)
            mat.SetTexture("_NmlTex", _normalTexture);
    }

    /// <summary>
    /// 写入每个实例独立的属性：动画时长与时间偏移（单位：秒）。
    /// </summary>
    private void PushToMaterialPropertyBlock(float normalizedOffset)
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_Length", _clipLength);
        _mpb.SetFloat("_Offset", Mathf.Repeat(normalizedOffset, 1f) * _clipLength);
        _renderer.SetPropertyBlock(_mpb);
    }

    private static void WarnIfNotRepeatable(Texture2D texture, string fieldName)
    {
        if (texture != null && texture.wrapMode != TextureWrapMode.Repeat)
        {
            Debug.LogWarning(
                $"[{nameof(AnimationMeshPlayer)}] {fieldName} 的 Wrap Mode 不是 Repeat，顶点动画无法循环播放");
        }
    }
}
