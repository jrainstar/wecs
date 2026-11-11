using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 特效行为
    /// 逻辑层只维护特效的存在时长与参数, 具体表现由渲染层的 EffectAgent 按
    /// RIL_FACADE_EFFECT 里的 elapsed 采样 EffectController, 到点后由本行为移除
    /// </summary>
    public class Effect : Behavior<EffectInfo>
    {
        /// <summary>
        /// 默认存在时长 (秒), 配置没有填时长时使用
        /// </summary>
        public const float DEFAULT_DURATION = 2f;

        /// <summary>
        /// 播放特效
        /// </summary>
        /// <param name="fx">特效资源编号 (TbResourceData)</param>
        /// <param name="anchor">挂点骨骼</param>
        /// <param name="follow">是否跟随挂点</param>
        /// <param name="offset">相对挂点的偏移</param>
        /// <param name="rotate">相对挂点的旋转 (欧拉角, 度)</param>
        /// <param name="scale">缩放</param>
        /// <param name="duration">存在时长, 小于等于 0 使用默认时长</param>
        /// <returns>特效编号, 参数无效时返回 0</returns>
        public uint Play(int fx, string anchor, bool follow, FPVector3 offset, FPVector3 rotate, FP scale, FP duration)
        {
            if (fx <= 0) return 0;

            var effectinfo = info;
            if (null == effectinfo) return 0;

            var data = ObjectCache.Ensure<EffectData>();
            data.fx = fx;
            data.anchor = anchor;
            data.follow = follow;
            data.offset = offset;
            data.rotate = rotate;
            // 未配置缩放时按原尺寸播放
            data.scale = scale > FP.Zero ? scale : FP.One;
            data.elapsed = FP.Zero;
            data.duration = duration > FP.Zero ? duration : (FP)DEFAULT_DURATION;

            var id = ++effectinfo.increment;
            effectinfo.effectdict.Add(id, data);

            return id;
        }

        /// <summary>
        /// 停止特效
        /// </summary>
        /// <param name="id">特效编号</param>
        public void Stop(uint id)
        {
            var effectinfo = info;
            if (null == effectinfo || null == effectinfo.effectdict) return;
            if (false == effectinfo.effectdict.TryGetValue(id, out var data)) return;

            effectinfo.effectdict.Remove(id);
            data.Reset();
            ObjectCache.Set(data);
        }

        /// <summary>
        /// 停止所有特效
        /// </summary>
        public void StopAll()
        {
            var effectinfo = info;
            if (null == effectinfo || null == effectinfo.effectdict) return;

            foreach (var kv in effectinfo.effectdict)
            {
                kv.Value.Reset();
                ObjectCache.Set(kv.Value);
            }
            effectinfo.effectdict.Clear();
        }

        protected override void OnTick(FP tick)
        {
            var effectinfo = info;
            if (null == effectinfo || null == effectinfo.effectdict) return;
            if (0 == effectinfo.effectdict.Count) return;

            // 到点移除, 不能边遍历边删, 先收集编号
            List<uint> finished = null;
            foreach (var kv in effectinfo.effectdict)
            {
                var data = kv.Value;
                data.elapsed = data.elapsed + tick;
                if (data.elapsed < data.duration) continue;

                finished ??= ObjectCache.Ensure<List<uint>>();
                finished.Add(kv.Key);
            }

            if (null == finished) return;

            foreach (var id in finished) Stop(id);
            finished.Clear();
            ObjectCache.Set(finished);
        }
    }
}
