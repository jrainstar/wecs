namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 数据组件
    /// </summary>
    public abstract class BehaviorInfo
    {
        /// <summary>
        /// Entity
        /// </summary>
        public ulong actor { get; private set; }
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool active { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="actor">Actor</param>
        public void Ready(ulong actor)
        {
            this.actor = actor;
            this.active = true;
            OnReady();
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            OnReset();
            this.actor = default;
            this.active = false;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns>BehaviorInfo</returns>
        public BehaviorInfo Clone()
        {
            return OnClone();
        }

        /// <summary>
        /// 初始化, 当 BehaviorInfo 从对象池中取出, 在这个回调中初始化数据
        /// </summary>
        protected abstract void OnReady();

        /// <summary>
        /// 重置, 当 BehaviorInfo 回收, 重新回到对象池, 在这个回调中清理数据
        /// </summary>
        protected abstract void OnReset();

        /// <summary>
        /// 克隆, 克隆一个新的 BehaviorInfo
        /// </summary>
        /// <returns>BehaviorInfo</returns>
        protected abstract BehaviorInfo OnClone();
    }
}