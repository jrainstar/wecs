using GameEngine;

namespace GameLogic
{
    public class HeroSys : GameSystem<HeroSys>
    {
        public HeroData HeroData { get; set; } = new HeroData();

        public override void OnInit()
        {
            base.OnInit();
            EventModule.Instance.Subscribe(EventDefine.GetHeroData, OnGetHeroData);
            EventModule.Instance.Subscribe(EventDefine.SetHeroData, OnSetHeroData);
            EventModule.Instance.Subscribe(EventDefine.SaveHeroData, OnSaveHeroData);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // 请求英雄数据（当前走本地 SaveModule，后续可改为服务端）
            EventModule.Instance.Dispatch(EventDefine.GetHeroData);
        }

        public override void OnClear()
        {
            EventModule.Instance?.Unsubscribe(EventDefine.GetHeroData, OnGetHeroData);
            EventModule.Instance?.Unsubscribe(EventDefine.SetHeroData, OnSetHeroData);
            EventModule.Instance?.Unsubscribe(EventDefine.SaveHeroData, OnSaveHeroData);
            base.OnClear();
        }

        // ── GetHeroData（请求 → 加载 → 返回）─────────────────────

        private void OnGetHeroData(object obj)
        {
            var saveData = SaveModule.Instance.Load(SaveDefine.Hero, HeroSaveData.Default);
            HeroData.Cid = saveData.Cid;

            EventModule.Instance.Dispatch(EventDefine.AckGetHeroData, HeroData);
        }

        // ── SetHeroData（设置 → 保存 → 通知）─────────────────────

        private void OnSetHeroData(object obj)
        {
            var heroCfgId = (int)obj;
            HeroData.Cid = heroCfgId;

            EventModule.Instance.Dispatch(EventDefine.SaveHeroData);
            EventModule.Instance.Dispatch(EventDefine.AckSetHeroData, HeroData);
        }

        // ── SaveHeroData（持久化）────────────────────────────────

        private void OnSaveHeroData(object obj)
        {
            var saveData = new HeroSaveData { Cid = HeroData.Cid };
            SaveModule.Instance.Save(SaveDefine.Hero, saveData);
            EventModule.Instance.Dispatch(EventDefine.AckSaveHeroData);
        }
    }

}
