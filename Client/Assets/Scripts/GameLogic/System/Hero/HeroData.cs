using System.Collections.Generic;

namespace GameLogic
{
    public class HeroData
    {
        public int Cid;
    }

    public class HeroSaveData
    {
        public int Cid;

        public static HeroSaveData Default = new HeroSaveData() { Cid = 0 };
    }
}
