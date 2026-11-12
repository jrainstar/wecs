using GameConf;
using GameFrame;
using Luban.SimpleJSON;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 配置表管理器，负责 Luban 生成配置的加载与访问。
    /// </summary>
    public class ConfigManager : GameModule<ConfigManager>
    {
        /// <summary>配置表资源目录</summary>
        private const string CONFIG_PATH = "Assets/AssetRaws/Configs/";

        private bool m_Init = false;

        private Tables m_Tables;

        public Tables Tables
        {
            get
            {
                if (!m_Init)
                {
                    Load();
                }

                return m_Tables;
            }
        }

        /// <summary>
        /// 加载配置。
        /// </summary>
        private void Load()
        {
            m_Tables = new Tables(LoadByteBuf);
            m_Init = true;
        }

        private JSONNode LoadByteBuf(string file)
        {
            TextAsset textAsset = ResourceModule.Instance.LoadAssetSync<TextAsset>($"{CONFIG_PATH}{file}");
            return JSON.Parse(textAsset.text);
        }
    }
}
