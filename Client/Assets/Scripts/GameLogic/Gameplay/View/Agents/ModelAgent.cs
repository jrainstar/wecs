using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 模型代理, 负责根据 RIL_FACADE_MODEL 在节点代理下挂载模型
    /// </summary>
    public class ModelAgent : Agent
    {
        /// <summary>
        /// 模型节池
        /// </summary>
        private static GameObject modelpool = new("ModelPool");
        static ModelAgent()
        {
            var gameplay = GameObject.Find("Gameplay");
            if (null != gameplay) modelpool.transform.SetParent(gameplay.transform, false);
            modelpool.transform.localPosition = Vector3.zero;
            modelpool.transform.localScale = Vector3.one;
            modelpool.SetActive(false);
        }

        /// <summary>
        /// 模型 ID
        /// </summary>
        public int model { get; private set; } = 0;
        /// <summary>
        /// 资源名
        /// </summary>
        public string res { get; private set; }
        /// <summary>
        /// 模型 GameObject
        /// </summary>
        public GameObject go { get; private set; }

        protected override void OnReady()
        {
            model = 0;
            res = null;
            go = null;

            // 模型定义变化时重新加载
            WatchRIL<RIL_FACADE_MODEL>(OnRILFacadeModel);
        }

        protected override void OnReset()
        {
            RecycleModel();
            model = 0;
            res = null;
            go = null;
        }

        /// <summary>
        /// 模型定义变化
        /// </summary>
        /// <param name="ril">渲染指令</param>
        private void OnRILFacadeModel(RIL_FACADE_MODEL ril)
        {
            ChangeStatus(ChaseStatus.Chasing);
        }

        protected override void OnChase(float tick, float timescale)
        {
            base.OnChase(tick, timescale);
            Load();
        }

        protected override bool OnArrived()
        {
            // 模型挂载完成, 后续位置由节点代理驱动
            var node = world.GetAgent<NodeAgent>(actor);
            if (null == node || null == node.go) return false;
            if (model <= 0 || null == go) return false;
            if (go.transform.parent != node.go.transform) return false;

            return true;
        }

        /// <summary>
        /// 加载模型
        /// </summary>
        public void Load()
        {
            if (false == world.rilbucket.SeekRIL<RIL_FACADE_MODEL>(actor, out var ril) || ril.model <= 0)
            {
                RecycleModel();
                model = 0;

                return;
            }

            if (model == ril.model && null != go) return;

            // 节点代理不存在则先创建, 模型需要挂载到节点之下
            var node = world.EnsureAgent<NodeAgent>(actor);
            if (null == node || null == node.go) return;

            if (null == world.engine.config) return;

            RecycleModel();

            if (false == world.engine.config.location.TbRoleData.TryGetValue(ril.model, out var roledata)) return;
            if (false == world.engine.config.location.TbRoleModel.TryGetValue(roledata.ModelId, out var modeldata)) return;

            model = roledata.ModelId;
            res = modeldata.Model;

            go = ObjectPool.Get<GameObject>(res);
            if (null == go) go = world.engine.gameres.location.LoadModelSync(res);

            go.transform.SetParent(node.go.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
        }

        /// <summary>
        /// 回收模型 Go
        /// </summary>
        private void RecycleModel()
        {
            if (null == go) return;

            go.transform.SetParent(modelpool.transform, false);
            go.SetActive(false);
            ObjectPool.Set(go, res);
            go = null;
        }
    }
}
