using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

internal struct VertexInfo
{
    public Vector3 position;
    public Vector3 normal;
}

public class AnimationMeshBaker : MonoBehaviour
{
    public Button button;
    public ComputeShader computeShader;

    [Tooltip("烘焙产物输出目录，必须位于 Assets 下")]
    public string outputFolder = "Assets/Bake/Mesh";

#if UNITY_EDITOR
    private void Start()
    {
        button = GameObject.Find("Record").GetComponent<Button>();
        button.onClick.AddListener(onClick);
    }

    private void onClick()
    {
        StartCoroutine(BakeMesh());
    }

    private IEnumerator BakeMesh()
    {
        var animator = GetComponent<Animator>();
        var clips = animator.runtimeAnimatorController.animationClips;
        var skin = GetComponentInChildren<SkinnedMeshRenderer>();
        var vertCount = skin.sharedMesh.vertexCount;

        var mesh = new Mesh();
        animator.speed = 1;
        var textWidth = vertCount;
        EnsureFolder(outputFolder);
        foreach (var clip in clips)
        {
            var frameCount = (int) (clip.length * clip.frameRate);
            Debug.Log("Frames:" + clip.length * clip.frameRate);
            var vertexs = new List<VertexInfo>();

            // Linear 读写：顶点数据是原始数值，不能被当成颜色做 gamma / sRGB 转换
            var positionRenderTexture = new RenderTexture(textWidth, frameCount, 0, RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear);
            var normalRenderTexture = new RenderTexture(textWidth, frameCount, 0, RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear);

            positionRenderTexture.name = string.Format("{0}.{1}.position", name, clip.name);
            normalRenderTexture.name = string.Format("{0}.{1}.normal", name, clip.name);

            foreach (var renderTexture in new[] {positionRenderTexture, normalRenderTexture})
            {
                renderTexture.enableRandomWrite = true;
                renderTexture.Create();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.clear);
            }

            animator.Play(clip.name);
            yield return 0;

            for (var i = 0; i < frameCount; ++i)
            {
                animator.Play(clip.name, 0, (float) i / frameCount);
                yield return 0;
                skin.BakeMesh(mesh);
                vertexs.AddRange(Enumerable.Range(0, vertCount).Select(idx => new VertexInfo
                {
                    position = mesh.vertices[idx],
                    normal = mesh.normals[idx]
                }));
            }

            var buffer = new ComputeBuffer(vertexs.Count, Marshal.SizeOf(typeof(VertexInfo)));
            buffer.SetData(vertexs);

            var kernelID = computeShader.FindKernel("CSMain");

            computeShader.SetInt("vertCount", vertCount);
            computeShader.SetBuffer(kernelID, "meshInfo", buffer);
            computeShader.SetTexture(kernelID, "OutPositionTexture", positionRenderTexture);
            computeShader.SetTexture(kernelID, "OutNormalTexture", normalRenderTexture);

            computeShader.Dispatch(kernelID, vertCount, frameCount, 1);
            buffer.Release();

            var pt = Convert(positionRenderTexture);
            var nt = Convert(normalRenderTexture);

            Graphics.CopyTexture(positionRenderTexture, pt);
            Graphics.CopyTexture(normalRenderTexture, nt);

            AssetDatabase.CreateAsset(pt, Path.Combine(outputFolder, positionRenderTexture.name + ".asset"));
            AssetDatabase.CreateAsset(nt, Path.Combine(outputFolder, normalRenderTexture.name + ".asset"));

            SaveAsJPG(pt, outputFolder, positionRenderTexture.name);
            SaveAsJPG(nt, outputFolder, normalRenderTexture.name);

            CreateAnimationData(pt, nt, skin.sharedMesh, clip, frameCount);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(string.Format("[{0}] 烘焙完成：clip={1}\tverts={2}\tframes={3}", name, clip.name, vertCount,
                frameCount), this);
        }

        Debug.Log(string.Format("[{0}] 全部 {1} 个 clip 烘焙完成，输出目录：{2}", name, clips.Length, outputFolder), this);

        yield return null;
    }

    /// <summary>
    /// 生成 MeshAnimationData：把位置 / 法线贴图和播放参数打包成一个资产，
    /// AnimationMeshPlayer 只需引用它，不用再逐个填时长等参数。
    /// </summary>
    private MeshAnimationData CreateAnimationData(Texture2D positionTex, Texture2D normalTex, Mesh sourceMesh,
        AnimationClip clip, int frameCount)
    {
        var data = ScriptableObject.CreateInstance<MeshAnimationData>();
        data.characterName = name;
        data.clipName = clip.name;
        data.positionTexture = positionTex;
        data.normalTexture = normalTex;
        data.sourceMesh = sourceMesh;
        data.clipLength = clip.length;
        data.frameCount = frameCount;
        data.frameRate = clip.frameRate;

        var dataName = string.Format("{0}.{1}.MeshAnimationData", name, clip.name);
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

    public static Texture2D Convert(RenderTexture rt)
    {
        // linear:true —— 顶点数据是原始数值，不能被当成 sRGB 颜色采样
        var texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false, true);
        RenderTexture.active = rt;
        texture2D.ReadPixels(Rect.MinMaxRect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;
        return texture2D;
    }

    public static void SaveAsJPG(Texture2D texture2D, string contents, string pngName)
    {
        var bytes = texture2D.EncodeToJPG();
        var file = File.Open(contents + "/" + pngName + ".jpg", FileMode.Create);
        var writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        writer.Close();
    }
}