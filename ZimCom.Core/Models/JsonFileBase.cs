using System.Text.Json;
using ZimCom.Core.Modules.Static;

namespace ZimCom.Core.Models;

public class JsonFileBase<T> : IJsonModel<T>
{
    public bool Save()
    {
        try
        {
            File.WriteAllText(GetFilePath(), ToString());
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public static T? Load()
    {
        T? temp = default;
        try
        {
            var content = File.ReadAllText(GetFilePath());
            if (!string.IsNullOrEmpty(content)) temp = JsonSerializer.Deserialize<T>(content);
        }
        catch (Exception ex)
        {
            StaticLogModule.LogError($"Error during {typeof(T).FullName} load", ex);
            return default;
        }

        StaticLogModule.LogDebug($"Loaded {typeof(T).FullName} from disk");
        return temp ?? default;
    }

    public static string GetFilePath()
    {
        return Path.Combine(StaticLocalPathModule.GetLocalApplicationFolder(), $"{typeof(T).Name}.json");
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize<JsonFileBase<T>>(this, new JsonSerializerOptions { WriteIndented = true });
    }
}