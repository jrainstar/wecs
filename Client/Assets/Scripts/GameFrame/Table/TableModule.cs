using GameConf;
using GameEngine;
using Luban.SimpleJSON;
using UnityEngine;

namespace GameLogic
{
    public class TableModule : GameModule<TableModule>
    {
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

        protected override void DynamicAwake()
        {
            base.DynamicAwake();
            transform.parent = GameModules.Instance.transform;
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
            TextAsset textAsset = ResourceModule.Instance.LoadAssetSync<TextAsset>(file);
            return JSON.Parse(textAsset.text);
        }
    }
}
