using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 特效翻译器
    /// elapsed 不进哈希, 只有特效增删/参数变化才重发; 渲染层自己按渲染帧推进采样时间,
    /// 重发时用 RIL 里的 elapsed 做一次对齐 (回滚/重播时逻辑时间会倒退)
    /// </summary>
    public class EffectTranslator : Translator<EffectInfo, RIL_FACADE_EFFECT>
    {
        public override ushort id => RIL_DEFINE.FACADE_EFFECT;

        protected override int OnCalcHashCode(EffectInfo info)
        {
            int hash = info.actor.GetHashCode();
            if (null == info.effectdict) return hash;

            // 字典遍历顺序不保证稳定, 逐个特效求和, 与顺序无关
            foreach (var kv in info.effectdict)
            {
                var data = kv.Value;
                int effecthash = kv.Key.GetHashCode();
                effecthash = effecthash * 31 + data.fx;
                effecthash = effecthash * 31 + data.sound;
                effecthash = effecthash * 31 + (data.anchor != null ? data.anchor.GetHashCode() : 0);
                effecthash = effecthash * 31 + data.follow.GetHashCode();
                effecthash = effecthash * 31 + data.offset.x.GetHashCode();
                effecthash = effecthash * 31 + data.offset.y.GetHashCode();
                effecthash = effecthash * 31 + data.offset.z.GetHashCode();
                effecthash = effecthash * 31 + data.scale.GetHashCode();
                effecthash = effecthash * 31 + data.duration.GetHashCode();

                hash += effecthash;
            }

            return hash;
        }

        protected override void OnRIL(EffectInfo info, RIL_FACADE_EFFECT ril)
        {
            if (null == info.effectdict) return;

            foreach (var kv in info.effectdict)
            {
                var data = kv.Value;

                var effect = RILCache.Ensure<RIL_EFFECT>();
                effect.id = kv.Key;
                effect.fx = data.fx;
                effect.sound = data.sound;
                effect.anchor = data.anchor;
                effect.follow = data.follow;
                effect.offset = data.offset;
                effect.scale = data.scale;
                effect.elapsed = data.elapsed;
                effect.duration = data.duration;

                ril.effectdict.Add(kv.Key, effect);
            }
        }
    }
}
