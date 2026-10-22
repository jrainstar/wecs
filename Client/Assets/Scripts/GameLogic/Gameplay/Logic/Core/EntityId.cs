
//using System;

//namespace GameLogic.Gameplay.Logic
//{
//    /// <summary>
//    /// Entity 的唯一 ID, 0 表示无效。
//    /// <para>同时携带一个 <see cref="Version"/> 版本号, 用于解决 Entity 销毁后 Index 被复用导致的悬垂引用。</para>
//    /// <para>结构为值类型, 可作为 Dictionary Key 且零 GC。</para>
//    /// </summary>
//    public readonly struct EntityId : IEquatable<EntityId>
//    {
//        /// <summary>无效 ID。</summary>
//        public static readonly EntityId Invalid = default;

//        /// <summary>槽位下标(递增复用)。</summary>
//        public readonly int Index;

//        /// <summary>版本号(标记同一 Index 的第几代), 从 1 开始, 0 表示无效。</summary>
//        public readonly int Version;

//        public EntityId(int index, int version)
//        {
//            Index = index;
//            Version = version;
//        }

//        /// <summary>是否为有效 ID。</summary>
//        public bool IsValid => Version != 0;

//        public bool Equals(EntityId other) => Index == other.Index && Version == other.Version;

//        public override bool Equals(object obj) => obj is EntityId other && Equals(other);

//        public override int GetHashCode() => Index * 397 ^ Version;

//        public override string ToString() => IsValid ? $"Entity({Index}:{Version})" : "Entity(Invalid)";

//        public static bool operator ==(EntityId a, EntityId b) => a.Equals(b);

//        public static bool operator !=(EntityId a, EntityId b) => !a.Equals(b);
//    }
//}

