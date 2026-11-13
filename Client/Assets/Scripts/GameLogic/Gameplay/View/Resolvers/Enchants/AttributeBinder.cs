using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 属性代理赋能
    /// 有生命上限的单位才需要血条, 没有数值信息的对象 (例如 AOE) 不创建
    /// </summary>
    public class AttributeBinder : AgentBinder<RIL_ATTRIBUTE>
    {
        protected override void OnRIL(RIL_ATTRIBUTE ril)
        {
            // 有生命上限 => 是角色单位, 创建血条代理
            if (ril.maxhp > 0)
            {
                rilbucket.world.EnsureAgent<HealthBarAgent>(ril.actor);
            }
            else
            {
                RecycleAgent(ril.actor);
            }
        }

        protected override void OnLossRIL(RIL_LOSS ril)
        {
            base.OnLossRIL(ril);
            RecycleAgent(ril.actor);
        }

        /// <summary>
        /// 回收血条代理
        /// </summary>
        /// <param name="actor">ActorID</param>
        private void RecycleAgent(ulong actor)
        {
            var agent = rilbucket.world.GetAgent<HealthBarAgent>(actor);
            if (null == agent) return;
            rilbucket.world.RmvAgent(agent);
        }
    }
}
