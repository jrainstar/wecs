using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 行为系统
    /// </summary>
    public abstract class Behavior
    {
        /// <summary>
        /// 世界
        /// </summary>
        protected Stage stage { get; private set; }
        /// <summary>
        /// Entity
        /// </summary>
        public ulong actor { get; private set; }
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool active { get; set; }

        /// <summary>
        /// 组装
        /// </summary>
        /// <param name="world">世界</param>
        /// <param name="entity">Actor</param>
        public void Assemble(Stage world, ulong entity)
        {
            this.stage = world;
            this.actor = entity;
            this.active = true;
            AddBehaviorInfo();
            OnAssemble();
        }

        /// <summary>
        /// 拆解
        /// </summary>
        public void Disassemble()
        {
            this.active = false;
            OnDisassemble();
        }

        /// <summary>
        /// 添加绑定信息, 在 BehaviorInfo 被加载时调用
        /// </summary>
        public void AddBehaviorInfo()
        {
            OnAddBehaviorInfo();
        }

        /// <summary>
        /// 移除绑定信息, 在 BehaviorInfo 被卸载时调用
        /// </summary>
        public void RemoveBehaviorInfo()
        {
            OnRemoveBehaviorInfo();
        }

        /// <summary>
        /// Tick, 在每一帧中, 会被调用
        /// </summary>
        /// <param name="tick">步长</param>
        public void Tick(FP tick)
        {
            OnTick(tick);
        }

        /// <summary>
        /// EndTick, 在全部逻辑帧末, 会被调用
        /// </summary>
        public void EndTick()
        {
            OnEndTick();
        }

        /// <summary>
        /// 组装, 子类重写
        /// </summary>
        protected virtual void OnAssemble()
        {
        }

        /// <summary>
        /// 拆解, 子类重写
        /// </summary>
        protected virtual void OnDisassemble()
        {
        }

        /// <summary>
        /// 添加绑定信息, 子类重写
        /// </summary>
        protected virtual void OnAddBehaviorInfo()
        {
        }

        /// <summary>
        /// 移除绑定信息, 子类重写
        /// </summary>
        protected virtual void OnRemoveBehaviorInfo()
        {
        }

        /// <summary>
        /// Tick, 子类重写
        /// </summary>
        /// <param name="tick">步长</param>
        protected virtual void OnTick(FP tick)
        {
        }

        /// <summary>
        /// EndTick, 子类重写
        /// </summary>
        protected virtual void OnEndTick()
        {
        }
    }

    /// <summary>
    /// 行为系统
    /// </summary>
    /// <typeparam name="T">BehaviorInfo 类型</typeparam>
    public abstract class Behavior<T> : Behavior where T : BehaviorInfo, new()
    {
        /// <summary>
        /// BehaviorInfo 快捷访问
        /// </summary>
        public T info => stage.GetBehaviorInfo<T>(actor, true);

        protected override void OnAddBehaviorInfo()
        {
            base.OnAddBehaviorInfo();
            // System<T> 实现类, 可以指定 BehaviorInfo 用来快速访问对应的 BehaviorInfo
            if (false == stage.SeekBehaviorInfo(actor, out T behaviorinfo)) stage.AddBehaviorInfo<T>(actor);
        }

        protected override void OnRemoveBehaviorInfo()
        {
            base.OnRemoveBehaviorInfo();
            // System<T> 实现类, 绑定的 BehaviorInfo 在卸载时需要被移除
            if (stage.SeekBehaviorInfo(actor, out T behaviorinfo)) stage.RemoveBehaviorInfo(behaviorinfo);
        }
    }
}
