namespace GameFrame
{
    public class SaveModule : GameModule<SaveModule>
    {
        ES3Settings settings = new ES3Settings
        (
            ES3.EncryptionType.None, 
            ES3.Location.File,
            ES3.Format.JSON
        );

        protected override void DynamicAwake()
        {
            base.DynamicAwake();
            transform.parent = GameModules.Instance.transform;
        }

        public void Save<T>(string key, T value)
        {
            ES3.Save(key, value, settings);
        }

        public T Load<T>(string key, T defaultValue)
        {
            return ES3.Load(key, defaultValue, settings);
        }

    }
}