using UnityEngine;
using GameCore;

namespace GameEngine
{
    public class AssetReference : MonoBehaviour
    {
        private string m_Location;

        public void Init(string location)
        {
            m_Location = location;
            ResourceModule.Instance.LoadAssetReference(m_Location);
        }

        public void OnDestroy()
        {
            ResourceModule.Instance?.UnloadAssetReference(m_Location);
        }
    }
}