namespace GameLogic
{
    public enum EventDefine : uint
    {
        // 网络
        NetConnectComplete,
        NetConnectFail,
        NetDisconnect,

        // 用户数据
        GetUserData,
        AckGetUserData,
        SetUserData,
        AckSetUserData,

        // 英雄数据
        GetHeroData,
        AckGetHeroData,
        SetHeroData,
        AckSetHeroData,
        SaveHeroData,
        AckSaveHeroData,

        // 背包
        GetPackData,
        SetPackData,

        // 输入
        JoystickMove,
        JoystickAttackDown,
        JoystickAttackDrag,
        JoystickAttackUp,

        // 输入
        JoystickDown,
        JoystickDrag,
        JoystickUp,
    }
}
