using GameConf.Gameplay;
using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// BUFF 行为 (单位级)
    /// 触发时机与 AOE 保持一致: 创建 / 周期 / 叠层 / 销毁, 每次触发都派发配置负载,
    /// 具体做什么由 Payload 的执行器决定;
    /// 因为不带委托, BUFF 的层数、剩余时长、周期进度都能随快照/回滚完整恢复
    /// </summary>
    public class Buff : Behavior<BuffInfo>
    {
        /// <summary>
        /// 本帧待派发的触发
        /// 先收集再统一派发, 派发期间施加/移除 BUFF 不会打乱正在进行的遍历
        /// </summary>
        private struct Firing
        {
            public int id;
            public ulong caster;
            public BuffTrigger trigger;
        }

        /// <summary>
        /// 本帧待派发的触发缓冲
        /// </summary>
        private List<Firing> firings { get; set; }

        protected override void OnAssemble()
        {
            base.OnAssemble();
            firings = ObjectCache.Ensure<List<Firing>>();
            firings.Clear();
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            if (null == firings) return;

            firings.Clear();
            ObjectCache.Set(firings);
            firings = null;
        }

        /// <summary>
        /// 施加 BUFF, 已经挂着时按叠层策略处理
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <param name="caster">施加者</param>
        /// <param name="stack">本次施加的层数</param>
        /// <returns>YES/NO</returns>
        public bool Apply(int id, ulong caster, int stack = 1)
        {
            if (0 >= id) return false;
            if (false == Config(id, out var data)) return false;

            var buffinfo = info;
            if (null == buffinfo || null == buffinfo.slots) return false;
            if (stack <= 0) stack = 1;

            var slot = Find(id);
            if (null == slot)
            {
                slot = ObjectCache.Ensure<BuffSlot>();
                slot.Ready();

                slot.id = id;
                slot.caster = caster;
                slot.duration = data.Duration;
                slot.triggers = Triggers(data);
                slot.layers.Add(slot.duration);
                buffinfo.slots.Add(slot);

                Fire(id, caster, BuffTrigger.Create);
                return true;
            }

            // 已经挂着: 施法者与配置以最新一次为准, 配置热更后也能跟上
            if (0 != caster) slot.caster = caster;
            slot.duration = data.Duration;
            slot.triggers = Triggers(data);

            var before = slot.layers.Count;
            var max = data.MaxStack > 0 ? data.MaxStack : 1;
            var layers = slot.layers;

            if (BuffStackPolicy.Independent == data.StackPolicy)
            {
                // 独立: 每层单独计时, 新层从完整时长开始, 旧层不受影响
                for (int i = 0; i < stack && layers.Count < max; i++) layers.Add(slot.duration);
            }
            else
            {
                // 刷新/叠加: 所有层共享一份计时, 重复施加统一重置
                var count = BuffStackPolicy.Stack == data.StackPolicy ? System.Math.Min(max, layers.Count + stack) : 1;
                layers.Clear();
                for (int i = 0; i < count; i++) layers.Add(slot.duration);
            }

            if (layers.Count > before) Fire(slot.id, slot.caster, BuffTrigger.Stack);

            return true;
        }

        /// <summary>
        /// 移除 BUFF (驱散)
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <returns>YES/NO</returns>
        public bool Remove(int id)
        {
            var index = IndexOf(id);
            if (index < 0) return false;

            var buffinfo = info;
            var slot = buffinfo.slots[index];
            var caster = slot.caster;

            // 先摘出列表再回池, 避免回池后的对象仍被列表引用
            buffinfo.slots.RemoveAt(index);
            slot.Reset();
            ObjectCache.Set(slot);

            // 驱散与自然到期走同样的销毁触发
            Fire(id, caster, BuffTrigger.Destroy);

            return true;
        }

        /// <summary>
        /// 移除全部 BUFF
        /// </summary>
        public void RemoveAll()
        {
            var buffinfo = info;
            if (null == buffinfo || null == buffinfo.slots) return;

            // 先全部摘掉再统一派发, 销毁触发里重新施加的 BUFF 不会被打乱
            var removed = ObjectCache.Ensure<List<Firing>>();
            var slots = buffinfo.slots;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                removed.Add(new Firing
                {
                    id = slot.id,
                    caster = slot.caster,
                    trigger = BuffTrigger.Destroy,
                });

                // 先摘出列表再回池, 避免回池后的对象仍被列表引用
                slots.RemoveAt(i);
                slot.Reset();
                ObjectCache.Set(slot);
            }

            foreach (var firing in removed) Fire(firing.id, firing.caster, firing.trigger);

            removed.Clear();
            ObjectCache.Set(removed);
        }

        /// <summary>
        /// 是否挂着指定 BUFF
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <returns>YES/NO</returns>
        public bool Has(int id)
        {
            return IndexOf(id) >= 0;
        }

        /// <summary>
        /// 查询层数
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <returns>层数, 没挂着返回 0</returns>
        public int Query(int id)
        {
            var slot = Find(id);

            return null != slot && null != slot.layers ? slot.layers.Count : 0;
        }

        /// <summary>
        /// 查询可以接续的技能
        /// 连招窗口这类 BUFF 在配置里指定"前置技能 → 接续技能";
        /// 用技能 ID 匹配而不是槽位序号, 技能换到别的槽位后依然有效;
        /// 多个窗口同时存在时取最先施加的那个
        /// </summary>
        /// <param name="source">槽位上当前装的技能 ID</param>
        /// <param name="buffid">提供接续的 BUFF 配置 ID</param>
        /// <returns>接续技能 ID, 没有窗口返回 0</returns>
        public uint Chain(uint source, out int buffid)
        {
            buffid = 0;

            var buffinfo = info;
            if (null == buffinfo || null == buffinfo.slots) return 0;

            foreach (var slotinfo in buffinfo.slots)
            {
                if (null == slotinfo) continue;
                if (false == Config(slotinfo.id, out var data)) continue;
                if (0 >= data.Skill) continue;
                // 0 表示任意槽位, 否则只对装着前置技能的槽位生效
                if (0 != data.Source && (uint)data.Source != source) continue;

                buffid = slotinfo.id;
                return (uint)data.Skill;
            }

            return 0;
        }

        protected override void OnTick(FP tick)
        {
            var buffinfo = info;
            if (null == buffinfo || null == buffinfo.slots || 0 == buffinfo.slots.Count) return;
            if (tick <= FP.Zero) return;

            var slots = buffinfo.slots;

            // 倒序遍历: 到期移除不影响尚未遍历的下标
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                if (false == Config(slot.id, out var data))
                {
                    // 配置丢失 (漏配/热更), 直接移除, 不派发负载
                    slots.RemoveAt(i);
                    slot.Reset();
                    ObjectCache.Set(slot);
                    continue;
                }

                Period(slot, data, tick);

                if (slot.duration > FP.Zero && Expire(slot, tick))
                {
                    firings.Add(new Firing
                    {
                        id = slot.id,
                        caster = slot.caster,
                        trigger = BuffTrigger.Destroy,
                    });

                    // 先摘出列表再回池, 避免回池后的对象仍被列表引用
                    slots.RemoveAt(i);
                    slot.Reset();
                    ObjectCache.Set(slot);
                }
            }

            FireAll();
        }

        /// <summary>
        /// 周期触发
        /// 到点只补一次, 卡帧时不连续爆发多段
        /// </summary>
        /// <param name="slot">BUFF 数据</param>
        /// <param name="data">BUFF 配置</param>
        /// <param name="tick">步长</param>
        private void Period(BuffSlot slot, BuffData data, FP tick)
        {
            if (0 == (slot.triggers & (1 << (int)BuffTrigger.Tick))) return;

            var period = (FP)data.TickTime;
            if (period <= FP.Zero) return;

            slot.tickelapsed += tick;
            if (slot.tickelapsed < period) return;

            // 多出来的时间留在累计里, 长时间运行不漂移
            slot.tickelapsed -= period;
            if (slot.tickelapsed >= period || slot.tickelapsed < FP.Zero) slot.tickelapsed = FP.Zero;

            firings.Add(new Firing
            {
                id = slot.id,
                caster = slot.caster,
                trigger = BuffTrigger.Tick,
            });
        }

        /// <summary>
        /// 层倒计时, 判定期满
        /// </summary>
        /// <param name="slot">BUFF 数据</param>
        /// <param name="tick">步长</param>
        /// <returns>YES 表示层已全部到期</returns>
        private static bool Expire(BuffSlot slot, FP tick)
        {
            var layers = slot.layers;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var remaining = layers[i] - tick;
                if (remaining <= FP.Zero) layers.RemoveAt(i);
                else layers[i] = remaining;
            }

            return 0 == layers.Count;
        }

        /// <summary>
        /// 派发本帧收集到的触发
        /// </summary>
        private void FireAll()
        {
            if (null == firings || 0 == firings.Count) return;

            for (int i = 0; i < firings.Count; i++)
            {
                var firing = firings[i];
                Fire(firing.id, firing.caster, firing.trigger);
            }

            firings.Clear();
        }

        /// <summary>
        /// 派发指定触发时机的负载
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <param name="caster">施加者</param>
        /// <param name="trigger">触发时机</param>
        private void Fire(int id, ulong caster, BuffTrigger trigger)
        {
            if (false == Config(id, out var data)) return;
            if (null == data.Events) return;

            var payload = stage.payload;
            if (null == payload) return;

            foreach (var e in data.Events)
            {
                if (null == e) continue;
                if (e.Trigger != trigger) continue;
                if (0 >= e.PayloadId) continue;

                // 施法者是施加 BUFF 的人, 目标是 BUFF 的持有者
                payload.Play(e.PayloadId, caster, actor);
            }
        }

        /// <summary>
        /// 计算配置了哪些触发时机 (BuffTrigger 位掩码)
        /// </summary>
        /// <param name="data">BUFF 配置</param>
        /// <returns>触发时机位掩码</returns>
        private static ushort Triggers(BuffData data)
        {
            ushort triggers = 0;
            if (null == data || null == data.Events) return triggers;

            foreach (var e in data.Events)
            {
                if (null == e) continue;

                triggers |= (ushort)(1 << (int)e.Trigger);
            }

            return triggers;
        }

        /// <summary>
        /// 查找 BUFF 数据
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <returns>BUFF 数据, 没挂着返回 null</returns>
        private BuffSlot Find(int id)
        {
            var index = IndexOf(id);
            if (index < 0) return null;

            return info.slots[index];
        }

        /// <summary>
        /// 查找 BUFF 下标
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <returns>下标, 没挂着返回 -1</returns>
        private int IndexOf(int id)
        {
            if (0 >= id) return -1;

            var buffinfo = info;
            if (null == buffinfo || null == buffinfo.slots) return -1;

            var slots = buffinfo.slots;
            for (int i = 0; i < slots.Count; i++) if (slots[i].id == id) return i;

            return -1;
        }

        /// <summary>
        /// 读取 BUFF 配置
        /// </summary>
        /// <param name="id">BUFF 配置 ID</param>
        /// <param name="data">BUFF 配置</param>
        /// <returns>YES/NO</returns>
        private bool Config(int id, out BuffData data)
        {
            data = null;
            if (0 >= id) return false;
            if (null == stage.cfg) return false;

            return stage.cfg.location.TbBuffData.TryGetValue(id, out data) && null != data;
        }
    }
}
