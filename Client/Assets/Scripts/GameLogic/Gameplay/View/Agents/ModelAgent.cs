using GameFrame;
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
        /// 角色编号, 与 RIL_FACADE_MODEL.model 一致
        /// </summary>
        public int model { get; private set; } = 0;
        /// <summary>
        /// 资源编号 (TbResourceData), 与 RIL_FACADE_MODEL.resource 一致
        /// </summary>
        public int resource { get; private set; } = 0;
        /// <summary>
        /// 资源路径, 同时作为对象池的键
        /// </summary>
        public string res { get; private set; }
        /// <summary>
        /// 模型 GameObject
        /// </summary>
        public GameObject go { get; private set; }

        protected override void OnReady()
        {
            model = 0;
            resource = 0;
            res = null;
            go = null;

            // 模型定义变化时重新加载
            WatchRIL<RIL_FACADE_MODEL>(OnRILFacadeModel);
        }

        protected override void OnReset()
        {
            RecycleModel();
            model = 0;
            resource = 0;
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
            if (model <= 0 && resource <= 0) return false;
            if (null == go) return false;
            if (go.transform.parent != node.go.transform) return false;

            return true;
        }

        /// <summary>
        /// 加载模型
        /// </summary>
        public void Load()
        {
            if (false == world.rilbucket.SeekRIL<RIL_FACADE_MODEL>(actor, out var facademodel))
            {
                RecycleModel();
                model = 0;
                resource = 0;
                res = null;

                return;
            }

            // 模型有两种来源: 角色 (model 为角色编号) 与 资源 (resource 为资源编号, 例如 AOE)
            var hasrole = facademodel.model > 0;
            var hasres = facademodel.resource > 0;
            if (false == hasrole && false == hasres)
            {
                RecycleModel();
                model = 0;
                resource = 0;
                res = null;

                return;
            }

            // 模型定义没有变化, 直接复用
            if (null != go && model == facademodel.model && resource == facademodel.resource) return;

            // 节点代理不存在则先创建, 模型需要挂载到节点之下
            var node = world.EnsureAgent<NodeAgent>(actor);
            if (null == node || null == node.go) return;

            if (null == world.engine.config) return;

            RecycleModel();

            // 角色走 角色 -> 角色模型 -> 资源 的查表链, 资源直接按编号取路径
            var tables = world.engine.config.location;
            string path;
            if (hasrole)
            {
                if (false == ResourceUtil.TryGetRoleModelPath(tables, facademodel.model, out path)) return;
            }
            else
            {
                if (false == ResourceUtil.TryGetPath(tables, facademodel.resource, out path)) return;
            }

            model = facademodel.model;
            resource = facademodel.resource;
            res = path;

            go = ObjectPool.Get<GameObject>(res);
            if (null == go) go = ResourceModule.Instance.InstantiateGameObject(res);
            if (null == go) return;

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
