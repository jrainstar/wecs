using UnityEngine;

/// <summary>
/// 配合 AnimationBoneBaker 使用：把烘焙好的 BoneAnimationData（骨骼矩阵贴图 + 烘焙 Mesh + 播放参数）
/// 交给 Custom/BoneAnimationShader，在顶点着色器里完成蒙皮，运行时不再需要 Animator 和 SkinnedMeshRenderer。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimationBonePlayer : MonoBehaviour
{
    [Header("烘焙数据")]
    [Tooltip("AnimationBoneBaker 生成的 *.BoneAnimationData.asset，包含贴图 / Mesh / 骨骼数 / 帧数 / 帧率")]
    public BoneAnimationData data;

    [Header("播放")]
    [Tooltip("随机初始相位，让多个实例不同步播放")]
    public bool randomStart = true;

    [Range(0f, 1f)] public float startOffset = 0f;

    [Header("材质")]
    [Tooltip("留空则使用 MeshRenderer 上当前的材质，需为 Custom/BoneAnimationShader")]
    public Material targetMaterial;

    [Tooltip("把贴图 / 骨骼数 / 帧率写入材质。会修改共享材质，建议仅在运行时使用")]
    public bool applyStaticProperties = true;

    private Texture2D _boneMatrixTexture;
    private int _boneCount = 50;
    private int _frameCount = 30;
    private float _frameRate = 30f;
    private int _startRow = 0;

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();

        if (data == null)
        {
            Debug.LogWarning(
                $"[{nameof(AnimationBonePlayer)}] 未指定 BoneAnimationData，组件已禁用", this);
            enabled = false;
            return;
        }

        _boneMatrixTexture = data.boneMatrixTexture;
        _boneCount = data.boneCount;
        _frameCount = data.frameCount;
        _frameRate = data.frameRate;
        _startRow = data.startRow;

        if (data.bakedMesh != null)
            GetComponent<MeshFilter>().sharedMesh = data.bakedMesh;

        if (applyStaticProperties)
            ApplyStaticProperties();
    }

    private void Start()
    {
        PushToMaterialPropertyBlock(randomStart ? Random.value : startOffset);
    }

    /// <summary>
    /// 写入整批实例共用的材质属性：骨骼矩阵贴图、骨骼数、帧率。
    /// </summary>
    public void ApplyStaticProperties()
    {
        var mat = targetMaterial != null ? targetMaterial : _renderer.sharedMaterial;
        if (mat == null)
        {
            Debug.LogWarning(
                $"[{nameof(AnimationBonePlayer)}] 未找到材质，请给 MeshRenderer 指定使用 Custom/BoneAnimationShader 的材质",
                this);
            return;
        }

        if (_boneMatrixTexture != null)
            mat.SetTexture("_AnimTex", _boneMatrixTexture);

        mat.SetInt("_BoneCount", _boneCount);
        mat.SetFloat("_FrameRate", _frameRate);
    }

    /// <summary>
    /// 写入每个实例独立的属性：播放相位、动画起始行、总帧数。
    /// </summary>
    private void PushToMaterialPropertyBlock(float normalizedOffset)
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_Offset", Mathf.Repeat(normalizedOffset, 1f));
        _mpb.SetFloat("_Start", _startRow);
        _mpb.SetFloat("_FrameCount", _frameCount);
        _renderer.SetPropertyBlock(_mpb);
    }
}
