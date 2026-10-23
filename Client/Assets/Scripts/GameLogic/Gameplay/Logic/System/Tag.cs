namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 标签
    /// </summary>
    public class Tag : System<TagInfo>
    {
        /// <summary>
        /// 拥有指定的标签
        /// </summary>
        /// <param name="key">标签 KEY</param>
        /// <returns>YES/NO</returns>
        public bool Has(ushort key)
        {
            return Component.Tags.ContainsKey(key);
        }

        /// <summary>
        /// 获取指定标签的值
        /// </summary>
        /// <param name="key">标签 KEY</param>
        /// <param name="tag">标签值</param>
        /// <returns>YES/NO</returns>
        public bool Get(ushort key, out long tag)
        {
            return Component.Tags.TryGetValue(key, out tag);
        }

        /// <summary>
        /// 移除指定的标签
        /// </summary>
        /// <param name="key">标签 KEY</param>
        public void Rmv(ushort key)
        {
            if (false == Component.Tags.ContainsKey(key)) return;

            Component.Tags.Remove(key);
        }

        /// <summary>
        /// 设置指定标签的值
        /// </summary>
        /// <param name="key">标签 KEY</param>
        /// <param name="tag">标签值</param>
        public void Set(ushort key, long tag)
        {
            if (Component.Tags.ContainsKey(key)) Component.Tags.Remove(key);
            Component.Tags.Add(key, tag);
        }
    }
}
