using GameLogic.Gameplay.Common;

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
        protected Stage stage { get; private set; }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="stage">世界</param>
        /// <returns>预制创建器</returns>
        public Prefab Load(Stage stage)
        {
            this.stage = stage;

            return this;
        }

        /// <summary>
        /// 卸载
        /// </summary>
        public void Unload()
        {
            stage = null;
        }

        /// <summary>
        /// 处理预制创建器逻辑
        /// </summary>
        /// <param name="actor">Entity</param>
        /// <param name="info">预制信息</param>
        public void Processing(ulong actor, PrefabInfo info)
        {
            OnProcessing(actor, info);
        }

        /// <summary>
        /// 处理预制创建器逻辑
        /// </summary>
        /// <param name="actor">Actor</param>
        /// <param name="info">预制信息</param>
        protected virtual void OnProcessing(ulong actor, PrefabInfo info)
        {
        }
    }

    /// <summary>
    /// 预制创建器, 创建设定好的 Actor
    /// </summary>
    /// <typeparam name="T">预制信息类型</typeparam>
    public abstract class Prefab<T> : Prefab where T : PrefabInfo
    {
        /// <summary>
        /// Actor 类型
        /// </summary>
        public abstract byte type { get; }

        protected override void OnProcessing(ulong entity, PrefabInfo info)
        {
            if (stage.SeekBehavior(entity, out Tag tag)) tag.Set(TAG_DEFINE.ACTOR_TYPE, type);

            OnProcessing(entity, info as T);
        }

        /// <summary>
        /// 处理预制创建器逻辑
        /// </summary>
        /// <param name="actor">Actor</param>
        /// <param name="info">预制信息</param>
        protected abstract void OnProcessing(ulong actor, T info);
    }
}
