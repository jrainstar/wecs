using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum UVChannel
{
    UV,
    UV2,
    UV3,
    UV4,
    UV5,
    UV6,
    UV7,
    UV8
}


public class AnimationBoneBaker : MonoBehaviour
{
    public Button button;
    public Mesh mesh;

    [Tooltip("烘焙产物输出目录，必须位于 Assets 下")]
    public string outputFolder = "Assets/Bake/Bone";

#if UNITY_EDITOR
    private void Start()
    {
        button = GameObject.Find("Record").GetComponent<Button>();
        button.onClick.AddListener(onClick);
    }

    private void onClick()
    {
        StartCoroutine(BakeBone());
    }

    private IEnumerator BakeBone()
    {
        var animator = GetComponent<Animator>();
        var clips = animator.runtimeAnimatorController.animationClips;
        var skin = GetComponentInChildren<SkinnedMeshRenderer>();
        var boneCount = mesh.bindposes.Length;

        animator.speed = 1;
        EnsureFolder(outputFolder);

        // 骨骼索引 / 权重只与模型有关，与 clip 无关，整个角色共用一份 Mesh
        var bakedMesh = Instantiate(mesh);
        bakedMesh.name = string.Format("{0}.mesh", name);
        MappingBoneIndexAndWeightToMeshUV(bakedMesh, UVChannel.UV2, UVChannel.UV3);
        AssetDatabase.CreateAsset(bakedMesh, Path.Combine(outputFolder, bakedMesh.name + ".mesh"));

        foreach (var clip in clips)
        {
            var frameCount = (int) (clip.length * clip.frameRate);

            yield return 0;

            var boneTex = CreateBoneTex(animator, skin, clip, mesh, 512, frameCount);
            if (boneTex == null)
                continue;
            Debug.Log("BoneCount:" + boneCount + "\tFrameCount:" + frameCount + "\tFrameRate:" + clip.frameRate);

            boneTex.name = string.Format("{0}.{1}.BoneMatrix", name, clip.name);
            SaveAsJPG(boneTex, outputFolder, boneTex.name);
            AssetDatabase.CreateAsset(boneTex, Path.Combine(outputFolder, boneTex.name + ".asset"));

            CreateAnimationData(boneTex, bakedMesh, clip, boneCount, frameCount);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// 生成 BoneAnimationData：把贴图、Mesh 和播放参数打包成一个资产，
    /// AnimationBonePlayer 只需引用它，不用再逐个填骨骼数 / 帧数 / 帧率。
    /// </summary>
    private BoneAnimationData CreateAnimationData(Texture2D boneTex, Mesh bakedMesh, AnimationClip clip,
        int boneCount, int frameCount)
    {
        var data = ScriptableObject.CreateInstance<BoneAnimationData>();
        data.characterName = name;
        data.clipName = clip.name;
        data.boneMatrixTexture = boneTex;
        data.bakedMesh = bakedMesh;
        data.boneCount = boneCount;
        data.frameCount = frameCount;
        data.frameRate = clip.frameRate;
        data.startRow = 0;

        var dataName = string.Format("{0}.{1}.BoneAnimationData", name, clip.name);
        data.name = dataName;
        AssetDatabase.CreateAsset(data, Path.Combine(outputFolder, dataName + ".asset"));
        return data;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder))
            return;

        folder = folder.Replace('\\', '/').TrimEnd('/');
        if (folder != "Assets" && !folder.StartsWith("Assets/"))
            folder = "Assets/" + folder;

        if (AssetDatabase.IsValidFolder(folder))
            return;

        var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
#endif


    public static void SaveAsJPG(Texture2D texture2D, string contents, string pngName)
    {
        var bytes = texture2D.EncodeToJPG();
        var file = File.Open(contents + "/" + pngName + ".jpg", FileMode.Create);
        var writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        writer.Close();
    }

    private Texture2D CreateBoneTex(Animator animator, SkinnedMeshRenderer skinnedMeshRenderer, AnimationClip clip,
        Mesh mesh,
        int width, int animFrameCount)
    {
        // 绑定姿势。每个索引处的绑定姿势对应相同索引的骨骼。
        // 是每一根骨骼的逆矩阵（世界的而不是父骨骼的）
        var bindPoses = mesh.bindposes;
        var bones = skinnedMeshRenderer.bones;

        var bonesCount = bones.Length;
        if (bindPoses.Length != bones.Length)
            return null;
        animator.applyRootMotion = false;
        animator.Play(clip.name);

        // 开始采样
        // 每一根骨骼、每一帧占 12 个 float（= 12 个像素），即矩阵的前 3 行 × 4 分量；第 4 行 (0,0,0,1) 不存，由着色器补——仿射矩阵第 4 行恒为它，省 25% 带宽。
        // 算这张贴图需要多少行像素（高度）
        // 像素是一维流式铺开的，宽度固定 512，高度由数据量决定：
        var lines = Mathf.CeilToInt((float) bones.Length * animFrameCount * 12 / width);
        // 最后一维 linear:true 必须为 true：骨骼矩阵是按位编码在 RGBA 里的，
        // 一旦被当成 sRGB 纹理，Linear 色彩空间下采样时会做 sRGB->线性 解码，数据直接被破坏
        var result = new Texture2D(width, lines, TextureFormat.RGBA32, false, true);
        result.filterMode = FilterMode.Point;
        result.wrapMode = TextureWrapMode.Clamp;
        var colors = new Color[width * lines * 3];
        //List<Color> colors = new List<Color>();
        // 逐帧写入矩阵
        for (var i = 0; i <= animFrameCount; i++)
        {
            clip.SampleAnimation(gameObject, i / clip.frameRate);


            // 写入变换后的矩阵
            for (var j = 0; j < bonesCount; j++)
            {
                var matrix = transform.worldToLocalMatrix * bones[j].localToWorldMatrix * bindPoses[j];
                //Debug.Log(matrix.ToString());
                //Matrix4x4 matrix = bones[j].localToWorldMatrix * bindPoses[j];

                colors[(i * bonesCount + j) * 12 + 0] = EncodeFloatRGBA(matrix.m00);
                colors[(i * bonesCount + j) * 12 + 1] = EncodeFloatRGBA(matrix.m01);
                colors[(i * bonesCount + j) * 12 + 2] = EncodeFloatRGBA(matrix.m02);
                colors[(i * bonesCount + j) * 12 + 3] = EncodeFloatRGBA(matrix.m03);

                colors[(i * bonesCount + j) * 12 + 4] = EncodeFloatRGBA(matrix.m10);
                colors[(i * bonesCount + j) * 12 + 5] = EncodeFloatRGBA(matrix.m11);
                colors[(i * bonesCount + j) * 12 + 6] = EncodeFloatRGBA(matrix.m12);
                colors[(i * bonesCount + j) * 12 + 7] = EncodeFloatRGBA(matrix.m13);

                colors[(i * bonesCount + j) * 12 + 8] = EncodeFloatRGBA(matrix.m20);
                colors[(i * bonesCount + j) * 12 + 9] = EncodeFloatRGBA(matrix.m21);
                colors[(i * bonesCount + j) * 12 + 10] = EncodeFloatRGBA(matrix.m22);
                colors[(i * bonesCount + j) * 12 + 11] = EncodeFloatRGBA(matrix.m23);
            }
        }

        //从左到右，从下到上
        result.SetPixels(colors);
        result.Apply();

        return result;
    }

    private static Vector4 EncodeFloatRGBA(float v)
    {
        v = v * 0.01f + 0.5f;
        var kEncodeMul = new Vector4(1.0f, 255.0f, 65025.0f, 160581375.0f);
        var kEncodeBit = 1.0f / 255.0f;
        var enc = kEncodeMul * v;
        for (var i = 0; i < 4; i++)
            enc[i] = enc[i] - Mathf.Floor(enc[i]);
        enc = enc - new Vector4(enc.y, enc.z, enc.w, enc.w) * kEncodeBit;
        return enc;
    }

    public static bool MappingBoneIndexAndWeightToMeshUV(Mesh mesh, UVChannel indexChannel, UVChannel weightChannel)
    {
        var boneWeights = mesh.boneWeights;

        var indexUV = new List<Vector2>(boneWeights.Length);
        var weightUV = new List<Vector2>(boneWeights.Length);


        for (var i = 0; i < boneWeights.Length; i++)
        {
            var bw = boneWeights[i];
            weightUV.Add(new Vector2(bw.weight0, bw.weight1));
            indexUV.Add(new Vector2(bw.boneIndex0, bw.boneIndex1));
        }

        mesh.SetUVs((int)indexChannel, indexUV);
        mesh.SetUVs((int) weightChannel, weightUV);

        return true;
    }
}