using DG.Tweening;
using GameConf.Gameplay;
using GameConf.Gameplay.Role;
using GameEngine;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIRoleInfo : UIWidget
    {
        [SerializeField]
        [LabelText("图标")]
        private Image m_RoleIcon;

        [SerializeField]
        [LabelText("名称")]
        private TextMeshProUGUI m_RoleName;

        [SerializeField]
        [LabelText("描述")]
        private TextMeshProUGUI m_RoleDesc;

        [SerializeField]
        [LabelText("生命")]
        private Slider m_Health;

        [SerializeField]
        [LabelText("攻击")]
        private Slider m_Attack;

        [SerializeField]
        [LabelText("防御")]
        private Slider m_Defense;

        [SerializeField]
        [LabelText("速度")]
        private Slider m_Speed;

        [SerializeField]
        [LabelText("暴击")]
        private Slider m_CritRate;

        [SerializeField]
        [LabelText("幸运")]
        private Slider m_Luck;

        [SerializeField]
        [LabelText("坐标")]
        private Vector3 m_RenderRolePos;

        private float m_MaxValue = 100f;
        private float m_Duration = 0.5f;

        private GameObject m_RenderRole;

        private void OnEnable()
        {
            m_RoleIcon.gameObject.SetActive(false);

            m_Health.value = 0;
            m_Attack.value = 0;
            m_Defense.value = 0;
            m_Speed.value = 0;
            m_CritRate.value = 0;
            m_Luck.value = 0;
        }

        private void OnDisable()
        {
            if (m_RenderRole != null)
            {
                if (ObjectPoolModule.Instance != null)
                {
                    ObjectPoolModule.Instance.PushGameObject(m_RenderRole);
                }
                m_RenderRole = null;
            }
        }

        public void SetData(RoleData roleData)
        {
            var modelId = roleData.ModelId;
            var modelData = TableModule.Instance.Tables.TbRoleModel.Get(modelId);

            if (m_RenderRole != null)
            {
                ObjectPoolModule.Instance.PushGameObject(m_RenderRole);
            }

            m_RenderRole = ObjectPoolModule.Instance.GetGameObject(modelData.Model);
            m_RenderRole.layer = LayerMask.NameToLayer("UIModel");
            m_RenderRole.transform.position = m_RenderRolePos;

            m_RoleIcon.sprite = ResourceModule.Instance.LoadSpriteSync(roleData.Icon);
            m_RoleName.text = roleData.Name;
            m_RoleDesc.text = roleData.Desc;

            // 先 Kill 之前的动画，防止快速切换角色时动画冲突
            m_Health.DOKill(true);
            m_Attack.DOKill(true);
            m_Defense.DOKill(true);
            m_Speed.DOKill(true);
            m_CritRate.DOKill(true);
            m_Luck.DOKill(true);

            m_Health.DOValue(roleData.Hp / m_MaxValue, m_Duration);
            m_Attack.DOValue(roleData.Atk / m_MaxValue, m_Duration);
            m_Defense.DOValue(roleData.Def / m_MaxValue, m_Duration);
            m_Speed.DOValue(roleData.Spd / m_MaxValue, m_Duration);
            m_CritRate.DOValue(roleData.Crit / m_MaxValue, m_Duration);
            m_Luck.DOValue(roleData.Luck / m_MaxValue, m_Duration);
        }
    }
}
