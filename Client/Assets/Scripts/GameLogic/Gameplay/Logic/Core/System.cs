using GameLogic.Gameplay.Shared;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 行为系统
    /// </summary>
    public abstract class System
    {
        /// <summary>
        /// 世界
        /// </summary>
        protected World World { get; private set; }
        /// <summary>
        /// Entity
        /// </summary>
        public ulong Entity { get; private set; }
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 组装
        /// </summary>
        /// <param name="world">世界</param>
        /// <param name="entity">EntityID</param>
        public void Assemble(World world, ulong entity)
        {
            this.World = world;
            this.Entity = entity;
            this.Enable = true;
            AddComponent();
            OnAssemble();
        }

        /// <summary>
        /// 拆解
        /// </summary>
        public void Disassemble()
        {
            this.Enable = false;
            OnDisassemble();
        }

        /// <summary>
        /// 添加绑定信息, 在 System 被加载时调用
        /// </summary>
        public void AddComponent()
        {
            OnAddComponent();
        }

        /// <summary>
        /// 移除绑定信息, 在 System 被卸载时调用
        /// </summary>
        public void RemoveComponent()
        {
            OnRemoveComponent();
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
        protected virtual void OnAddComponent()
        {
        }

        /// <summary>
        /// 移除绑定信息, 子类重写
        /// </summary>
        protected virtual void OnRemoveComponent()
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
    /// <typeparam name="T">Component 类型</typeparam>
    public abstract class System<T> : System where T : Component, new()
    {
        /// <summary>
        /// Component 快捷访问
        /// </summary>
        public T Component => World.GetComponent<T>(Entity, true);

        protected override void OnAddComponent()
        {
            base.OnAddComponent();
            // System<T> 实现类, 可以指定 Component 用来快速访问对应的 Component
            if (false == World.SeekComponent(Entity, out T component)) World.AddComponent<T>(Entity);
        }

        protected override void OnRemoveComponent()
        {
            base.OnRemoveComponent();
            // System<T> 实现类, 绑定的 Component 在卸载时需要被移除
            if (World.SeekComponent(Entity, out T component)) World.RemoveComponent(component);
        }
    }
}
