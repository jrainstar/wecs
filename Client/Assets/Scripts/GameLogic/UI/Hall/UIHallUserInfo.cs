using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace GameLogic
{
    public class UIHallUserInfo : MonoBehaviour
    {
        [SerializeField]
        [LabelText("用户名称")]
        private TextMeshProUGUI m_UserName;

        [SerializeField]
        [LabelText("用户等级")]
        private TextMeshProUGUI m_UserLevel;

        [SerializeField]
        [LabelText("用户经验")]
        private TextMeshProUGUI m_UserExp;

        [SerializeField]
        [LabelText("用户经验滑条")]
        private Slider m_UserExpSlider;

        public TextMeshProUGUI UserName { get => m_UserName; set => m_UserName = value; }

        private void Awake()
        {

        }

        private void OnDestroy()
        {

        }

        private void OnAckGetUserData(object obj)
        {
            //var scUserInfo = (UserData)obj;
            //m_UserName.text = scUserInfo.Name;
            //m_UserLevel.text = scUserInfo.Level.ToString();
            //m_UserExp.text = $"{scUserInfo.CurExp}/{scUserInfo.NextExp}";
            //m_UserExpSlider.value = (float)scUserInfo.CurExp / scUserInfo.NextExp;
        }
    }
}
