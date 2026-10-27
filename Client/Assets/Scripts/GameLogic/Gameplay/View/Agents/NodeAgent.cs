using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 节点代理, 负责把逻辑层的空间状态同步到 GameObject 的 Transform
    /// </summary>
    public class NodeAgent : Agent
    {
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

        protected override void OnReady()
        {
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

        protected override bool OnArrived()
        {
            return ApplySpatial();
        }

        protected override void OnFlash()
        {
            base.OnFlash();
            ApplySpatial();
        }

        /// <summary>
        /// 将逻辑层的空间状态同步到 Transform
        /// </summary>
        /// <returns>YES/NO</returns>
        private bool ApplySpatial()
        {
            if (null == go) return false;
            if (false == world.rilbucket.SeekRIL<RIL_SPATIAL>(actor, out var ril)) return false;

            var transform = go.transform;
            transform.position = ril.position.ToVector3();
            transform.rotation = Quaternion.Euler(ril.euler.ToVector3());
            transform.localScale = Vector3.one * ril.scale.AsFloat();

            if (false == go.activeSelf) go.SetActive(true);

            return true;
        }
    }
}
