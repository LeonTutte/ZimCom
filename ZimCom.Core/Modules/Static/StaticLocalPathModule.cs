namespace ZimCom.Core.Modules.Static;

internal static class StaticLocalPathModule
{
    public static string GetLocalApplicationFolder()
    {
        var localApplicationFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZimCom");
        if (!Path.Exists(localApplicationFolder)) Directory.CreateDirectory(localApplicationFolder);
        return localApplicationFolder;
    }
}