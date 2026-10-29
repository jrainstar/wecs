using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 节点代理, 负责把逻辑层的空间状态同步到 GameObject 的 Transform
    /// 逻辑帧 (25fps) 与渲染帧 (60fps) 不对等, 逐帧阻尼追赶以避免位移一卡一卡
    /// </summary>
    public class NodeAgent : Agent
    {
        /// <summary>
        /// 追踪速度, 越大越贴近逻辑帧(越小越平滑但延迟越大)
        /// </summary>
        private const float SPEED = 12f;

        /// <summary>
        /// 贴合阈值(米), 距离小于该值直接对齐
        /// </summary>
        private const float SNAP = 0.001f;

        /// <summary>
        /// Node 根
        /// </summary>
        private static GameObject root = new("Node");

        static NodeAgent()
        {
            var gameplay = GameObject.Find("Gameplay");
            if (null != gameplay) root.transform.SetParent(gameplay.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// GameObject
        /// </summary>
        public GameObject go { get; private set; }
        /// <summary>
        /// 是否已经完成一次定位 (完成前直接贴合, 避免从原点飞过来)
        /// </summary>
        private bool settled { get; set; }

        protected override void OnReady()
        {
            settled = false;

            go = ObjectPool.Get<GameObject>("NODE_GO_KEY");
            if (null == go)
            {
                go = new GameObject("Node");
                go.transform.SetParent(root.transform, false);
                go.SetActive(false);
            }
            go.transform.SetParent(root.transform, false);
            go.SetActive(true);

            // 关心空间状态, 空间变化时重新进入追逐
            WatchRIL<RIL_SPATIAL>(OnRILSpatial);
        }

        protected override void OnReset()
        {
            settled = false;
            if (null == go) return;

            go.transform.SetParent(root.transform, false);
            go.SetActive(false);
            ObjectPool.Set(go, "NODE_GO_KEY");
            go = null;
        }

        /// <summary>
        /// 空间状态变更
        /// </summary>
        /// <param name="ril">渲染指令</param>
        private void OnRILSpatial(RIL_SPATIAL ril)
        {
            ChangeStatus(ChaseStatus.Chasing);
        }

        protected override void OnFlash()
        {
            base.OnFlash();
            settled = false;
            ApplySpatial(0f);
        }

        protected override bool OnArrived()
        {
            return ApplySpatial(Time.deltaTime);
        }

        /// <summary>
        /// 将逻辑层的空间状态同步到 Transform
        /// </summary>
        /// <param name="tick">渲染帧时长</param>
        /// <returns>YES/NO</returns>
        private bool ApplySpatial(float tick)
        {
            if (null == go) return false;
            if (false == world.rilbucket.SeekRIL<RIL_SPATIAL>(actor, out var ril)) return false;

            var transform = go.transform;
            var position = ril.position.ToVector3();
            var rotation = Quaternion.Euler(ril.euler.ToVector3());
            var localScale = Vector3.one * ril.scale.AsFloat();

            if (tick <= 0 || false == settled)
            {
                // 首次定位或瞬移, 直接贴合
                transform.position = position;
                transform.rotation = rotation;
                transform.localScale = localScale;
                settled = true;
            }
            else
            {
                // 与帧率无关的指数阻尼, 把 25fps 的位移摊平到每个渲染帧
                var t = 1f - Mathf.Exp(-SPEED * tick);
                transform.position = Vector3.Lerp(transform.position, position, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
                transform.localScale = Vector3.Lerp(transform.localScale, localScale, t);

                // 足够接近时贴合, 避免无限逼近导致的抖动
                if ((transform.position - position).sqrMagnitude < SNAP * SNAP)
                {
                    transform.position = position;
                    transform.rotation = rotation;
                    transform.localScale = localScale;
                }
            }

            if (false == go.activeSelf) go.SetActive(true);

            // 停在原地不再变化时结束追逐, 有新的空间状态会重新进入
            return (transform.position - position).sqrMagnitude < SNAP * SNAP;
        }
    }
}
