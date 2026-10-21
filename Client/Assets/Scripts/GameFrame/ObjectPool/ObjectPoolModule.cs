using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using GameCore;

namespace GameEngine
{
    public class ObjectPoolModule : GameModule<ObjectPoolModule>
    {
        private GameObject m_Root;
        private Dictionary<string, ObjectPoolData> m_GameObjectPoolDic = new Dictionary<string, ObjectPoolData>();

        protected override void Awake()
        {
            base.Awake();
            m_Root = gameObject;
        }

        public GameObject GetGameObject(string assetName)
        {
            GameObject obj = null;
            if (m_GameObjectPoolDic.TryGetValue(assetName, out var gameObjectPoolData) && gameObjectPoolData.m_Pool.Count > 0)
            {
                obj = gameObjectPoolData.GetGameObject();
            }

            if (obj == null)
            {
                obj = LoadGameObject(assetName);
                obj.name = assetName;
            }
            obj.transform.parent = null;
            return obj;
        }

        public async UniTask<GameObject> GetGameObjectAsync(string assetName, Transform parent = null)
        {
            GameObject obj = null;
            if (m_GameObjectPoolDic.TryGetValue(assetName, out var gameObjectPoolData) && gameObjectPoolData.m_Pool.Count > 0)
            {
                obj = gameObjectPoolData.GetGameObject();
            }

            if (obj == null)
            {
                obj = await LoadGameObjectAsync(assetName, parent: parent);
                obj.name = assetName;
            }
            obj.transform.parent = null;
            return obj;
        }

        /// <summary>
        /// 同步加载游戏物体并实例化。
        /// </summary>
        /// <param name="assetName">资源定位地址。</param>
        /// <param name="parent">资源实例父节点。</param>
        /// <returns>异步游戏物体实例。</returns>
        private GameObject LoadGameObject(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error("Asset name is invalid.");
                return null;
            }
            var assetObject = ResourceModule.Instance.LoadAssetSync<GameObject>(assetName);
            var gameObject = GameObject.Instantiate(assetObject);
            gameObject.name = assetName;
            return gameObject;
        }

        /// <summary>
        /// 异步加载游戏物体并实例化。
        /// </summary>
        /// <param name="assetName">资源定位地址。</param>
        /// <param name="parent">资源实例父节点。</param>
        /// <returns>异步游戏物体实例。</returns>
        private async UniTask<GameObject> LoadGameObjectAsync(string assetName, Transform parent = null)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error("Asset name is invalid.");
                return null;
            }
            var assetObject = await ResourceModule.Instance.LoadAssetAsync<GameObject>(assetName);
            var gameObject = GameObject.Instantiate(assetObject, parent);
            gameObject.name = assetName;
            return gameObject;
        }

        public void PushGameObject(GameObject obj)
        {
            string objName = obj.name;
            if (m_GameObjectPoolDic.TryGetValue(objName, out var gameObjectPoolData))
            {
                gameObjectPoolData.PushGameObject(obj);
            }
            else
            {
                m_GameObjectPoolDic.Add(objName, new ObjectPoolData(obj, m_Root));
            }
        }

        public void Clear(bool clearGameObject = true)
        {
            if (clearGameObject)
            {
                for (int index = 0; index < m_Root.transform.childCount; ++index)
                {
                    GameObject.Destroy(m_Root.transform.GetChild(index).gameObject);
                }

                m_GameObjectPoolDic.Clear();
            }
        }

        public void ClearAllGameObject() => Clear();

        public void ClearGameObject(string prefabName)
        {
            GameObject obj = m_Root.transform.Find(prefabName).gameObject;
            if (obj == null)
            {
                return;
            }

            GameObject.Destroy(obj);
            m_GameObjectPoolDic.Remove(prefabName);
        }

        public void ClearGameObject(GameObject prefab) => ClearGameObject(prefab.name);

        internal void Clear()
        {
            ClearAllGameObject();
        }
    }
}
