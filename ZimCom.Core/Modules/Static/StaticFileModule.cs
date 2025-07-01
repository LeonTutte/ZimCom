namespace ZimCom.Core.Modules.Static;
internal class StaticFileModule {
    public static bool RequestedFileWasCreated = false;
    public static string GetFileAsFullPath(string subDirectory, string file) {
        string path = Path.Combine(GetApplicationDirectory(), subDirectory);
        string filePath = Path.Combine(path, file);
        GetOrCreateDirectoryFromPath(path);
        RequestedFileWasCreated = GetOrCreateFileFromPath(filePath);
        if (!Path.Exists(filePath)) {
            StaticLogModule.LogError("File or path is not accessible or not available: " + filePath, null);
            Environment.Exit(1);
        }
        return filePath;
    }
    private static bool GetOrCreateFileFromPath(string path) {
        if (!File.Exists(path)) {
            System.IO.File.Create(path);
            return true;
        }
        return false;
    }
    private static bool GetOrCreateDirectoryFromPath(string path) {
        if (!Path.Exists(path)) {
            System.IO.Directory.CreateDirectory(path);
            return true;
        }
        return false;
    }
    internal static string GetApplicationDirectory() {
        string baseFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        ArgumentNullException.ThrowIfNullOrEmpty(baseFolderPath);
        return Path.Combine(baseFolderPath, "ZimCom");
    }
}
