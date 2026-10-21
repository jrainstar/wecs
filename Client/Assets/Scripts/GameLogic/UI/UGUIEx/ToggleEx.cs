using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class ToggleEx : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_Toggle;

        [SerializeField]
        [LabelText("未选中")]
        private GameObject m_Unselect;

        [SerializeField]
        [LabelText("已选中")]
        private GameObject m_Select;

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            m_Toggle.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(bool isOn)
        {
            m_Select.SetActive(isOn);
            m_Unselect.SetActive(!isOn);
        }
    }

}
