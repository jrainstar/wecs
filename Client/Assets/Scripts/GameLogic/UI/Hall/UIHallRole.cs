using GameFrame;
using GameLogic.Gameplay.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameLogic
{
    public class UIHallRole : MonoBehaviour
    {
        [SerializeField]
        [LabelText("模型坐标")]
        private Vector3 m_RenderPos;

        private GameObject m_RenderRole;

        private void OnEnable()
        {
            EventModule.Instance.Subscribe(EventDefine.AckGetHeroData, OnAckGetHeroData);

            // 如果数据已就绪（非首次），直接加载
            if (HeroSys.Instance.HeroData.Cid > 0)
            {
                LoadCurrentHero();
            }
        }

        private void OnDisable()
        {
            EventModule.Instance.Unsubscribe(EventDefine.AckGetHeroData, OnAckGetHeroData);
            ReleaseModel();
        }

        private void OnAckGetHeroData(object obj)
        {
            LoadCurrentHero();
        }

        private void LoadCurrentHero()
        {
            var cid = HeroSys.Instance.HeroData.Cid;

            if (cid == 0)
            {
                // 先取第一个~（后续可改为默认角色配置）
                var allData = TableModule.Instance.Tables.TbRoleData.DataList;
                if (allData == null || allData.Count == 0) return;
                cid = allData[0].Id;
            }

            var roleData = TableModule.Instance.Tables.TbRoleData.Get(cid);
            if (roleData == null) return;

            var modelData = TableModule.Instance.Tables.TbRoleModel.Get(roleData.ModelId);
            if (modelData == null) return;
            // 模型表里存的是资源编号, 实际加载用的是资源路径
            if (false == ResourceUtil.TryGetPath(TableModule.Instance.Tables, modelData.Model, out var modelpath)) return;

            ReleaseModel();

            m_RenderRole = ObjectPoolModule.Instance.GetGameObject(modelpath);
            if (null == m_RenderRole) return;
            m_RenderRole.layer = LayerMask.NameToLayer("UIModel");
            m_RenderRole.transform.position = m_RenderPos;
        }

        private void ReleaseModel()
        {
            if (m_RenderRole == null) return;
            if (ObjectPoolModule.Instance == null) return;

            ObjectPoolModule.Instance.PushGameObject(m_RenderRole);
            m_RenderRole = null;
        }
    }
}
