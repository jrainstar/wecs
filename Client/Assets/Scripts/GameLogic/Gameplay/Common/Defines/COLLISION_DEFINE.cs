using System.Collections.Generic;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 碰撞定义
    /// </summary>
    public class COLLISION_DEFINE
    {
        /// <summary>
        /// 立方体
        /// </summary>
        public const byte COLLIDER_BOX = 1;
        /// <summary>
        /// 球体
        /// </summary>
        public const byte COLLIDER_SPHERE = 2;
        /// <summary>
        /// 胶囊体
        /// </summary>
        public const byte COLLIDER_CAPSULE = 3;
        /// <summary>
        /// 圆柱体
        /// </summary>
        public const byte COLLIDER_CYLINDER = 4;

        /// <summary>
        /// 碰撞类型 - 攻击盒
        /// </summary>
        public const byte COLLISION_TYPE_HURT = 1;
        /// <summary>
        /// 碰撞类型 - 嗅探器
        /// </summary>
        public const byte COLLISION_TYPE_SENSOR = 2;

        /// <summary>
        /// 碰撞射线
        /// </summary>
        public const byte COLLISION_RAY = 1;
        /// <summary>
        /// 碰撞线段
        /// </summary>
        public const byte COLLISION_LINE = 2;
        /// <summary>
        /// 碰撞立方体
        /// </summary>
        public const byte COLLISION_BOX = 3;
        /// <summary>
        /// 碰撞球体
        /// </summary>
        public const byte COLLISION_SPHERE = 4;

        /// <summary>
        /// 默认
        /// </summary>
        public const int LAYER_DEFAULT = 1;
        /// <summary>
        /// 地板
        /// </summary>
        public const int LAYER_GROUND = 2;
        /// <summary>
        /// 玩家
        /// </summary>
        public const int LAYER_PLAYER = 4;
        /// <summary>
        /// AOE (只由 AOE 自身做重叠检测, 不参与其它单位的推挤/碰撞)
        /// </summary>
        public const int LAYER_AOE = 8;

        /// <summary>
        /// 物理层检测掩码
        /// 注意 LAYER_AOE 只单向查询: AOE 能检测到默认层/玩家, 但默认层/玩家/地面的掩码里都不含 AOE,
        /// 因此 AOE 不会被角色移动推挤, 也不会挡住角色
        /// </summary>
        private static Dictionary<int, int> maskdict = new()
        {
            { LAYER_DEFAULT, LAYER_DEFAULT | LAYER_GROUND | LAYER_PLAYER },
            { LAYER_GROUND , LAYER_DEFAULT | LAYER_PLAYER },
            { LAYER_PLAYER , LAYER_DEFAULT | LAYER_GROUND },
            { LAYER_AOE , LAYER_DEFAULT | LAYER_PLAYER },
        };

        /// <summary>
        /// 查询层关系
        /// </summary>
        /// <param name="self">自身层</param>
        /// <param name="target">目标层</param>
        /// <returns>YES/NO</returns>
        public static bool QUERY(int self, int target)
        {
            if (maskdict.TryGetValue(self, out var mask))
            {
                return 0 != (mask & target);
            }

            return false;
        }
    }
}