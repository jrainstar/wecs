using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// RIL 事件处理器
    /// </summary>
    public abstract class RILEventHandler : Comp
    {
        /// <summary>
        /// 数据状态桶
        /// </summary>
        protected RILBucket rilbucket { get; private set; }

        /// <summary>
        /// 初始化 RIL 事件处理器
        /// </summary>
        /// <param name="rilbucket">数据状态桶</param>
        /// <returns>RIL 事件处理器</returns>
        public RILEventHandler Initialize(RILBucket rilbucket)
        {
            this.rilbucket = rilbucket;

            return this;
        }

        /// <summary>
        /// 处理 RIL 事件
        /// </summary>
        /// <param name="e">RIL 事件</param>
        public virtual void Handle(IRIL_EVENT e)
        {
        }
    }
    
    /// <summary>
    /// RIL 事件处理器
    /// </summary>
    /// <typeparam name="T">RIL_EVENT 类型</typeparam>
    public abstract class RILEventHandler<T> : RILEventHandler where T : IRIL_EVENT
    {
        /// <summary>
        /// 处理 RIL 事件
        /// </summary>
        /// <param name="e">RIL 事件</param>
        public override void Handle(IRIL_EVENT e)
        {
            base.Handle(e);
            OnSalute(e as T);
        }

        /// <summary>
        /// 处理 RIL 事件
        /// </summary>
        /// <param name="e">RIL 事件</param>
        protected abstract void OnSalute(T e);
    }
}