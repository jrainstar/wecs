using GameConf.Gameplay;
using Sirenix.OdinInspector;
using SuperScrollView;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameLogic
{
    public class UIRoleItems : MonoBehaviour
    {
        [SerializeField]
        [LabelText("循环列表")]
        private LoopGridView m_GridView;

        private List<RoleData> m_RoleDatas;

        private int m_CurrentSelectItemId = -1;

        /// <summary>
        /// 当前选中英雄变化时回调，参数为选中的 HeroCfg
        /// </summary>
        public event Action<RoleData> OnSelectedChanged;

        /// <summary>
        /// 当前选中的英雄数据
        /// </summary>
        public RoleData CurrentHeroData
        {
            get
            {
                if (m_RoleDatas == null || m_CurrentSelectItemId < 0)
                {
                    return null;
                }
                return m_RoleDatas.Find(d => d.Id == m_CurrentSelectItemId);
            }
        }

        private void Awake()
        {
            InitHero();
        }

        private void InitHero()
        {
            m_RoleDatas = TableModule.Instance.Tables.TbRoleData.DataList.ToList();

            if (m_RoleDatas == null || m_RoleDatas.Count == 0)
            {
                return;
            }

            // 优先使用已保存的当前角色，否则默认选中第一个
            var savedCid = HeroSys.Instance.HeroData.Cid;
            if (savedCid > 0 && m_RoleDatas.Exists(d => d.Id == savedCid))
            {
                m_CurrentSelectItemId = savedCid;
            }
            else
            {
                m_CurrentSelectItemId = m_RoleDatas[0].Id;
            }

            m_GridView.InitGridView(m_RoleDatas.Count, OnGetItemByIndex);
        }

        /// <summary>
        /// 主动触发一次当前选中通知（用于外部初始化时刷新依赖项）
        /// </summary>
        public void NotifyCurrentSelected()
        {
            var data = CurrentHeroData;
            if (data != null)
            {
                OnSelectedChanged?.Invoke(data);
            }
        }

        private LoopGridViewItem OnGetItemByIndex(LoopGridView view, int index, int row, int column)
        {
            if (index < 0)
            {
                return null;
            }

            var itemData = m_RoleDatas[index];
            if (itemData == null)
            {
                return null;
            }

            LoopGridViewItem item = m_GridView.NewListViewItem("CharacterCard");

            UIRoleItem itemScript = item.GetComponent<UIRoleItem>();
            if (item.IsInitHandlerCalled == false)
            {
                item.IsInitHandlerCalled = true;
                itemScript.Init(OnItemClicked);
            }

            itemScript.SetItemData(itemData);
            itemScript.SetItemSelected(m_CurrentSelectItemId == itemData.Id);
            return item;
        }

        void OnItemClicked(int itemId)
        {
            if (m_CurrentSelectItemId == itemId)
            {
                return;
            }

            m_CurrentSelectItemId = itemId;
            m_GridView.RefreshAllShownItem();

            var data = CurrentHeroData;
            if (data != null)
            {
                OnSelectedChanged?.Invoke(data);
            }
        }

    }
}
