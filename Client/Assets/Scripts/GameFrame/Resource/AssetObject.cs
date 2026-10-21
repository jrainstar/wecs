using System;
using YooAsset;
using GameCore;

namespace GameEngine
{
    public class AssetObject : IMemory
    {
        private bool m_IsPreLoad;
        private string m_Location;
        private AssetHandle m_AssetHandle;
        private SubAssetsHandle m_SubAssetHandle;


        public long LastUseTime { get; set; }
        public int ReferenceCount { get; set; }

        public bool IsPreload => m_IsPreLoad;
        public string Location => m_Location;
        public AssetHandle AssetHandle => m_AssetHandle;
        public SubAssetsHandle SubAssetsHandle => m_SubAssetHandle;

        public EOperationStatus Status => CurrentHandle.Status;
        public bool IsLoading => CurrentHandle.Status == EOperationStatus.Processing;
        public HandleBase CurrentHandle
        {
            get
            {
                if (AssetHandle != null) return AssetHandle;
                if (SubAssetsHandle != null) return SubAssetsHandle;
                return null;
            }
        }

        public void Init(string location, AssetHandle handle, bool isPreload)
        {
            m_Location = location;
            m_AssetHandle = handle;
            m_IsPreLoad = isPreload;
        }

        public void Init(string location, SubAssetsHandle handle, bool isPreload)
        {
            m_Location = location;
            m_SubAssetHandle = handle;
            m_IsPreLoad = isPreload;
        }


        public void Clear()
        {
            if (m_AssetHandle is { IsValid: true })
            {
                m_AssetHandle.Release();
            }
            if (m_SubAssetHandle is { IsValid: true })
            {
                m_SubAssetHandle.Release();
            }
            m_SubAssetHandle = null;
            m_AssetHandle = null;
            m_Location = null;
            LastUseTime = 0;
            ReferenceCount = 0;
        }

    }
}