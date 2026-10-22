using GameLogic.Gameplay.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GameLogic.Gameplay.Logic
{
    public sealed class World
    {
        /// <summary>
        /// 所以 World 也是 Entity, 但它是一个特殊的 Entity, 它的 ID 是 ulong.MaxValue
        /// </summary>
        public ulong Entity => ulong.MaxValue;
        /// <summary>
        /// World 缓存
        /// </summary>
        public WorldCache Cache { get; private set; }
        public WorldComponent Info { get; private set; }
        public WorldComponent SnapShot { get; private set; }

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
            if (id == Entity && typeof(T) == typeof(WorldComponent)) return Info as T;
            if (false == Cache.ComponentDict.TryGetValue(id, out var dict)) return default;
            if (false == dict.TryGetValue(typeof(T), out var behaviorinfo)) return default;
            if (false == force && false == behaviorinfo.Enable) return default;

            return (T)behaviorinfo;
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