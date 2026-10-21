using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;

namespace GameLogic
{
    public class UIHallResource : MonoBehaviour
    {
        public const int EnergyCid = 1;
        public const int GoldCid = 11;
        public const int DiamondCid = 12;

        [SerializeField]
        [LabelText("体力数量")]
        private TextMeshProUGUI m_EnergyCount;

        [SerializeField]
        [LabelText("金币数量")]
        private TextMeshProUGUI m_GoldCount;

        [SerializeField]
        [LabelText("钻石数量")]
        private TextMeshProUGUI m_DiamondCount;

        private void Awake()
        {
            // EventBusComponent.Instance.Subscribe(EventConst.SetPackData, OnSetPackData);
        }

        private void OnDestroy()
        {
            // EventBusComponent.Instance?.Unsubscribe(EventConst.SetPackData, OnSetPackData);
        }

        private void OnSetPackData(object obj)
        {
            //var packData = (PackData)obj;

            //var energyItems = packData.EnergyItems;
            //foreach (var item in energyItems)
            //{
            //    if (item.Cid == EnergyCid)
            //    {
            //        m_EnergyCount.text = item.Count.ToString();
            //    }
            //}

            //var currencyItems = packData.CurrencyItems;

            //foreach (var item in currencyItems)
            //{
            //    if (item.Cid == GoldCid)
            //    {
            //        m_GoldCount.text = item.Count.ToString();
            //    }
            //    if (item.Cid == DiamondCid)
            //    {
            //        m_DiamondCount.text = item.Count.ToString();
            //    }
            //}
        }
    }
}
