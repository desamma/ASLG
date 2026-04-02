using System.IO;
using UnityEngine;

public static class JsonHelper
{
    // Loads a JSON file from the specified path and deserializes it into an object of type T.
    public static T Load<T>(string path) where T : class, new()
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[JsonHelper] File not found: {path}. Returning default.");
            return new T();
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }
    
    // Saves an object of type T to a JSON file at the specified path.
    public static void Save<T>(string path, T data) where T : class
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(path, json);
    }
}
