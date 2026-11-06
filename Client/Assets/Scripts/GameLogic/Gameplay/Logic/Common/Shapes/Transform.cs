using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 三维刚性变换: 位置与旋转, 不包含缩放。
    /// 窄相检测统一以该结构表达形状在世界空间中的摆放。
    /// </summary>
    public struct Transform
    {
        /// <summary>
        /// 位置
        /// </summary>
        public FPVector3 position;
        /// <summary>
        /// 旋转
        /// </summary>
        public FPQuaternion rotation;

        public static readonly Transform identity = new Transform(FPVector3.zero, FPQuaternion.identity);

        public Transform(FPVector3 position, FPQuaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public FPVector3 right => TransformDirection(FPVector3.right);
        public FPVector3 up => TransformDirection(FPVector3.up);
        public FPVector3 forward => TransformDirection(FPVector3.forward);

        /// <summary>
        /// 局部点转到世界空间
        /// </summary>
        public FPVector3 LocalToWorld(FPVector3 point) => position + rotation * point;

        /// <summary>
        /// 世界点转到局部空间
        /// </summary>
        public FPVector3 WorldToLocal(FPVector3 point) => FPQuaternion.Conjugate(rotation) * (point - position);

        /// <summary>
        /// 局部方向转到世界空间
        /// </summary>
        public FPVector3 TransformDirection(FPVector3 direction) => rotation * direction;

        /// <summary>
        /// 世界方向转到局部空间
        /// </summary>
        public FPVector3 InverseTransformDirection(FPVector3 direction) => FPQuaternion.Conjugate(rotation) * direction;
    }
}
