using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


namespace GameLogic
{
    public class UIHallMainMenu : MonoBehaviour
    {
        [SerializeField]
        [LabelText("英雄面板")]
        private Button m_ToHero;

        [SerializeField]
        [LabelText("库存面板")]
        private Button m_ToInventory;

        [SerializeField]
        [LabelText("技能面板")]
        private Button m_ToUpgrade;

        [SerializeField]
        [LabelText("商店面板")]
        private Button m_ToShop;


        private void Awake()
        {
            m_ToHero.onClick.AddListener(ToHero);
            m_ToInventory.onClick.AddListener(ToInventory);
        }

        private void ToHero()
        {
            // UIComponent.Instance.ShowWindowSync<HeroWindow>();
            GameEntry.Instance.ChangeState(GameState.UIHero);
        }

        private void ToSkill()
        {

        }

        private void ToInventory()
        {
            // UIComponent.Instance.ShowWindowSync<EquipWindow>();
        }

        private void OnDestroy()
        {
            m_ToHero.onClick.RemoveListener(ToHero);
            m_ToInventory.onClick.RemoveListener(ToInventory);
        }
    }
}
