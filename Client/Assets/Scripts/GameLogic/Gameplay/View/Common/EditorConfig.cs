#if UNITY_EDITOR
using GameConf;
using Luban.SimpleJSON;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 编辑器配置
    /// </summary>
    [InitializeOnLoad]
    public class EditorConfig
    {
        /// <summary>
        /// 配置表定位器
        /// </summary>
        public static Tables location { get; private set; }

        static EditorConfig()
        {
            location = new Tables(LoadByteBuf);
        }

        static JSONNode LoadByteBuf(string file)
        {
            return JSON.Parse(File.ReadAllText($"{Application.dataPath}/AssetRaws/Cfgs"));
        }
    }
}
#endif