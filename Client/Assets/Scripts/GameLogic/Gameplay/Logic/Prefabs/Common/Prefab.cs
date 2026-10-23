using GameLogic.Gameplay.Shared;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 预制创建器, 创建设定好的 Entity
    /// </summary>
    public abstract class Prefab
    {
        /// <summary>
        /// 世界
        /// </summary>
        protected World World { get; private set; }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="world">世界</param>
        /// <returns>预制创建器</returns>
        public Prefab Load(World world)
        {
            this.World = world;

            return this;
        }

        /// <summary>
        /// 卸载
        /// </summary>
        public void Unload()
        {
            World = null;
        }

        /// <summary>
        /// 处理预制创建器逻辑
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="info">预制信息</param>
        public void Processing(ulong entity, PrefabInfo info)
        {
            OnProcessing(entity, info);
        }

        /// <summary>
        /// 处理预制创建器逻辑
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="info">预制信息</param>
        protected virtual void OnProcessing(ulong entity, PrefabInfo info)
        {
        }
    }

    /// <summary>
    /// 预制创建器, 创建设定好的 Entity
    /// </summary>
    /// <typeparam name="T">预制信息类型</typeparam>
    public abstract class Prefab<T> : Prefab where T : PrefabInfo
    {
        /// <summary>
        /// Entity 类型
        /// </summary>
        public abstract byte Type { get; }

        protected override void OnProcessing(ulong entity, PrefabInfo info)
        {
            if (World.SeekSystem(entity, out Tag tag)) tag.Set(TAG_DEFINE.ENTITY_TYPE, Type);

            OnProcessing(entity, info as T);
        }

        /// <summary>
        /// 处理预制创建器逻辑
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="info">预制信息</param>
        protected abstract void OnProcessing(ulong entity, T info);
    }
}
