namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 座位系统
    /// </summary>
    public class Seat : System<SeatInfo>
    {
        protected override void OnAssemble()
        {
            base.OnAssemble();
            World.Eventor.Listen<EntityRemoveEvent>(this, OnEntityRmv);
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            World.Eventor.UnListen<EntityRemoveEvent>(this, OnEntityRmv);
        }

        /// <summary>
        /// 根据座位 ID 获取 EntityID
        /// </summary>
        /// <param name="seat">座位 ID</param>
        /// <returns>EntityID</returns>
        public ulong GetEntity(ulong seat)
        {
            if (Component.SeatDict.TryGetValue(seat, out var entity)) return entity;

            return 0;
        }

        /// <summary>
        /// 根据 EntityID 获取座位 ID
        /// </summary>
        /// <param name="entity">EntityID</param>
        /// <returns>座位 ID</returns>
        public ulong GetSeat(ulong entity)
        {
            if (Component.EntityDict.TryGetValue(entity, out var seat)) return seat;

            return 0;
        }

        /// <summary>
        /// 坐下
        /// </summary>
        /// <param name="seat">座位 ID</param>
        /// <param name="entity">EntityID</param>
        public void Sitdown(ulong seat, ulong entity)
        {
            if (Component.SeatDict.ContainsKey(seat)) Component.SeatDict.Remove(seat);
            if (Component.EntityDict.ContainsKey(entity)) Component.EntityDict.Remove(entity);

            Component.SeatDict.Add(seat, entity);
            Component.EntityDict.Add(entity, seat);
        }

        /// <summary>
        /// 站起
        /// </summary>
        /// <param name="entity">EntityID</param>
        public void Standup(ulong entity)
        {
            if (false == Component.EntityDict.TryGetValue(entity, out var seat)) return;

            Component.EntityDict.Remove(entity);
            Component.SeatDict.Remove(seat);
        }

        private void OnEntityRmv(EntityRemoveEvent e)
        {
            // 站起
            Standup(e.Entity);
        }
    }
}
