namespace GameEngine
{
    public enum UILayer
    {
        Bottom,//主页签界面(如底部的页签切换的界面)
        Middle,//Bottom与Top中间的常驻界面如(如HomeWindow)
        Top,//所有的弹窗
        Tip,//一些提示弹窗,这些弹窗上不会再点出弹窗(如飘字,提示文本)
        Mask,//如断线重连,加载过度等弹出的覆盖界面
    }
}