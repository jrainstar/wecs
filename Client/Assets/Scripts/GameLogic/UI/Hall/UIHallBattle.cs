using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


namespace GameLogic
{
    public class UIHallBattle : MonoBehaviour
    {
        [SerializeField]
        [LabelText("英雄面板")]
        private Button m_ToBattle;


        private void Awake()
        {
            m_ToBattle.onClick.AddListener(ToBattle);
        }

        private void ToBattle()
        {
            GameEntry.Instance.ChangeState(GameState.Gameplay);
        }

        private void OnDestroy()
        {
            m_ToBattle.onClick.RemoveListener(ToBattle);
        }
    }
}
