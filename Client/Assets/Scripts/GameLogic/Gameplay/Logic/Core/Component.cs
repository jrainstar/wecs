namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 数据组件
    /// </summary>
    public abstract class Component
    {
        /// <summary>
        /// Entity
        /// </summary>
        public ulong Entity { get; private set; }
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="actor">ActorID</param>
        public void Ready(ulong entity)
        {
            this.Entity = entity;
            this.Enable = true;
            OnReady();
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            OnReset();
            this.Entity = default;
            this.Enable = false;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns>Component</returns>
        public Component Clone()
        {
            return OnClone();
        }

        /// <summary>
        /// 初始化, 当 Component 从对象池中取出, 在这个回调中初始化数据
        /// </summary>
        protected abstract void OnReady();
        /// <summary>
        /// 重置, 当 Component 回收, 重新回到对象池, 在这个回调中清理数据
        /// </summary>
        protected abstract void OnReset();
        /// <summary>
        /// 克隆, 克隆一个新的 Component
        /// </summary>
        /// <returns>Component</returns>
        protected abstract Component OnClone();
    }
}