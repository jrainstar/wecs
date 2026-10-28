using System;
using Cysharp.Threading.Tasks;
using GameCore;
using UnityEngine;
using UnityEngine.U2D;

namespace GameFrame
{
    /// <summary>
    /// ResourceModule - SpriteAtlas 便捷加载分部
    /// 提供图集 Sprite 的同步 / 异步加载入口，配合 SpriteAtlas 类型资源使用。
    /// 路径格式约定：atlasName/spriteName（例如 "UIIcons/hp_icon"）。
    /// </summary>
    public partial class ResourceModule
    {
        #region Sync

        public Sprite LoadSpriteSync(string path)
        {
            if (!TrySplitAtlasPath(path, out var atlasName, out var spriteName))
                return null;
            return LoadSpriteSync(atlasName, spriteName);
        }

        public Sprite LoadSpriteSync(string atlasName, string spriteName)
        {
            var sprite = LoadSubAssetsSync<Sprite>(atlasName, spriteName);
            return sprite;
        }

        /// <summary>
        /// 通过 "atlasName/spriteName" 形式的路径同步加载 Sprite。
        /// </summary>
        public Sprite LoadSpriteAtlasSync(string path)
        {
            if (!TrySplitAtlasPath(path, out var atlasName, out var spriteName))
                return null;
            return LoadSpriteAtlasSync(atlasName, spriteName);
        }

        /// <summary>
        /// 同步加载指定图集中的 Sprite。
        /// </summary>
        public Sprite LoadSpriteAtlasSync(string atlasName, string spriteName)
        {
            if (string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName))
            {
                Log.Error($"[ResourceModule] LoadSpriteAtlasSync 参数为空: atlas='{atlasName}', sprite='{spriteName}'");
                return null;
            }

            var atlas = LoadAssetSync<SpriteAtlas>(atlasName);
            if (atlas == null)
            {
                Log.Error($"[ResourceModule] LoadSpriteAtlasSync 图集未找到: {atlasName}");
                return null;
            }

            var sprite = atlas.GetSprite(spriteName);
            if (sprite == null)
                Log.Error($"[ResourceModule] LoadSpriteAtlasSync 图集 '{atlasName}' 中未找到 sprite: {spriteName}");
            return sprite;
        }

        #endregion

        #region Async

        /// <summary>
        /// 通过 "atlasName/spriteName" 形式的路径异步加载子资源 Sprite。
        /// </summary>
        public async UniTask<Sprite> LoadSpriteAsync(string path)
        {
            if (!TrySplitAtlasPath(path, out var atlasName, out var spriteName))
                return null;
            return await LoadSpriteAsync(atlasName, spriteName);
        }

        /// <summary>
        /// 异步加载指定资源中的子 Sprite。
        /// </summary>
        public async UniTask<Sprite> LoadSpriteAsync(string atlasName, string spriteName)
        {
            if (string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName))
            {
                Log.Error($"[ResourceModule] LoadSpriteAsync 参数为空: atlas='{atlasName}', sprite='{spriteName}'");
                return null;
            }

            var sprite = await LoadSubAssetsAsync<Sprite>(atlasName, spriteName);
            if (sprite == null)
                Log.Error($"[ResourceModule] LoadSpriteAsync 资源 '{atlasName}' 中未找到 sprite: {spriteName}");
            return sprite;
        }

        /// <summary>
        /// 异步加载子 Sprite（回调式）。
        /// </summary>
        public async UniTaskVoid LoadSpriteAsync(string path, Action<Sprite> callback)
        {
            var sprite = await LoadSpriteAsync(path);
            callback?.Invoke(sprite);
        }

        /// <summary>
        /// 异步加载子 Sprite（回调式）。
        /// </summary>
        public async UniTaskVoid LoadSpriteAsync(string atlasName, string spriteName, Action<Sprite> callback)
        {
            var sprite = await LoadSpriteAsync(atlasName, spriteName);
            callback?.Invoke(sprite);
        }

        /// <summary>
        /// 通过 "atlasName/spriteName" 形式的路径异步加载 Sprite。
        /// </summary>
        public async UniTask<Sprite> LoadSpriteAtlasAsync(string path)
        {
            if (!TrySplitAtlasPath(path, out var atlasName, out var spriteName))
                return null;
            return await LoadSpriteAtlasAsync(atlasName, spriteName);
        }

        /// <summary>
        /// 异步加载指定图集中的 Sprite。
        /// </summary>
        public async UniTask<Sprite> LoadSpriteAtlasAsync(string atlasName, string spriteName)
        {
            if (string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName))
            {
                Log.Error($"[ResourceModule] LoadSpriteAtlasAsync 参数为空: atlas='{atlasName}', sprite='{spriteName}'");
                return null;
            }

            var atlas = await LoadAssetAsync<SpriteAtlas>(atlasName);
            if (atlas == null)
            {
                Log.Error($"[ResourceModule] LoadSpriteAtlasAsync 图集未找到: {atlasName}");
                return null;
            }

            var sprite = atlas.GetSprite(spriteName);
            if (sprite == null)
                Log.Error($"[ResourceModule] LoadSpriteAtlasAsync 图集 '{atlasName}' 中未找到 sprite: {spriteName}");
            return sprite;
        }

        /// <summary>
        /// 异步加载指定图集中的 Sprite（回调式）。
        /// </summary>
        public async UniTaskVoid LoadSpriteAtlasAsync(string path, Action<Sprite> callback)
        {
            var sprite = await LoadSpriteAtlasAsync(path);
            callback?.Invoke(sprite);
        }

        /// <summary>
        /// 异步加载指定图集中的 Sprite（回调式）。
        /// </summary>
        public async UniTaskVoid LoadSpriteAtlasAsync(string atlasName, string spriteName, Action<Sprite> callback)
        {
            var sprite = await LoadSpriteAtlasAsync(atlasName, spriteName);
            callback?.Invoke(sprite);
        }

        #endregion

        #region Helper

        /// <summary>
        /// 拆分 "atlasName/spriteName" 形式的路径。
        /// </summary>
        private static bool TrySplitAtlasPath(string path, out string atlasName, out string spriteName)
        {
            atlasName = null;
            spriteName = null;
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("[ResourceModule] SpriteAtlas 路径为空");
                return false;
            }

            var idx = path.IndexOf('/');
            if (idx <= 0 || idx >= path.Length - 1)
            {
                Log.Error($"[ResourceModule] SpriteAtlas 路径格式错误，应为 'atlasName/spriteName'：{path}");
                return false;
            }

            atlasName = path.Substring(0, idx);
            spriteName = path.Substring(idx + 1);
            return true;
        }

        #endregion
    }
}