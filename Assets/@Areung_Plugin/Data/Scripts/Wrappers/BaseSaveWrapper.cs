namespace Areung_Plugin.Data.Scripts.Wrappers
{
    public abstract class BaseSaveWrapper
    {
        public static BaseSaveWrapper ActiveWrapper = new DefaultSaveWrapper();
        public abstract GlobalSave Load(string fileName);
        public abstract void Save(GlobalSave globalSave, string fileName);
        public abstract void Delete(string fileName);

        public virtual bool UseThreads() { return false; }
    }
}