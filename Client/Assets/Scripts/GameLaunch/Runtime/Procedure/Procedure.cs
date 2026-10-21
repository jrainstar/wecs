namespace GameLaunch
{
    public abstract class Procedure
    {
        public abstract void OnEnter(object data);
        protected void ChangeProcedure<T>(object data = null) where T : Procedure, new()
        {
            UnityEngine.Debug.Log(typeof(T).Name);
            new T().OnEnter(data);
        }
    }
}