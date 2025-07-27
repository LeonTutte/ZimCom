namespace ZimCom.Core.Models;
public interface IJsonModel<T> {
    public bool Save();
    public abstract static T? Load();
    public abstract static string GetFilePath();
}
