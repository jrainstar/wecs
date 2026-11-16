using Cysharp.Threading.Tasks;
using GameFrame;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// GameRes 资源加载组件
    /// </summary>
    public class GameResNode : Node
    {
        /// <summary>
        /// 资源加载定位器，具体的加载在这里实现
        /// </summary>
        public LocationNode location { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            location = AddNode<LocationNode>();
            location.Create();
        }

        public async UniTask<T> LoadAssetAsync<T>(string res) where T : UnityEngine.Object
        {
            return await ResourceModule.Instance.LoadAssetAsync<T>(res);
        }

        public T LoadAssetSync<T>(string res) where T : UnityEngine.Object
        {
            return ResourceModule.Instance.LoadAssetSync<T>(res);
        }

        public async UniTask<byte[]> LoadRawFileAsync(string res)
        {
            return (await ResourceModule.Instance.LoadAssetAsync<TextAsset>(res)).bytes;
        }

        public byte[] LoadRawFileSync(string res)
        {
            return ResourceModule.Instance.LoadAssetSync<TextAsset>(res).bytes;
        }

        public async UniTask<Scene> LoadSceneASync(string res)
        {
            return await ResourceModule.Instance.LoadSceneAsync(res);
        }
    }
}
