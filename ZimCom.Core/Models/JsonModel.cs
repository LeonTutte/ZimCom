namespace ZimCom.Core.Models;

public interface IJsonModel<T>
{
    public bool Save();
    public static abstract T? Load();
    public static abstract string GetFilePath();
}