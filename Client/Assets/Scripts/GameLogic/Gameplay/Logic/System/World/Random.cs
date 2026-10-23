using GameLogic.Gameplay.Shared;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 确定性, 随机器
    /// </summary>
    public class Random : System<RandomInfo>
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="s">种子</param>
        public void Initialze(int s)
        {
            Component.Seed = s;
            Component.Current = Component.Seed;
        }

        /// <summary>
        /// 整数范围随机
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>结果</returns>
        public int Range(int min, int max)
        {
            Component.Current = (Component.A * Component.Current + Component.C) % Component.M;

            return (int)(min + (Component.Current % (max - min)));
        }

        /// <summary>
        /// 确定性, 浮点数范围随机
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>结果</returns>
        public FP Range(FP min, FP max)
        {
            return Range((min * 10000).AsInt(), (max * 10000).AsInt()) * FP.EN4;
        }
    }
}
