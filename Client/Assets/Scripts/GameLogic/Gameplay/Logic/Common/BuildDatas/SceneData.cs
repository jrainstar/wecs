using System;
using GameLogic.Gameplay.Common;
using UnityEngine;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 场景数据
    /// 由编辑器工具 (Tools/场景/场景碰撞导出) 从 Unity 场景中的物理碰撞盒导出,
    /// 运行时挂在 StageData.scene 上, 由 Stage.Builds 逐个生成墙体 Actor。
    /// </summary>
    [Serializable]
    public struct SceneData
    {
        /// <summary>
        /// 场景名称, 同时作为导出文件名
        /// </summary>
        public string name;
        /// <summary>
        /// 碰撞物件集合, 为空表示没有场景数据
        /// </summary>
        public CollisionData[] colliders;

        /// <summary>
        /// 从 JSON 解析场景数据
        /// </summary>
        /// <param name="json">JSON 文本</param>
        /// <returns>场景数据, 文本为空时返回 default (colliders 为 null)</returns>
        public static SceneData FromJson(string json)
        {
            return string.IsNullOrEmpty(json) ? default : JsonUtility.FromJson<SceneData>(json);
        }

        /// <summary>
        /// 从 TextAsset 解析场景数据
        /// </summary>
        /// <param name="asset">场景数据文件</param>
        /// <returns>场景数据, 资源为空时返回 default (colliders 为 null)</returns>
        public static SceneData FromJson(TextAsset asset)
        {
            return null != asset ? FromJson(asset.text) : default;
        }
    }

    /// <summary>
    /// 单个碰撞物件数据
    /// 直接使用 Unity 的浮点数据 (米/度), 只在 Apply 时一次性转成 FP,
    /// 转换后即进入 SpatialInfo / ColliderInfo, 后续逻辑帧不参与浮点运算, 不影响确定性。
    /// 形状由 shape 决定, 只读取对应的字段:
    /// BOX -> size; SPHERE -> radius; CAPSULE/CYLINDER -> radius + height
    /// </summary>
    [Serializable]
    public struct CollisionData
    {
        /// <summary>
        /// 碰撞层 (COLLISION_DEFINE.LAYER_*), 0 表示默认层
        /// </summary>
        public int layer;
        /// <summary>
        /// 几何体类型 (COLLISION_DEFINE.COLLIDER_*)
        /// </summary>
        public int shape;
        /// <summary>
        /// 世界坐标位置 (碰撞盒中心)
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// 世界坐标欧拉角 (度)
        /// </summary>
        public Vector3 euler;
        /// <summary>
        /// 立方体尺寸
        /// </summary>
        public Vector3 size;
        /// <summary>
        /// 球体/胶囊体/圆柱体半径
        /// </summary>
        public float radius;
        /// <summary>
        /// 胶囊体/圆柱体高度 (包含两端半球的全高)
        /// </summary>
        public float height;

        /// <summary>
        /// 写入空间信息, 缩放固定为 1 (尺寸已按世界缩放烘焙进形状数据)
        /// </summary>
        /// <param name="spatial">空间信息</param>
        public void Apply(SpatialInfo spatial)
        {
            spatial.position = new FPVector3(position.x, position.y, position.z);
            spatial.euler = new FPVector3(euler.x, euler.y, euler.z);
            spatial.scale = FP.One;
        }

        /// <summary>
        /// 写入碰撞盒信息
        /// </summary>
        /// <param name="collider">碰撞盒</param>
        public void Apply(ColliderInfo collider)
        {
            if (null == collider) return;

            switch (shape)
            {
                case COLLISION_DEFINE.COLLIDER_BOX:
                    collider.shape = COLLISION_DEFINE.COLLIDER_BOX;
                    collider.box = new BoxShape
                    {
                        offset = FPVector3.zero,
                        size = new FPVector3(size.x, size.y, size.z),
                    };
                    break;
                case COLLISION_DEFINE.COLLIDER_SPHERE:
                    collider.shape = COLLISION_DEFINE.COLLIDER_SPHERE;
                    collider.sphere = new SphereShape
                    {
                        offset = FPVector3.zero,
                        radius = radius,
                    };
                    break;
                case COLLISION_DEFINE.COLLIDER_CAPSULE:
                    collider.shape = COLLISION_DEFINE.COLLIDER_CAPSULE;
                    collider.capsule = new CapsuleShape
                    {
                        offset = FPVector3.zero,
                        radius = radius,
                        height = height,
                    };
                    break;
                case COLLISION_DEFINE.COLLIDER_CYLINDER:
                    collider.shape = COLLISION_DEFINE.COLLIDER_CYLINDER;
                    collider.cylinder = new CylinderShape
                    {
                        offset = FPVector3.zero,
                        radius = radius,
                        height = height,
                    };
                    break;
                default:
                    // 未知形状: 留空, 检测层会直接忽略
                    collider.shape = 0;
                    return;
            }

            collider.layer = layer > 0 ? layer : COLLISION_DEFINE.LAYER_DEFAULT;
        }
    }
}
