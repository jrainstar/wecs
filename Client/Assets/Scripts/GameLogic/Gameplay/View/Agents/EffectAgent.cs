using GameFrame;
using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 特效节点, 一个逻辑特效对应一个节点
    /// fx 为 0 时节点只承载音效, 没有 GameObject
    /// </summary>
    public class EffectNode
    {
        /// <summary>
        /// 特效编号
        /// </summary>
        public uint id { get; set; }
        /// <summary>
        /// 特效资源编号
        /// </summary>
        public int fx { get; set; }
        /// <summary>
        /// 资源路径, 同时作为对象池的键
        /// </summary>
        public string res { get; set; }
        /// <summary>
        /// 特效 GameObject
        /// </summary>
        public GameObject go { get; set; }
        /// <summary>
        /// 特效控制器, 由 EffectAgent 按采样时间驱动
        /// </summary>
        public EffectController controller { get; set; }
        /// <summary>
        /// 渲染层的采样时间 (秒)
        /// </summary>
        public float elapsed { get; set; }
        /// <summary>
        /// 上一次 RIL 下发的逻辑时间 (秒), 用于检测逻辑时间倒退
        /// </summary>
        public float lastelapsed { get; set; }
        /// <summary>
        /// 存在时长 (秒)
        /// </summary>
        public float duration { get; set; }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            id = 0;
            fx = 0;
            res = null;
            go = null;
            controller = null;
            elapsed = 0f;
            lastelapsed = 0f;
            duration = 0f;
        }
    }

    /// <summary>
    /// 特效代理
    /// 按 RIL_FACADE_EFFECT 维护该 Actor 上的所有特效: 增删对比决定特效的创建/回收,
    /// 采样时间由渲染层按渲染帧推进, 每帧交给 EffectController 绝对时间采样 (粒子/动画可回放)
    /// </summary>
    public class EffectAgent : Agent
    {
        /// <summary>
        /// 特效池, 与模型池同理, 回收的特效 GameObject 挂在这里
        /// </summary>
        private static GameObject effectpool = new("EffectPool");
        static EffectAgent()
        {
            var gameplay = GameObject.Find("Gameplay");
            if (null != gameplay) effectpool.transform.SetParent(gameplay.transform, false);
            effectpool.transform.localPosition = Vector3.zero;
            effectpool.transform.localScale = Vector3.one;
            effectpool.SetActive(false);
        }

        /// <summary>
        /// 特效节点集合 (特效编号 -> 节点)
        /// </summary>
        private Dictionary<uint, EffectNode> nodes { get; set; }
        /// <summary>
        /// 待移除的特效编号, 避免边遍历边删
        /// </summary>
        private List<uint> rmvids { get; set; }

        protected override void OnReady()
        {
            nodes = ObjectPool.Ensure<Dictionary<uint, EffectNode>>();
            rmvids = ObjectPool.Ensure<List<uint>>();

            // 特效集合变化时重新同步
            WatchRIL<RIL_FACADE_EFFECT>(OnRILEffect);
        }

        protected override void OnReset()
        {
            DetachAll();

            if (null != rmvids)
            {
                rmvids.Clear();
                ObjectPool.Set(rmvids);
                rmvids = null;
            }

            if (null != nodes)
            {
                nodes.Clear();
                ObjectPool.Set(nodes);
                nodes = null;
            }
        }

        /// <summary>
        /// 特效集合变化
        /// </summary>
        /// <param name="ril">渲染指令</param>
        private void OnRILEffect(RIL_FACADE_EFFECT ril)
        {
            ChangeStatus(ChaseStatus.Chasing);
        }

        protected override void OnFlash()
        {
            base.OnFlash();
            Sync(0f);
        }

        protected override void OnChase(float tick, float timescale)
        {
            base.OnChase(tick, timescale);
            Sync(tick * timescale);
        }

        protected override bool OnArrived()
        {
            return null == nodes || 0 == nodes.Count;
        }

        /// <summary>
        /// 同步特效
        /// </summary>
        /// <param name="tick">本帧推进的采样时间 (秒), OnFlash 传 0</param>
        private void Sync(float tick)
        {
            if (null == nodes) return;

            if (false == world.rilbucket.SeekRIL<RIL_FACADE_EFFECT>(actor, out var ril) || null == ril.effectdict)
            {
                DetachAll();
                return;
            }

            // 逻辑层已经移除的特效, 连同 GameObject 一起回收
            rmvids.Clear();
            foreach (var kv in nodes) if (false == ril.effectdict.ContainsKey(kv.Key)) rmvids.Add(kv.Key);
            foreach (var id in rmvids) Detach(id);

            foreach (var kv in ril.effectdict)
            {
                var effect = kv.Value;
                if (false == nodes.TryGetValue(kv.Key, out var node))
                {
                    node = Attach(effect);
                    if (null == node) continue;
                    nodes.Add(kv.Key, node);
                }

                var elapsed = effect.elapsed.AsFloat();
                // 逻辑时间倒退说明回滚/重播, 对齐到逻辑时间;
                // 正常情况渲染层领先逻辑层不足一帧, 不能被拉回去, 否则每帧都往回拽会抖
                if (elapsed < node.lastelapsed) node.elapsed = elapsed;
                node.lastelapsed = elapsed;
                node.duration = effect.duration.AsFloat();

                if (tick > 0f) node.elapsed += tick;
                // 逻辑层还没移除但采样已经到点, 停在最后一帧等回收
                if (node.duration > 0f && node.elapsed > node.duration) node.elapsed = node.duration;

                if (null != node.controller) node.controller.Simulate(node.elapsed);
            }
        }

        /// <summary>
        /// 创建特效
        /// </summary>
        /// <param name="effect">特效渲染数据</param>
        /// <returns>特效节点</returns>
        private EffectNode Attach(RIL_EFFECT effect)
        {
            var node = ObjectPool.Ensure<EffectNode>();
            node.id = effect.id;
            node.fx = effect.fx;
            node.elapsed = effect.elapsed.AsFloat();
            node.lastelapsed = node.elapsed;
            node.duration = effect.duration.AsFloat();

            // 音效只在特效创建时播一次
            PlaySound(effect.sound);

            if (effect.fx > 0) LoadEffect(node, effect);

            return node;
        }

        /// <summary>
        /// 加载特效物体并挂到挂点上
        /// </summary>
        /// <param name="node">特效节点</param>
        /// <param name="effect">特效渲染数据</param>
        private void LoadEffect(EffectNode node, RIL_EFFECT effect)
        {
            if (null == world.engine || null == world.engine.config) return;
            if (false == ResourceUtil.TryGetPath(world.engine.config.location, effect.fx, out var path))
            {
                Debug.LogWarning($"VisualEvent: fx {effect.fx} is not exist.");
                return;
            }

            // 挂点找不到就没有落点, 直接放弃, 避免特效出现在世界原点
            var anchor = FindAnchor(actor, effect.anchor);
            if (null == anchor) return;

            var go = ObjectPool.Get<GameObject>(path);
            if (null == go) go = ResourceModule.Instance.InstantiateGameObject(path);
            if (null == go) return;

            var controller = go.GetComponent<EffectController>();
            if (null == controller) controller = go.AddComponent<EffectController>();
            // 预制体上没有绑定粒子时运行时补一份, 保证 Simulate 能采样到
            if (null == controller.pss || 0 == controller.pss.Length) controller.pss = go.GetComponentsInChildren<ParticleSystem>(true);
            controller.Reset();

            var offset = effect.offset.ToVector3();
            if (effect.follow)
            {
                // 跟随: 挂成骨骼的子节点, 位置/旋转由骨骼带动
                go.transform.SetParent(anchor, false);
                go.transform.localPosition = offset;
                go.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // 不跟随: 只取创建瞬间挂点的世界位置, 之后留在原地
                go.transform.SetParent(world.root.transform, true);
                go.transform.position = anchor.TransformPoint(offset);
                go.transform.rotation = anchor.rotation;
            }
            go.transform.localScale = Vector3.one * effect.scale.AsFloat();
            go.SetActive(true);

            node.go = go;
            node.res = path;
            node.controller = controller;
        }

        /// <summary>
        /// 回收特效
        /// </summary>
        /// <param name="id">特效编号</param>
        private void Detach(uint id)
        {
            if (null == nodes || false == nodes.TryGetValue(id, out var node)) return;

            nodes.Remove(id);
            RecycleNode(node);
        }

        /// <summary>
        /// 回收所有特效
        /// </summary>
        private void DetachAll()
        {
            if (null == nodes) return;

            foreach (var kv in nodes) RecycleNode(kv.Value);
            nodes.Clear();
        }

        /// <summary>
        /// 回收特效节点
        /// </summary>
        /// <param name="node">特效节点</param>
        private void RecycleNode(EffectNode node)
        {
            if (null != node.go)
            {
                node.go.transform.SetParent(effectpool.transform, false);
                node.go.SetActive(false);
                ObjectPool.Set(node.go, node.res);
            }

            node.Reset();
            ObjectPool.Set(node);
        }

        /// <summary>
        /// 播发音效
        /// </summary>
        /// <param name="sound">音效资源编号</param>
        private void PlaySound(int sound)
        {
            if (sound <= 0) return;
            if (null == world.engine || null == world.engine.config) return;
            if (false == ResourceUtil.TryGetPath(world.engine.config.location, sound, out var path))
            {
                Debug.LogWarning($"VisualEvent: sound {sound} is not exist.");
                return;
            }

            var module = SoundModule.Instance;
            if (null == module) return;

            module.PlaySound(path);
        }

        /// <summary>
        /// 查找挂点骨骼
        /// 空挂点表示单位根节点; 找不到骨骼时返回 null (视为无效挂点)
        /// </summary>
        /// <param name="actor">ActorID</param>
        /// <param name="anchor">挂点骨骼</param>
        /// <returns>挂点 Transform</returns>
        private UnityEngine.Transform FindAnchor(ulong actor, string anchor)
        {
            var node = world.GetAgent<NodeAgent>(actor);
            if (null == node || null == node.go) return null;

            var root = node.go.transform;
            if (string.IsNullOrEmpty(anchor)) return root;

            // 先按层级路径查找 (例如 Bip001/Bip001 Spine), 再按名字在整棵模型树里比对
            var found = root.Find(anchor);
            if (null != found) return found;

            foreach (var child in root.GetComponentsInChildren<UnityEngine.Transform>(true))
            {
                if (string.Equals(child.name, anchor, System.StringComparison.OrdinalIgnoreCase)) return child;
            }

            return null;
        }
    }
}
