using GameLogic.Gameplay.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// Entity 出生事件
    /// </summary>
    public struct EntityAddEvent : IEvent
    {
        /// <summary>
        /// EntityID
        /// </summary>
        public ulong Entity { get; set; }
    }

    /// <summary>
    /// Entity 移除事件
    /// </summary>
    public struct EntityRemoveEvent : IEvent
    {
        /// <summary>
        /// EntityID
        /// </summary>
        public ulong Entity { get; set; }
    }

    /// <summary>
    /// World, 逻辑层的容器, 负责容纳所有的 Entity, 以及 Entity 的 System & Component
    /// </summary>
    public sealed class World
    {
        /// <summary>
        /// 所以 World 也是 Entity, 但它是一个特殊的 Entity, 它的 ID 是 ulong.MaxValue
        /// </summary>
        public ulong Entity => ulong.MaxValue;
        /// <summary>
        /// World 当前的运行状态
        /// </summary>
        public WorldState State => Info.State;
        /// <summary>
        /// World 当前的帧号
        /// </summary>
        public uint Frame => Info.Frame;
        /// <summary>
        /// 时间流逝
        /// </summary>
        public FP Elapsed => Info.Elapsed;
        /// <summary>
        /// 时间缩放
        /// </summary>
        public FP TimeScale
        {
            get => Info.TimeScale;
            set => Info.TimeScale = value;
        }
        /// <summary>
        /// World 缓存
        /// </summary>
        public WorldCache Cache { get; private set; }
        /// <summary>
        /// World 数据
        /// </summary>
        public WorldInfo Info { get; private set; }
        /// <summary>
        /// 快照
        /// </summary>
        public WorldInfo SnapShot { get; private set; }
        /// <summary>
        /// 是否有快照
        /// </summary>
        public bool HasSnapShot => null != SnapShot;
        /// <summary>
        /// 快照帧号
        /// </summary>
        public uint SnapShotFrame => null != SnapShot ? SnapShot.Frame : 0;
        /// <summary>
        /// World 初始化的游戏数据
        /// </summary>
        public WorldData Data { get; set; }
        /// <summary>
        /// 预制创建器, 键为预制创建器信息类型
        /// </summary>
        public Dictionary<Type, Prefab> Prefabs { get; private set; }
        /// <summary>
        /// 事件订阅派发者
        /// </summary>
        public Eventor Eventor => GetSystem<Eventor>(Entity, true);
        /// <summary>
        /// 座位
        /// </summary>
        public Seat Seat => GetSystem<Seat>(Entity, true);
        /// <summary>
        /// 随机数
        /// </summary>
        public Random Random => GetSystem<Random>(Entity, true);

        /// <summary>
        /// 初始化 World
        /// </summary>
        /// <param name="data">World 初始化的游戏数据</param>
        /// <returns>World</returns>
        /// <exception cref="Exception">初始化数据为空 || 重复初始化</exception>
        public World Initialize(WorldData data)
        {
            if (null != Info) throw new Exception("you cannot initialize more than once.");
            // 初始化数据存储
            this.Data = data;
            // WorldCache 初始化
            Cache = new WorldCache().Initialize();
            // 构建 WorldInfo, 因为 World 的数据也是走 Component, 无法通过自举的方式走 API 添加
            // 悖论存在, 此处手动添加
            Info = ObjectCache.Ensure<WorldInfo>();
            Info.Ready(Entity);
            Info.State = WorldState.Initialized;

            AddEntity(Entity);
            // 构建 World 的 System
            InitSystems();
            // 构建 World 的 Prefab
            InitPrefabs();

            return this;
        }

        /// <summary>
        /// 销毁 World
        /// </summary>
        public void Dispose()
        {
            if (WorldState.Stopped != Info.State) return;
            Info.State = WorldState.Disposed;

            // 回收 Entity
            Cache.RemoveEntities.Clear();
            Cache.RemoveEntitiySet.Clear();
            Cache.RemoveEntities.AddRange(Info.Entities);
            foreach (var entity in Info.Entities) EntityToRecycle(entity);
            Recycle();

            // 卸载 Prefabs
            foreach (var prefab in Prefabs.Values) prefab.Unload();
            Prefabs.Clear();
            ObjectCache.Set(Prefabs);

            // WorldInfo 回收
            Info.Reset();
            ObjectCache.Set(Info);

            // WorldCache 回收
            Cache.Dispose();
        }

        /// <summary>
        /// 初始化 World 的 Systems
        /// </summary>
        private void InitSystems()
        {
            GetSystem<Tag>(Entity).Set(TAG_DEFINE.ENTITY_TYPE, ENTITY_DEFINE.WORLD);
            AddSystem<Random>(Entity).Initialze(Data.Seed);
            AddSystem<Eventor>(Entity);
            AddSystem<Seat>(Entity);
        }

        /// <summary>
        /// 初始化 World 的 Prefabs
        /// </summary>
        private void InitPrefabs()
        {
            Prefabs = ObjectCache.Ensure<Dictionary<Type, Prefab>>();
            // 注册预制创建器: Prefab<具体预制创建器, 预制创建器信息>()
        }

        /// <summary>
        /// 注册预制创建器
        /// </summary>
        /// <typeparam name="T">预制创建器类型</typeparam>
        /// <typeparam name="E">预制创建器信息类型</typeparam>
        public void AddPrefab<T, E>() where T : Prefab, new() where E : PrefabInfo
        {
            if (Prefabs.ContainsKey(typeof(E))) return;

            Prefabs.Add(typeof(E), ObjectCache.Ensure<T>().Load(this));
        }

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            if (WorldState.Initialized != Info.State) return;
            Info.State = WorldState.Ticking;
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            if (WorldState.Ticking != Info.State) return;
            Info.State = WorldState.Paused;
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Resume()
        {
            if (WorldState.Paused != Info.State) return;
            Info.State = WorldState.Ticking;
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (WorldState.Stopped == Info.State) return;
            Info.State = WorldState.Stopped;
        }

        /// <summary>
        /// 驱动
        /// </summary>
        public void Step()
        {
            if (WorldState.Ticking != Info.State) return;

            var tick = Info.TimeScale * GAME_DEFINE.LOGIC_TICK;
            // 帧号递增 & 时间流逝
            Info.Frame++;
            Info.Elapsed += tick;

            // Tick 驱动
            foreach (var type in TICK_DEFINE.TICK_TYPE_LIST)
            {
                if (false == SeekSystems(type, out var systems)) continue;
                foreach (var system in systems)
                {
                    // 携带 TickerInfo 的 Entity, 使用自身的时间缩放
                    if (SeekComponent(system.Entity, out TickerInfo ticker))
                    {
                        system.Tick(ticker.TimeScale * tick);
                        continue;
                    }

                    system.Tick(tick);
                }
            }

            // 帧末调度
            foreach (var type in TICK_DEFINE.TICK_TYPE_LIST)
            {
                if (false == SeekSystems(type, out var systems)) continue;
                foreach (var system in systems) system.EndTick();
            }

            // 回收
            Recycle();
        }

        /// <summary>
        /// 添加 System
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <typeparam name="T">System 类型</typeparam>
        /// <returns>System</returns>
        /// <exception cref="Exception">Entity 不存在 | System 已存在</exception>
        public T AddSystem<T>(ulong id) where T : System, new()
        {
            return AddSystem(id, typeof(T)) as T;
        }

        /// <summary>
        /// 添加 System
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="type">System 类型</param>
        /// <returns>System</returns>
        /// <exception cref="Exception">Entity 不存在 | System 已存在</exception>
        private System AddSystem(ulong id, Type type)
        {
            // 检查 System 容器是否存在
            if (false == Cache.Systems.TryGetValue(type, out var list)) Cache.Systems.Add(type, list = ObjectCache.Ensure<List<System>>());
            if (false == Cache.SystemDict.TryGetValue(id, out var dict)) Cache.SystemDict.Add(id, dict = ObjectCache.Ensure<Dictionary<Type, System>>());
            // 检查 System 是否已经存在容器中
            if (dict.ContainsKey(type)) throw new Exception($"system {type} is exist.");
            if (false == Info.SystemTypes.TryGetValue(id, out var types)) Info.SystemTypes.Add(id, types = ObjectCache.Ensure<List<Type>>());
            // 检查 Entity 是否存在
            if (false == Info.Entities.Contains(id)) throw new Exception($"entity {id} is not exist.");

            var behavior = ObjectCache.Ensure(type) as System;
            if (false == types.Contains(type)) types.Add(type);
            dict.Add(type, behavior);
            list.Add(behavior);

            behavior.Assemble(this, id);
            // 排序
            list.Sort((a, b) =>
            {
                return a.Entity.CompareTo(b.Entity);
            });

            return behavior;
        }

        /// <summary>
        /// 移除 System
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveSystems)
        /// </summary>
        /// <param name="system">System</param>
        public void RemoveSystem(System system)
        {
            if (false == system.Enable) return;
            system.Enable = false;
            system.RemoveComponent();
            Cache.RemoveSystems.Add(system);
        }

        /// <summary>
        /// 获取指定类型的所有 System 列表
        /// </summary>
        /// <param name="behaviors">所有 System 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">System 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekSystems<T>(out List<T> behaviors, bool force = false) where T : System
        {
            behaviors = GetSystems<T>(force);

            return null != behaviors;
        }

        /// <summary>
        /// 获取指定类型的所有 System 列表
        /// </summary>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">System 类型</typeparam>
        /// <returns>所有 System 列表</returns>
        public List<T> GetSystems<T>(bool force = false) where T : System
        {
            var type = typeof(T);
            if (false == Cache.Systems.TryGetValue(type, out var list) || 0 == list.Count) return default;

            // 根据类型去获取所有 System
            List<T> result = default;
            foreach (var behavior in list)
            {
                if (false == force && false == behavior.Enable) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<T>>();
                    Cache.AutoRecycle(result);
                }

                result.Add(behavior as T);
            }

            return result;
        }

        /// <summary>
        /// 获取指定类型的所有 System 列表
        /// </summary>
        /// <param name="type">System 类型</param>
        /// <param name="behaviors">所有 System 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekSystems(Type type, out List<System> behaviors, bool force = false)
        {
            behaviors = GetSystems(type, force);

            return null != behaviors;
        }

        /// <summary>
        /// 获取指定类型的所有 System 列表
        /// <param name="type">System 类型</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// </summary>
        /// <returns>所有 System 列表</returns>
        public List<System> GetSystems(Type type, bool force = false)
        {
            if (false == Cache.Systems.TryGetValue(type, out var behaviors) || 0 == behaviors.Count) return default;
            List<System> result = default;
            foreach (var behavior in behaviors)
            {
                if (false == force && false == behavior.Enable) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<System>>();
                    Cache.AutoRecycle(result);
                }
                result.Add(behavior);
            }

            return result;
        }

        /// <summary>
        /// 获取 Entity 所有 System 列表
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="behaviors">System 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekSystems(ulong id, out List<System> behaviors, bool force = false)
        {
            behaviors = GetSystems(id, force);

            return null != behaviors;
        }

        /// <summary>
        /// 获取 Entity 所有 System 列表
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>System 列表</returns>
        public List<System> GetSystems(ulong id, bool force = false)
        {
            if (false == Info.SystemTypes.TryGetValue(id, out var types)) return default;

            // 根据 EntityID 获取所有 System
            List<System> result = default;
            foreach (var type in types)
            {
                if (false == SeekSystem(id, type, out var behavior, force)) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<System>>();
                    Cache.AutoRecycle(result);
                }

                result.Add(behavior);
            }

            return result;
        }

        /// <summary>
        /// 获取 System
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="behavior">System</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">System 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekSystem<T>(ulong id, out T behavior, bool force = false) where T : System, new()
        {
            behavior = GetSystem<T>(id, force);

            return null != behavior;
        }

        /// <summary>
        /// 获取 System
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="type">System 类型</param>
        /// <param name="behavior">System</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekSystem(ulong id, Type type, out System behavior, bool force = false)
        {
            behavior = GetSystem(id, type, force);

            return null != behavior;
        }

        /// <summary>
        /// 获取 System
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">System 类型</typeparam>
        /// <returns>System</returns>
        /// <exception cref="Exception">Entity 不存在</exception>
        public T GetSystem<T>(ulong id, bool force = false) where T : System
        {
            var behavior = GetSystem(id, typeof(T), force);
            if (null == behavior) return default;

            return behavior as T;
        }

        /// <summary>
        /// 获取 System
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="type">System 类型</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>System</returns>
        public System GetSystem(ulong id, Type type, bool force = false)
        {
            if (false == Cache.SystemDict.TryGetValue(id, out var dict)) return default;
            if (false == dict.TryGetValue(type, out var behavior)) return default;
            if (false == force && false == behavior.Enable) return default;

            return behavior;
        }

        /// <summary>
        /// 移除 Component
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveComponents)
        /// </summary>
        /// <param name="component">Component</param>
        public void RemoveComponent(Component component)
        {
            if (false == component.Enable) return;
            component.Enable = false;
            Cache.RemoveComponents.Add(component);
        }

        /// <summary>
        /// 添加 Component
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <typeparam name="T">Component 类型</typeparam>
        /// <returns>Component</returns>
        /// <exception cref="Exception">Entity 不存在 | Component 已存在</exception>
        public T AddComponent<T>(ulong id) where T : Component, new()
        {
            // 检查 Component 容器是否存在
            if (false == Cache.ComponentDict.TryGetValue(id, out var dict)) Cache.ComponentDict.Add(id, dict = ObjectCache.Ensure<Dictionary<Type, Component>>());
            // 检查 Component 是否已经存在容器中
            if (dict.ContainsKey(typeof(T))) throw new Exception($"Component {typeof(T)} is exist.");
            // 初始化 Components
            if (false == Info.ComponentDict.TryGetValue(typeof(T), out var list)) Info.ComponentDict.Add(typeof(T), list = ObjectCache.Ensure<List<Component>>());

            var behaviorinfo = ObjectCache.Ensure<T>();
            dict.Add(typeof(T), behaviorinfo);
            list.Add(behaviorinfo);
            behaviorinfo.Ready(id);

            return behaviorinfo;
        }

        /// <summary>
        /// 获取指定类型的所有 Component 列表
        /// </summary>
        /// <param name="components">Component 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Component 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekComponents<T>(out List<T> components, bool force = false) where T : Component
        {
            components = GetComponents<T>(force);

            return null != components;
        }

        /// <summary>
        /// 获取指定类型的所有 Component 列表
        /// </summary>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Component 类型</typeparam>
        /// <returns>Component 列表</returns>
        /// 
        public List<T> GetComponents<T>(bool force = false) where T : Component
        {
            if (false == Info.ComponentDict.TryGetValue(typeof(T), out var components)) return default;
            List<T> result = default;
            foreach (var i in components)
            {
                if (false == force && false == i.Enable) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<T>>();
                    Cache.AutoRecycle(result);
                }

                result.Add(i as T);
            }

            return result;
        }

        /// <summary>
        /// 获取 Entity 所有 Component 列表
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="components">Component 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekComponents(ulong id, out List<Component> components, bool force = false)
        {
            components = GetComponents(id, force);

            return null != components;
        }

        /// <summary>
        /// 获取 Entity 所有 Component 列表
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>Component 列表</returns>
        public List<Component> GetComponents(ulong id, bool force = false)
        {
            if (false == Cache.ComponentDict.TryGetValue(id, out var dict)) return default;
            List<Component> result = default;
            foreach (var behaviorinfo in dict.Values)
            {
                if (false == force && false == behaviorinfo.Enable) continue;
                if (null == result) result = ObjectCache.Ensure<List<Component>>();
                result.Add(behaviorinfo);
            }

            if (null != result)
            {
                // 排序
                result.Sort((a, b) =>
                {
                    return a.Entity.CompareTo(b.Entity);
                });
                Cache.AutoRecycle(result);
            }

            return result;
        }

        /// <summary>
        /// 获取 Component
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="info">Component</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Component 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekComponent<T>(ulong id, out T info, bool force = false) where T : Component
        {
            info = GetComponent<T>(id, force);

            return null != info;
        }

        /// <summary>
        /// 获取 Component
        /// </summary>
        /// <param name="id">EntityID</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Component 类型</typeparam>
        /// <returns>Component</returns>
        public T GetComponent<T>(ulong id, bool force = false) where T : Component
        {
            if (id == Entity && typeof(T) == typeof(WorldInfo)) return Info as T;
            if (false == Cache.ComponentDict.TryGetValue(id, out var dict)) return default;
            if (false == dict.TryGetValue(typeof(T), out var behaviorinfo)) return default;
            if (false == force && false == behaviorinfo.Enable) return default;

            return (T)behaviorinfo;
        }

        // ======================================================================
        // Entity 补充 API
        // ======================================================================

        /// <summary>
        /// 生成 Entity
        /// </summary>
        /// <returns>EntityID</returns>
        public ulong GenEntity()
        {
            var entity = ++Info.Increment;
            AddEntity(entity);

            return entity;
        }

        /// <summary>
        /// 添加 Entity
        /// 会自动携带 Tag, 并把 EntityType 写入 NONE
        /// </summary>
        /// <param name="entity">EntityID</param>
        public void AddEntity(ulong entity)
        {
            if (Info.Entities.Contains(entity)) return;
            Info.Entities.Add(entity);
            // 默认携带 Tag 系统, 写入 EntityType 为 NONE
            AddSystem<Tag>(entity).Set(TAG_DEFINE.ENTITY_TYPE, ENTITY_DEFINE.NONE);
        }

        /// <summary>
        /// 回收 Entity
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveEntities)
        /// </summary>
        /// <param name="entity">EntityID</param>
        /// <returns>YES/NO</returns>
        private bool EntityToRecycle(ulong entity)
        {
            if (false == Cache.Valid(entity)) return false;
            Cache.RemoveEntities.Add(entity);
            Cache.RemoveEntitiySet.Add(entity);

            if (SeekSystems(entity, out var systems)) foreach (var system in systems) RemoveSystem(system);
            if (SeekComponents(entity, out var components)) foreach (var component in components) RemoveComponent(component);

            return true;
        }

        /// <summary>
        /// 移除 Entity
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveEntities)
        /// </summary>
        /// <param name="entity">EntityID</param>
        public void RemoveEntity(ulong entity)
        {
            if (false == EntityToRecycle(entity)) return;

            Eventor.Tell(new EntityRemoveEvent { Entity = entity });
        }

        /// <summary>
        /// 根据预制创建器构建一个 Entity
        /// </summary>
        /// <param name="info">预制信息</param>
        /// <typeparam name="T">预制信息类型</typeparam>
        /// <returns>Entity</returns>
        /// <exception cref="Exception">预制创建器不存在</exception>
        public ulong Spawn<T>(T info) where T : PrefabInfo
        {
            if (false == Prefabs.TryGetValue(typeof(T), out var prefab)) throw new Exception($"prefab {typeof(T)} is not exist.");

            var entity = GenEntity();
            prefab.Processing(entity, info);

            Eventor.Tell(new EntityAddEvent { Entity = entity });

            return entity;
        }

        /// <summary>
        /// 回收/销毁 Entity/System/Component
        /// </summary>
        private void Recycle()
        {
            // 拆解 System
            foreach (var remove in Cache.RemoveSystems) remove.Disassemble();

            // 回收 System
            foreach (var remove in Cache.RemoveSystems)
            {
                if (Cache.Systems.TryGetValue(remove.GetType(), out var systems)) systems.Remove(remove);
                if (Cache.SystemDict.TryGetValue(remove.Entity, out var dict)) dict.Remove(remove.GetType());
                if (Info.SystemTypes.TryGetValue(remove.Entity, out var types)) types.Remove(remove.GetType());

                ObjectCache.Set(remove);
            }

            // 重置 Component
            foreach (var remove in Cache.RemoveComponents)
            {
                if (Cache.ComponentDict.TryGetValue(remove.Entity, out var dict)) dict.Remove(remove.GetType());
                if (Info.ComponentDict.TryGetValue(remove.GetType(), out var components)) components.Remove(remove);

                remove.Reset();
                ObjectCache.Set(remove);
            }

            // 回收清理
            foreach (var remove in Cache.RemoveEntities)
            {
                if (Cache.SystemDict.TryGetValue(remove, out var systems))
                {
                    systems.Clear();
                    ObjectCache.Set(systems);
                    Cache.SystemDict.Remove(remove);
                }

                if (Cache.ComponentDict.TryGetValue(remove, out var components))
                {
                    components.Clear();
                    ObjectCache.Set(components);
                    Cache.ComponentDict.Remove(remove);
                }

                Info.Entities.Remove(remove);
            }

            Cache.RemoveEntities.Clear();
            Cache.RemoveEntitiySet.Clear();
            Cache.RemoveSystems.Clear();
            Cache.RemoveComponents.Clear();

            // 回收帧末临时 List
            foreach (var list in Cache.TickEndRecycleList)
            {
                list.Clear();
                ObjectCache.Set(list);
            }
            Cache.TickEndRecycleList.Clear();
        }
    }


    /// <summary>
    /// World 缓存
    /// </summary>
    public sealed class WorldCache
    {
        /// <summary>
        /// 行为列表, 键为 EntityId, 值为该 Entity 上的所有行为
        /// </summary>
        public Dictionary<ulong, Dictionary<Type, System>> SystemDict { get; set; }
        /// <summary>
        /// 行为列表, 键为行为类型, 值为该行为类型的所有 System 列表
        /// </summary>
        public Dictionary<Type, List<System>> Systems { get; set; }
        /// <summary>
        /// 行为信息列表, 键为 EntitiyId, 值为该 Entitiy 上的所有行为信息
        /// </summary>
        public Dictionary<ulong, Dictionary<Type, Component>> ComponentDict { get; set; }
        /// <summary>
        /// Remove Entitiy 列表
        /// </summary>
        public List<ulong> RemoveEntities { get; set; }
        /// <summary>
        /// Remove Entitiy 集合, 用于快速验证 Entitiy 是否有效
        /// </summary>
        public HashSet<ulong> RemoveEntitiySet { get; set; }
        /// <summary>
        /// Remove System列表
        /// </summary>
        public List<System> RemoveSystems { get; set; }
        /// <summary>
        /// Remove Component 列表
        /// </summary>
        public List<Component> RemoveComponents { get; set; }
        /// <summary>
        /// TickEnd 自动回收 List 列表
        /// </summary>
        public List<IList> TickEndRecycleList { get; set; }

        /// <summary>
        /// 初始化 WorldCache
        /// </summary>
        /// <returns>WorldCache</returns>
        public WorldCache Initialize()
        {
            SystemDict = ObjectCache.Ensure<Dictionary<ulong, Dictionary<Type, System>>>();
            Systems = ObjectCache.Ensure<Dictionary<Type, List<System>>>();
            ComponentDict = ObjectCache.Ensure<Dictionary<ulong, Dictionary<Type, Component>>>();
            RemoveEntities = ObjectCache.Ensure<List<ulong>>();
            RemoveEntitiySet = ObjectCache.Ensure<HashSet<ulong>>();
            RemoveSystems = ObjectCache.Ensure<List<System>>();
            RemoveComponents = ObjectCache.Ensure<List<Component>>();
            TickEndRecycleList = ObjectCache.Ensure<List<IList>>();

            return this;
        }

        /// <summary>
        /// 验证 Entity 是否有效
        /// </summary>
        /// <param name="entity">EntityID</param>
        /// <returns>YES/NO</returns>
        public bool Valid(ulong entity)
        {
            return false == RemoveEntitiySet.Contains(entity);
        }

        /// <summary>
        /// 添加 TickEnd 自动回收 List 列表
        /// </summary>
        /// <param name="list">List 列表</param>
        public void AutoRecycle(IList list)
        {
            if (TickEndRecycleList.Contains(list)) return;
            TickEndRecycleList.Add(list);
        }

        /// <summary>
        /// 销毁 WorldCache
        /// </summary>
        public void Dispose()
        {
            SystemDict.Clear();
            ObjectCache.Set(SystemDict);

            Systems.Clear();
            ObjectCache.Set(Systems);

            ComponentDict.Clear();
            ObjectCache.Set(ComponentDict);

            RemoveEntities.Clear();
            ObjectCache.Set(RemoveEntities);

            RemoveEntitiySet.Clear();
            ObjectCache.Set(RemoveEntitiySet);

            RemoveSystems.Clear();
            ObjectCache.Set(RemoveSystems);

            RemoveComponents.Clear();
            ObjectCache.Set(RemoveComponents);

            TickEndRecycleList.Clear();
            foreach (var list in TickEndRecycleList)
            {
                list.Clear();
                ObjectCache.Set(list);
            }
            ObjectCache.Set(TickEndRecycleList);
        }
    }
}