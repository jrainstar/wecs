using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 节点代理赋能, 有空间状态时创建节点代理
    /// </summary>
    public class NodeEnchant : AgentEnchant<RIL_SPATIAL>
    {
        protected override void OnRIL(RIL_SPATIAL ril)
        {
            rilbucket.world.EnsureAgent<NodeAgent>(ril.actor);
        }
    }
}
