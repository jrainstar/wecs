using GameLogic.Gameplay.Common;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 编辑器资源加载器
    /// </summary>
    public class EditorRes
    {
        /// <summary>
        /// 同步 Model 加载预制体
        /// </summary>
        /// <param name="path">资源路径 (完整路径, 见 TbResourceData)</param>
        /// <returns>Model 预制体, 资源不存在返回 null</returns>
        public static GameObject LoadModel(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (null == prefab) return null;

            return GameObject.Instantiate(prefab);
        }

        /// <summary>
        /// 加载模型配置
        /// </summary>
        /// <param name="res">资源地址</param>
        /// <returns>AnimationConfig</returns>
        public static AnimationConfig LoadAnimationConfig(string res)
        {
            return ScriptableObject.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationConfig>(Location.animcfgpath + res + ".asset"));
        }
    }
}