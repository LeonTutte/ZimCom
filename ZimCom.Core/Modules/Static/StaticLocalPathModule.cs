namespace ZimCom.Core.Modules.Static;
internal class StaticLocalPathModule {
    public static string GetLocalApplicationFolder() {
        string localApplicationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZimCom");
        if (!Path.Exists(localApplicationFolder)) { Directory.CreateDirectory(localApplicationFolder); }
        return localApplicationFolder;
    }
}
