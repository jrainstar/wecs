using GameConf.Gameplay;
using GameConf.Gameplay.Role;
using GameEngine;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIRoleItem : MonoBehaviour
    {
        private RoleData m_RoleData;
        public RoleData RoleData => m_RoleData;

        [SerializeField]
        [LabelText("名称")]
        private TextMeshProUGUI m_Text_Name;

        [SerializeField]
        [LabelText("图标")]
        private Image m_Image_Icon;

        [SerializeField]
        [LabelText("按钮")]
        private Button m_Button;

        [SerializeField]
        [LabelText("选中")]
        private GameObject m_Focus;

        private Action<int> m_OnClickItemCallBack;

        public void SetData(RoleData heroData)
        {
            m_RoleData = heroData;

            m_Text_Name.text = heroData.Name;

            var sprite = ResourceModule.Instance.LoadSpriteSync(heroData.Icon);
            m_Image_Icon.sprite = sprite;
        }

        public void Init(Action<int> onClickItemCallBack = null)
        {
            m_OnClickItemCallBack = onClickItemCallBack;
            m_Button = GetComponent<Button>();
            if (m_Button != null)
            {
                m_Button.onClick.AddListener(OnButtonClicked);
            }
        }

        void OnButtonClicked()
        {
            if (m_OnClickItemCallBack != null)
            {
                m_OnClickItemCallBack(RoleData.Id);
            }
        }

        public void SetItemData(RoleData roleData)
        {
            m_RoleData = roleData;
            m_Text_Name.text = roleData.Name;
            SetSprite();
        }

        public void SetItemSelected(bool select)
        {
            m_Focus.SetActive(select);
        }

        private void SetSprite()
        {
            var sprite = ResourceModule.Instance.LoadSpriteSync(RoleData.Icon);
            m_Image_Icon.sprite = sprite;
        }
    }
}
