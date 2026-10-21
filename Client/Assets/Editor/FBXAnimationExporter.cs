using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Animations;

public class FBXAnimationExporter : EditorWindow
{
    private DefaultAsset sourceFolder;
    private DefaultAsset targetFolder;
    private Vector2 scrollPosition;

    private bool createAnimController = false;
    private bool includeFBXName = true; // 是否包含FBX文件名

    private Dictionary<string, bool> selectedFiles = new Dictionary<string, bool>();
    private List<string> fbxFiles = new List<string>();
    private Dictionary<string, List<string>> fbxAnimations = new Dictionary<string, List<string>>();
    private bool isScanning = false;

    [MenuItem("Tools/FBX动画导出器")]
    public static void ShowWindow()
    {
        GetWindow<FBXAnimationExporter>("FBX动画导出器");
    }

    private void OnGUI()
    {
        GUILayout.Label("FBX动画批量导出工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 源文件夹选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("源文件夹:", GUILayout.Width(80));
        sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(sourceFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("扫描FBX", GUILayout.Width(80)))
        {
            ScanFBXFiles();
        }
        EditorGUILayout.EndHorizontal();

        // 目标文件夹选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("目标文件夹:", GUILayout.Width(80));
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(targetFolder, typeof(DefaultAsset), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 导出设置
        GUILayout.Label("导出设置:", EditorStyles.boldLabel);
        createAnimController = EditorGUILayout.Toggle("创建动画控制器", createAnimController);
        includeFBXName = EditorGUILayout.Toggle("文件名包含FBX名称", includeFBXName);

        // 显示文件名预览
        EditorGUILayout.Space(5);
        GUILayout.Label("文件名格式预览:", EditorStyles.boldLabel);
        if (includeFBXName)
            GUILayout.Label("FBX名称_动画名称.anim", EditorStyles.miniLabel);
        else
            GUILayout.Label("动画名称.anim", EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        // 扫描状态和文件列表
        if (isScanning)
        {
            EditorGUILayout.LabelField("正在扫描FBX文件...");
        }
        else if (fbxFiles.Count > 0)
        {
            GUILayout.Label($"找到 {fbxFiles.Count} 个FBX文件:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            // 全选按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                SetAllSelected(true);
            }
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                SetAllSelected(false);
            }
            EditorGUILayout.EndHorizontal();

            // 文件列表
            foreach (string file in fbxFiles)
            {
                if (!selectedFiles.ContainsKey(file))
                    selectedFiles[file] = true;

                EditorGUILayout.BeginHorizontal();
                selectedFiles[file] = EditorGUILayout.Toggle(selectedFiles[file], GUILayout.Width(20));
                EditorGUILayout.LabelField(Path.GetFileName(file));

                // 显示该FBX中的动画数量
                if (fbxAnimations.ContainsKey(file) && fbxAnimations[file].Count > 0)
                {
                    GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
                    style.normal.textColor = Color.green;
                    EditorGUILayout.LabelField($"({fbxAnimations[file].Count} 个动画)", style, GUILayout.Width(80));
                }
                else
                {
                    GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
                    style.normal.textColor = Color.yellow;
                    EditorGUILayout.LabelField("(无动画)", style, GUILayout.Width(60));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(10);

        // 导出按钮
        GUI.enabled = sourceFolder != null && targetFolder != null && fbxFiles.Count > 0;

        if (GUILayout.Button("开始导出动画", GUILayout.Height(40)))
        {
            ExportAnimations();
        }

        GUI.enabled = true;
    }

    private void ScanFBXFiles()
    {
        if (sourceFolder == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择源文件夹", "确定");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(sourceFolder);
        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog("错误", "源文件夹不存在", "确定");
            return;
        }

        isScanning = true;
        fbxFiles.Clear();
        selectedFiles.Clear();
        fbxAnimations.Clear();

        // 递归查找所有FBX文件
        string[] allFiles = Directory.GetFiles(folderPath, "*.fbx", SearchOption.AllDirectories);
        foreach (string file in allFiles)
        {
            string relativePath = file.Replace("\\", "/");
            fbxFiles.Add(relativePath);

            // 获取FBX内部的动画信息
            List<string> animationNames = GetFBXAnimations(relativePath);
            fbxAnimations[relativePath] = animationNames;
        }

        // 设置默认选中状态
        foreach (string file in fbxFiles)
        {
            selectedFiles[file] = true;
        }

        isScanning = false;
        Repaint();

        Debug.Log($"扫描完成，找到 {fbxFiles.Count} 个FBX文件");
    }

    private List<string> GetFBXAnimations(string fbxPath)
    {
        List<string> animations = new List<string>();

        // 加载FBX资源
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
            return animations;

        // 获取该FBX的所有子资源（包括动画）
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object subAsset in subAssets)
        {
            if (subAsset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                animations.Add(clip.name);
            }
        }

        return animations;
    }

    private void SetAllSelected(bool selected)
    {
        List<string> keys = new List<string>(selectedFiles.Keys);
        foreach (string key in keys)
        {
            selectedFiles[key] = selected;
        }
    }

    private void ExportAnimations()
    {
        if (targetFolder == null)
        {
            EditorUtility.DisplayDialog("提示", "请选择目标文件夹", "确定");
            return;
        }

        string targetPath = AssetDatabase.GetAssetPath(targetFolder);
        int exportedCount = 0;
        int totalSelected = 0;

        // 计算选中的文件数
        foreach (var pair in selectedFiles)
        {
            if (pair.Value)
                totalSelected++;
        }

        if (totalSelected == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有选择任何文件", "确定");
            return;
        }

        // 开始导出
        for (int i = 0; i < fbxFiles.Count; i++)
        {
            string fbxPath = fbxFiles[i];

            if (!selectedFiles.ContainsKey(fbxPath) || !selectedFiles[fbxPath])
                continue;

            // 显示进度条
            float progress = (float)i / fbxFiles.Count;
            if (EditorUtility.DisplayCancelableProgressBar("导出动画", $"正在处理: {Path.GetFileName(fbxPath)}", progress))
            {
                break;
            }

            try
            {
                bool success = ExportSingleFBXAnimations(fbxPath, targetPath);
                if (success)
                    exportedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"导出失败: {fbxPath}\n错误: {e.Message}");
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"成功导出 {exportedCount} 个FBX文件的动画", "确定");
    }

    private bool ExportSingleFBXAnimations(string fbxPath, string targetFolderPath)
    {
        // 加载FBX资源
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogWarning($"无法加载FBX: {fbxPath}");
            return false;
        }

        // 获取文件信息用于命名
        string fileName = Path.GetFileNameWithoutExtension(fbxPath);
        string relativePath = GetRelativePath(fbxPath, targetFolderPath);

        // 创建目标子文件夹
        string targetSubFolder = Path.Combine(targetFolderPath, relativePath);
        if (!Directory.Exists(targetSubFolder))
        {
            Directory.CreateDirectory(targetSubFolder);
        }

        // 获取FBX内部的所有动画剪辑
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        List<AnimationClip> clips = new List<AnimationClip>();

        foreach (Object subAsset in subAssets)
        {
            if (subAsset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                clips.Add(clip);
            }
        }

        if (clips.Count == 0)
        {
            Debug.LogWarning($"FBX文件中没有找到动画: {fbxPath}");
            return false;
        }

        Debug.Log($"找到 {clips.Count} 个动画在 {fileName}");

        // 导出动画剪辑
        AnimatorController controller = null;

        if (createAnimController)
        {
            // 创建动画控制器
            string controllerPath = Path.Combine(targetSubFolder, $"{fileName}_Controller.controller");
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        int exportedClipCount = 0;

        foreach (AnimationClip originalClip in clips)
        {
            // 创建新的动画剪辑并复制原始数据
            AnimationClip newClip = new AnimationClip();
            EditorUtility.CopySerialized(originalClip, newClip);

            // 保持原始名称
            newClip.name = originalClip.name;

            // 根据选项生成文件名
            string clipFileName;
            if (includeFBXName)
            {
                clipFileName = $"{fileName}_{originalClip.name}.anim";
            }
            else
            {
                clipFileName = $"{originalClip.name}.anim";
            }

            string clipPath = Path.Combine(targetSubFolder, clipFileName);

            // 检查文件是否已存在
            if (File.Exists(clipPath))
            {
                if (!EditorUtility.DisplayDialog("文件已存在",
                    $"文件 {clipFileName} 已存在，是否覆盖？",
                    "覆盖", "跳过"))
                {
                    continue;
                }
            }

            // 创建并保存动画资源
            AssetDatabase.CreateAsset(newClip, clipPath);

            if (controller != null)
            {
                // 添加到动画控制器
                AnimationClip savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (savedClip != null)
                {
                    controller.AddMotion(savedClip);
                }
            }

            exportedClipCount++;
            Debug.Log($"导出动画: {clipFileName}");
        }

        Debug.Log($"成功从 {fileName} 导出 {exportedClipCount} 个动画");
        AssetDatabase.SaveAssets();

        return exportedClipCount > 0;
    }

    private string GetRelativePath(string fullPath, string targetFolder)
    {
        // 获取源文件夹的相对路径
        string sourceFolderPath = AssetDatabase.GetAssetPath(sourceFolder);

        if (fullPath.StartsWith(sourceFolderPath))
        {
            string relative = fullPath.Substring(sourceFolderPath.Length);
            relative = relative.TrimStart('/');
            relative = Path.GetDirectoryName(relative);
            return relative;
        }

        return "";
    }
}