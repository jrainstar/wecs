using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Editor
{
    /// <summary>
    /// 快速打开 Application.persistentDataPath 目录的编辑器扩展
    /// </summary>
    public static class OpenPersistentDataPath
    {
        [MenuItem("Tools/打开存档目录 &p", false, 1000)]
        public static void Open()
        {
            var path = Application.persistentDataPath;
            // 确保目录存在
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            // 在系统文件管理器中打开
            Process.Start("explorer.exe", path);

            // 输出到 Console 方便查看/复制
            UnityEngine.Debug.Log($"persistentDataPath: {path}");
        }
    }
}
