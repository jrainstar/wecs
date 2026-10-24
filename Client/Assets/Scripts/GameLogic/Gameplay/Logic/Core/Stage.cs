using GameLogic.Gameplay.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// Actor 出生事件
    /// </summary>
    public struct ActorAddEvent : IEvent
    {
        /// <summary>
        /// ActorID
        /// </summary>
        public ulong actor { get; set; }
    }

    /// <summary>
    /// Actor 移除事件
    /// </summary>
    public struct ActorRmvEvent : IEvent
    {
        /// <summary>
        /// ActorID
        /// </summary>
        public ulong actor { get; set; }
    }

    /// <summary>
    /// Stage, 逻辑层的容器, 负责容纳所有的 Actor, 以及 Actor 的 Behavior & BehaviorInfo
    /// </summary>
    public sealed class Stage
    {
        /// <summary>
        /// 所以 Stage 也是 Actor, 但它是一个特殊的 Actor, 它的 ID 是 ulong.MaxValue
        /// </summary>
        public ulong sa => ulong.MaxValue;

        /// <summary>
        /// Stage 当前的运行状态
        /// </summary>
        public StageState state => info.state;

        /// <summary>
        /// Stage 当前的帧号
        /// </summary>
        public uint frame => info.frame;

        /// <summary>
        /// 时间流逝
        /// </summary>
        public FP elapsed => info.elapsed;

        /// <summary>
        /// 时间缩放
        /// </summary>
        public FP timescale
        {
            get => info.timescale;
            set => info.timescale = value;
        }

        /// <summary>
        /// Stage 缓存
        /// </summary>
        public StageCache cache { get; private set; }

        /// <summary>
        /// Stage 数据
        /// </summary>
        public StageInfo info { get; private set; }

        /// <summary>
        /// 快照
        /// </summary>
        public StageInfo snapshot { get; private set; }

        /// <summary>
        /// 是否有快照
        /// </summary>
        public bool hassnapshot => null != snapshot;

        /// <summary>
        /// 快照帧号
        /// </summary>
        public uint snapshotframe => null != snapshot ? snapshot.frame : 0;

        /// <summary>
        /// Stage 初始化的游戏数据
        /// </summary>
        public StageData data { get; set; }

        /// <summary>
        /// 预制创建器, 键为预制创建器信息类型
        /// </summary>
        public Dictionary<Type, Prefab> prefabs { get; private set; }

        /// <summary>
        /// 座位
        /// </summary>
        public Seat seat => GetBehavior<Seat>(sa, true);

        /// <summary>
        /// 渲染指令同步
        /// </summary>
        public RILSync rilsync => GetBehavior<RILSync>(sa, true);

        /// <summary>
        /// 事件订阅派发者
        /// </summary>
        public Eventor eventor => GetBehavior<Eventor>(sa, true);

        /// <summary>
        /// 随机数
        /// </summary>
        public Random random => GetBehavior<Random>(sa, true);

        /// <summary>
        /// 对外暴露抛出 RIL 的事件
        /// </summary>
        public Action<IRIL> onril { get; set; }

        /// <summary>
        /// 对外暴露抛出 RIL_DIFF 的事件
        /// </summary>
        public Action<IRIL_DIFF> ondiff { get; set; }

        /// <summary>
        /// 对外暴露抛出 RIL_EVENT 的事件
        /// </summary>
        public Action<IRIL_EVENT> onevent { get; set; }

        /// <summary>
        /// 初始化 Stage
        /// </summary>
        /// <param name="data">Stage 初始化的游戏数据</param>
        /// <returns>Stage</returns>
        /// <exception cref="Exception">初始化数据为空 || 重复初始化</exception>
        public Stage Initialize(StageData data)
        {
            if (null != info) throw new Exception("you cannot initialize more than once.");
            // 初始化数据存储
            this.data = data;
            // StageCache 初始化
            cache = new StageCache().Initialize();
            // 构建 StageInfo, 因为 Stage 的数据也是走 Behavior, 无法通过自举的方式走 API 添加
            // 悖论存在, 此处手动添加
            info = ObjectCache.Ensure<StageInfo>();
            info.Ready(sa);
            info.state = StageState.Initialized;

            AddActor(sa);
            // 构建 Stage 的 Behavior
            InitBehaviors();
            // 构建 Stage 的 Prefab
            InitPrefabs();

            return this;
        }

        /// <summary>
        /// 销毁 Stage
        /// </summary>
        public void Dispose()
        {
            if (StageState.Stopped != info.state) return;
            info.state = StageState.Disposed;

            // 回收 Actor
            cache.rmvactors.Clear();
            cache.rmvactorset.Clear();
            cache.rmvactors.AddRange(info.actors);
            foreach (var entity in info.actors) ActorToRecycle(entity);
            Recycle();

            // 卸载 Prefabs
            foreach (var prefab in prefabs.Values) prefab.Unload();
            prefabs.Clear();
            ObjectCache.Set(prefabs);

            // StageInfo 回收
            info.Reset();
            ObjectCache.Set(info);

            // StageCache 回收
            cache.Dispose();
        }

        /// <summary>
        /// 初始化 Stage 的 Behaviors
        /// </summary>
        private void InitBehaviors()
        {
            GetBehavior<Tag>(sa).Set(TAG_DEFINE.ACTOR_TYPE, ACTOR_DEFINE.WORLD);
            AddBehavior<Random>(sa).Initialze(data.seed);
            AddBehavior<Eventor>(sa);
            AddBehavior<Seat>(sa);
        }

        /// <summary>
        /// 初始化 Stage 的 Prefabs
        /// </summary>
        private void InitPrefabs()
        {
            prefabs = ObjectCache.Ensure<Dictionary<Type, Prefab>>();
            // 注册预制创建器: Prefab<具体预制创建器, 预制创建器信息>()
        }

        /// <summary>
        /// 注册预制创建器
        /// </summary>
        /// <typeparam name="T">预制创建器类型</typeparam>
        /// <typeparam name="E">预制创建器信息类型</typeparam>
        public void AddPrefab<T, E>() where T : Prefab, new() where E : PrefabInfo
        {
            if (prefabs.ContainsKey(typeof(E))) return;

            prefabs.Add(typeof(E), ObjectCache.Ensure<T>().Load(this));
        }

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            if (StageState.Initialized != info.state) return;
            info.state = StageState.Ticking;
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            if (StageState.Ticking != info.state) return;
            info.state = StageState.Paused;
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Resume()
        {
            if (StageState.Paused != info.state) return;
            info.state = StageState.Ticking;
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (StageState.Stopped == info.state) return;
            info.state = StageState.Stopped;
        }

        /// <summary>
        /// 驱动
        /// </summary>
        public void Step()
        {
            if (StageState.Ticking != info.state) return;

            var tick = info.timescale * GAME_DEFINE.LOGIC_TICK;
            // 帧号递增 & 时间流逝
            info.frame++;
            info.elapsed += tick;

            // Tick 驱动
            foreach (var type in TICK_DEFINE.TICK_TYPE_LIST)
            {
                if (false == SeekBehaviors(type, out var systems)) continue;
                foreach (var system in systems)
                {
                    // 携带 TickerInfo 的 Actor, 使用自身的时间缩放
                    if (SeekBehaviorInfo(system.actor, out TickerInfo ticker))
                    {
                        system.Tick(ticker.timescale * tick);
                        continue;
                    }

                    system.Tick(tick);
                }
            }

            // 帧末调度
            foreach (var type in TICK_DEFINE.TICK_TYPE_LIST)
            {
                if (false == SeekBehaviors(type, out var systems)) continue;
                foreach (var system in systems) system.EndTick();
            }

            // 回收
            Recycle();
        }

        /// <summary>
        /// 添加 Behavior
        /// </summary>
        /// <param name="id">Actor</param>
        /// <typeparam name="T">Behavior 类型</typeparam>
        /// <returns>Behavior</returns>
        /// <exception cref="Exception">Actor 不存在 | Behavior 已存在</exception>
        public T AddBehavior<T>(ulong id) where T : Behavior, new()
        {
            return AddBehavior(id, typeof(T)) as T;
        }

        /// <summary>
        /// 添加 Behavior
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="type">Behavior 类型</param>
        /// <returns>Behavior</returns>
        /// <exception cref="Exception">Actor 不存在 | Behavior 已存在</exception>
        private Behavior AddBehavior(ulong id, Type type)
        {
            // 检查 Behavior 容器是否存在
            if (false == cache.behaviors.TryGetValue(type, out var list)) cache.behaviors.Add(type, list = ObjectCache.Ensure<List<Behavior>>());
            if (false == cache.behaviordict.TryGetValue(id, out var dict)) cache.behaviordict.Add(id, dict = ObjectCache.Ensure<Dictionary<Type, Behavior>>());
            // 检查 Behavior 是否已经存在容器中
            if (dict.ContainsKey(type)) throw new Exception($"behavior {type} is exist.");
            if (false == info.behaviortypes.TryGetValue(id, out var types)) info.behaviortypes.Add(id, types = ObjectCache.Ensure<List<Type>>());
            // 检查 Actor 是否存在
            if (false == info.actors.Contains(id)) throw new Exception($"actor {id} is not exist.");

            var behavior = ObjectCache.Ensure(type) as Behavior;
            if (false == types.Contains(type)) types.Add(type);
            dict.Add(type, behavior);
            list.Add(behavior);

            behavior.Assemble(this, id);
            // 排序
            list.Sort((a, b) =>
            {
                return a.actor.CompareTo(b.actor);
            });

            return behavior;
        }

        /// <summary>
        /// 移除 Behavior
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveBehaviors)
        /// </summary>
        /// <param name="behavior">Behavior</param>
        public void RemoveBehavior(Behavior behavior)
        {
            if (false == behavior.active) return;
            behavior.active = false;
            behavior.RemoveBehaviorInfo();
            cache.rmvbehaviors.Add(behavior);
        }

        /// <summary>
        /// 获取指定类型的所有 Behavior 列表
        /// </summary>
        /// <param name="behaviors">所有 Behavior 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Behavior 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekBehaviors<T>(out List<T> behaviors, bool force = false) where T : Behavior
        {
            behaviors = GetBehaviors<T>(force);

            return null != behaviors;
        }

        /// <summary>
        /// 获取指定类型的所有 Behavior 列表
        /// </summary>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Behavior 类型</typeparam>
        /// <returns>所有 Behavior 列表</returns>
        public List<T> GetBehaviors<T>(bool force = false) where T : Behavior
        {
            var type = typeof(T);
            if (false == cache.behaviors.TryGetValue(type, out var list) || 0 == list.Count) return default;

            // 根据类型去获取所有 Behavior
            List<T> result = default;
            foreach (var behavior in list)
            {
                if (false == force && false == behavior.active) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<T>>();
                    cache.AutoRecycle(result);
                }

                result.Add(behavior as T);
            }

            return result;
        }

        /// <summary>
        /// 获取指定类型的所有 Behavior 列表
        /// </summary>
        /// <param name="type">Behavior 类型</param>
        /// <param name="behaviors">所有 Behavior 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekBehaviors(Type type, out List<Behavior> behaviors, bool force = false)
        {
            behaviors = GetBehaviors(type, force);

            return null != behaviors;
        }

        /// <summary>
        /// 获取指定类型的所有 Behavior 列表
        /// <param name="type">Behavior 类型</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// </summary>
        /// <returns>所有 Behavior 列表</returns>
        public List<Behavior> GetBehaviors(Type type, bool force = false)
        {
            if (false == cache.behaviors.TryGetValue(type, out var behaviors) || 0 == behaviors.Count) return default;
            List<Behavior> result = default;
            foreach (var behavior in behaviors)
            {
                if (false == force && false == behavior.active) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<Behavior>>();
                    cache.AutoRecycle(result);
                }
                result.Add(behavior);
            }

            return result;
        }

        /// <summary>
        /// 获取 Actor 所有 Behavior 列表
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="behaviors">Behavior 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekBehaviors(ulong id, out List<Behavior> behaviors, bool force = false)
        {
            behaviors = GetBehaviors(id, force);

            return null != behaviors;
        }

        /// <summary>
        /// 获取 Actor 所有 Behavior 列表
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>Behavior 列表</returns>
        public List<Behavior> GetBehaviors(ulong id, bool force = false)
        {
            if (false == info.behaviortypes.TryGetValue(id, out var types)) return default;

            // 根据 Actor 获取所有 Behavior
            List<Behavior> result = default;
            foreach (var type in types)
            {
                if (false == SeekBehavior(id, type, out var behavior, force)) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<Behavior>>();
                    cache.AutoRecycle(result);
                }

                result.Add(behavior);
            }

            return result;
        }

        /// <summary>
        /// 获取 Behavior
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="behavior">Behavior</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Behavior 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekBehavior<T>(ulong id, out T behavior, bool force = false) where T : Behavior, new()
        {
            behavior = GetBehavior<T>(id, force);

            return null != behavior;
        }

        /// <summary>
        /// 获取 Behavior
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="type">Behavior 类型</param>
        /// <param name="behavior">Behavior</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekBehavior(ulong id, Type type, out Behavior behavior, bool force = false)
        {
            behavior = GetBehavior(id, type, force);

            return null != behavior;
        }

        /// <summary>
        /// 获取 Behavior
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">Behavior 类型</typeparam>
        /// <returns>Behavior</returns>
        /// <exception cref="Exception">Actor 不存在</exception>
        public T GetBehavior<T>(ulong id, bool force = false) where T : Behavior
        {
            var behavior = GetBehavior(id, typeof(T), force);
            if (null == behavior) return default;

            return behavior as T;
        }

        /// <summary>
        /// 获取 Behavior
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="type">Behavior 类型</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>Behavior</returns>
        public Behavior GetBehavior(ulong id, Type type, bool force = false)
        {
            if (false == cache.behaviordict.TryGetValue(id, out var dict)) return default;
            if (false == dict.TryGetValue(type, out var behavior)) return default;
            if (false == force && false == behavior.active) return default;

            return behavior;
        }

        /// <summary>
        /// 移除 BehaviorInfo
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveBehaviorInfos)
        /// </summary>
        /// <param name="behaviorinfo">BehaviorInfo</param>
        public void RemoveBehaviorInfo(BehaviorInfo behaviorinfo)
        {
            if (false == behaviorinfo.active) return;
            behaviorinfo.active = false;
            cache.rmvbehaviorinfos.Add(behaviorinfo);
        }

        /// <summary>
        /// 添加 BehaviorInfo
        /// </summary>
        /// <param name="id">Actor</param>
        /// <typeparam name="T">BehaviorInfo 类型</typeparam>
        /// <returns>BehaviorInfo</returns>
        /// <exception cref="Exception">Actor 不存在 | BehaviorInfo 已存在</exception>
        public T AddBehaviorInfo<T>(ulong id) where T : BehaviorInfo, new()
        {
            // 检查 BehaviorInfo 容器是否存在
            if (false == cache.behaviorinfodict.TryGetValue(id, out var dict)) cache.behaviorinfodict.Add(id, dict = ObjectCache.Ensure<Dictionary<Type, BehaviorInfo>>());
            // 检查 BehaviorInfo 是否已经存在容器中
            if (dict.ContainsKey(typeof(T))) throw new Exception($"behaviorinfo {typeof(T)} is exist.");
            // 初始化 BehaviorInfos
            if (false == info.behaviorinfos.TryGetValue(typeof(T), out var list)) info.behaviorinfos.Add(typeof(T), list = ObjectCache.Ensure<List<BehaviorInfo>>());

            var behaviorinfo = ObjectCache.Ensure<T>();
            dict.Add(typeof(T), behaviorinfo);
            list.Add(behaviorinfo);
            behaviorinfo.Ready(id);

            return behaviorinfo;
        }

        /// <summary>
        /// 获取指定类型的所有 BehaviorInfo 列表
        /// </summary>
        /// <param name="behaviorinfos">BehaviorInfo 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">BehaviorInfo 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekBehaviorInfos<T>(out List<T> behaviorinfos, bool force = false) where T : BehaviorInfo
        {
            behaviorinfos = GetBehaviorInfos<T>(force);

            return null != behaviorinfos;
        }

        /// <summary>
        /// 获取指定类型的所有 BehaviorInfo 列表
        /// </summary>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">BehaviorInfo 类型</typeparam>
        /// <returns>BehaviorInfo 列表</returns>
        /// 
        public List<T> GetBehaviorInfos<T>(bool force = false) where T : BehaviorInfo
        {
            if (false == info.behaviorinfos.TryGetValue(typeof(T), out var components)) return default;
            List<T> result = default;
            foreach (var i in components)
            {
                if (false == force && false == i.active) continue;
                if (null == result)
                {
                    result = ObjectCache.Ensure<List<T>>();
                    cache.AutoRecycle(result);
                }

                result.Add(i as T);
            }

            return result;
        }

        /// <summary>
        /// 获取 Actor 所有 BehaviorInfo 列表
        /// </summary>
        /// <param name="id">Actor</param>
        /// <param name="behaviorinfos">BehaviorInfo 列表</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>YES/NO</returns>
        public bool SeekBehaviorInfos(ulong id, out List<BehaviorInfo> behaviorinfos, bool force = false)
        {
            behaviorinfos = GetBehaviorInfos(id, force);

            return null != behaviorinfos;
        }

        /// <summary>
        /// 获取 Actor 所有 BehaviorInfo 列表
        /// </summary>
        /// <param name="id">ActorID</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <returns>BehaviorInfo 列表</returns>
        public List<BehaviorInfo> GetBehaviorInfos(ulong id, bool force = false)
        {
            if (false == cache.behaviorinfodict.TryGetValue(id, out var dict)) return default;
            List<BehaviorInfo> result = default;
            foreach (var behaviorinfo in dict.Values)
            {
                if (false == force && false == behaviorinfo.active) continue;
                if (null == result) result = ObjectCache.Ensure<List<BehaviorInfo>>();
                result.Add(behaviorinfo);
            }

            if (null != result)
            {
                // 排序
                result.Sort((a, b) =>
                {
                    return a.actor.CompareTo(b.actor);
                });
                cache.AutoRecycle(result);
            }

            return result;
        }

        /// <summary>
        /// 获取 BehaviorInfo
        /// </summary>
        /// <param name="id">ActorID</param>
        /// <param name="info">BehaviorInfo</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">BehaviorInfo 类型</typeparam>
        /// <returns>YES/NO</returns>
        public bool SeekBehaviorInfo<T>(ulong id, out T info, bool force = false) where T : BehaviorInfo
        {
            info = GetBehaviorInfo<T>(id, force);

            return null != info;
        }

        /// <summary>
        /// 获取 BehaviorInfo
        /// </summary>
        /// <param name="id">ActorID</param>
        /// <param name="force">强制, 包含 Remove</param>
        /// <typeparam name="T">BehaviorInfo 类型</typeparam>
        /// <returns>BehaviorInfo</returns>
        public T GetBehaviorInfo<T>(ulong id, bool force = false) where T : BehaviorInfo
        {
            if (id == sa && typeof(T) == typeof(StageInfo)) return info as T;
            if (false == cache.behaviorinfodict.TryGetValue(id, out var dict)) return default;
            if (false == dict.TryGetValue(typeof(T), out var behaviorinfo)) return default;
            if (false == force && false == behaviorinfo.active) return default;

            return (T)behaviorinfo;
        }

        // ======================================================================
        // Actor 补充 API
        // ======================================================================

        /// <summary>
        /// 生成 Actor
        /// </summary>
        /// <returns>Actor</returns>
        public ulong GenActor()
        {
            var entity = ++info.increment;
            AddActor(entity);

            return entity;
        }

        /// <summary>
        /// 添加 Actor
        /// 会自动携带 Tag, 并把 ActorType 写入 NONE
        /// </summary>
        /// <param name="entity">ActorID</param>
        public void AddActor(ulong entity)
        {
            if (info.actors.Contains(entity)) return;
            info.actors.Add(entity);
            // 默认携带 Tag 系统, 写入 ActorType 为 NONE
            AddBehavior<Tag>(entity).Set(TAG_DEFINE.ACTOR_TYPE, ACTOR_DEFINE.NONE);
        }

        /// <summary>
        /// 回收 Actor
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveActors)
        /// </summary>
        /// <param name="entity">ActorID</param>
        /// <returns>YES/NO</returns>
        private bool ActorToRecycle(ulong entity)
        {
            if (false == cache.Valid(entity)) return false;
            cache.rmvactors.Add(entity);
            cache.rmvactorset.Add(entity);

            if (SeekBehaviors(entity, out var systems)) foreach (var system in systems) RemoveBehavior(system);
            if (SeekBehaviorInfos(entity, out var components)) foreach (var component in components) RemoveBehaviorInfo(component);

            return true;
        }

        /// <summary>
        /// 移除 Actor
        /// 不是立即执行, 而是添加入移除列表, 等待帧末执行回收 (Cache.RemoveActors)
        /// </summary>
        /// <param name="actor">Actor</param>
        public void RemoveActor(ulong actor)
        {
            if (false == ActorToRecycle(actor)) return;

            eventor.Tell(new ActorRmvEvent { actor = actor });
        }

        /// <summary>
        /// 根据预制创建器构建一个 Actor
        /// </summary>
        /// <param name="info">预制信息</param>
        /// <typeparam name="T">预制信息类型</typeparam>
        /// <returns>Actor</returns>
        /// <exception cref="Exception">预制创建器不存在</exception>
        public ulong Spawn<T>(T info) where T : PrefabInfo
        {
            if (false == prefabs.TryGetValue(typeof(T), out var prefab)) throw new Exception($"prefab {typeof(T)} is not exist.");

            var entity = GenActor();
            prefab.Processing(entity, info);

            eventor.Tell(new ActorAddEvent { actor = entity });

            return entity;
        }

        /// <summary>
        /// 回收/销毁 Actor/Behavior/BehaviorInfo
        /// </summary>
        private void Recycle()
        {
            // 拆解 Behavior
            foreach (var remove in cache.rmvbehaviors) remove.Disassemble();

            // 回收 Behavior
            foreach (var remove in cache.rmvbehaviors)
            {
                if (cache.behaviors.TryGetValue(remove.GetType(), out var systems)) systems.Remove(remove);
                if (cache.behaviordict.TryGetValue(remove.actor, out var dict)) dict.Remove(remove.GetType());
                if (info.behaviortypes.TryGetValue(remove.actor, out var types)) types.Remove(remove.GetType());

                ObjectCache.Set(remove);
            }

            // 重置 BehaviorInfo
            foreach (var remove in cache.rmvbehaviorinfos)
            {
                if (cache.behaviorinfodict.TryGetValue(remove.actor, out var dict)) dict.Remove(remove.GetType());
                if (info.behaviorinfos.TryGetValue(remove.GetType(), out var components)) components.Remove(remove);

                remove.Reset();
                ObjectCache.Set(remove);
            }

            // 回收清理
            foreach (var remove in cache.rmvactors)
            {
                if (cache.behaviordict.TryGetValue(remove, out var systems))
                {
                    systems.Clear();
                    ObjectCache.Set(systems);
                    cache.behaviordict.Remove(remove);
                }

                if (cache.behaviorinfodict.TryGetValue(remove, out var components))
                {
                    components.Clear();
                    ObjectCache.Set(components);
                    cache.behaviorinfodict.Remove(remove);
                }

                info.actors.Remove(remove);
            }

            cache.rmvactors.Clear();
            cache.rmvactorset.Clear();
            cache.rmvbehaviors.Clear();
            cache.rmvbehaviorinfos.Clear();

            // 回收帧末临时 List
            foreach (var list in cache.tickendrecyclelist)
            {
                list.Clear();
                ObjectCache.Set(list);
            }
            cache.tickendrecyclelist.Clear();
        }
    }


    /// <summary>
    /// Stage 缓存
    /// </summary>
    public sealed class StageCache
    {
        /// <summary>
        /// 行为列表, 键为 ActorId, 值为该 Actor 上的所有行为
        /// </summary>
        public Dictionary<ulong, Dictionary<Type, Behavior>> behaviordict { get; set; }
        /// <summary>
        /// 行为列表, 键为行为类型, 值为该行为类型的所有 Behavior 列表
        /// </summary>
        public Dictionary<Type, List<Behavior>> behaviors { get; set; }
        /// <summary>
        /// 行为信息列表, 键为 Actor, 值为该 Actor 上的所有行为信息
        /// </summary>
        public Dictionary<ulong, Dictionary<Type, BehaviorInfo>> behaviorinfodict { get; set; }
        /// <summary>
        /// Remove Actor 列表
        /// </summary>
        public List<ulong> rmvactors { get; set; }
        /// <summary>
        /// Remove Actor 集合, 用于快速验证 Actor 是否有效
        /// </summary>
        public HashSet<ulong> rmvactorset { get; set; }
        /// <summary>
        /// Remove Behavior 列表
        /// </summary>
        public List<Behavior> rmvbehaviors { get; set; }
        /// <summary>
        /// Remove BehaviorInfo 列表
        /// </summary>
        public List<BehaviorInfo> rmvbehaviorinfos { get; set; }
        /// <summary>
        /// TickEnd 自动回收 List 列表
        /// </summary>
        public List<IList> tickendrecyclelist { get; set; }

        /// <summary>
        /// 初始化 StageCache
        /// </summary>
        /// <returns>StageCache</returns>
        public StageCache Initialize()
        {
            behaviordict = ObjectCache.Ensure<Dictionary<ulong, Dictionary<Type, Behavior>>>();
            behaviors = ObjectCache.Ensure<Dictionary<Type, List<Behavior>>>();
            behaviorinfodict = ObjectCache.Ensure<Dictionary<ulong, Dictionary<Type, BehaviorInfo>>>();
            rmvactors = ObjectCache.Ensure<List<ulong>>();
            rmvactorset = ObjectCache.Ensure<HashSet<ulong>>();
            rmvbehaviors = ObjectCache.Ensure<List<Behavior>>();
            rmvbehaviorinfos = ObjectCache.Ensure<List<BehaviorInfo>>();
            tickendrecyclelist = ObjectCache.Ensure<List<IList>>();

            return this;
        }

        /// <summary>
        /// 验证 Actor 是否有效
        /// </summary>
        /// <param name="entity">ActorID</param>
        /// <returns>YES/NO</returns>
        public bool Valid(ulong entity)
        {
            return false == rmvactorset.Contains(entity);
        }

        /// <summary>
        /// 添加 TickEnd 自动回收 List 列表
        /// </summary>
        /// <param name="list">List 列表</param>
        public void AutoRecycle(IList list)
        {
            if (tickendrecyclelist.Contains(list)) return;
            tickendrecyclelist.Add(list);
        }

        /// <summary>
        /// 销毁 StageCache
        /// </summary>
        public void Dispose()
        {
            behaviordict.Clear();
            ObjectCache.Set(behaviordict);

            behaviors.Clear();
            ObjectCache.Set(behaviors);

            behaviorinfodict.Clear();
            ObjectCache.Set(behaviorinfodict);

            rmvactors.Clear();
            ObjectCache.Set(rmvactors);

            rmvactorset.Clear();
            ObjectCache.Set(rmvactorset);

            rmvbehaviors.Clear();
            ObjectCache.Set(rmvbehaviors);

            rmvbehaviorinfos.Clear();
            ObjectCache.Set(rmvbehaviorinfos);

            tickendrecyclelist.Clear();
            foreach (var list in tickendrecyclelist)
            {
                list.Clear();
                ObjectCache.Set(list);
            }
            ObjectCache.Set(tickendrecyclelist);
        }
    }
}