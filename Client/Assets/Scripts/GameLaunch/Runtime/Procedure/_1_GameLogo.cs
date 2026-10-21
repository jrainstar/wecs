namespace GameLaunch
{
    public class _1_GameLogo : Procedure
    {
        public override void OnEnter(object data)
        {
            ChangeProcedure<_2_InitPackage>();
        }
    }
}