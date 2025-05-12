using System;
using System.IO;
using UnityEngine;


/// <summary>
/// Manages saving and loading of game data using configurable backends.
/// Singleton pattern for global access. Works on WebGL and desktop platforms.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    /// <summary>
    /// Gets the singleton instance of the SaveLoadManager.
    /// </summary>
    public static SaveLoadManager Instance { get; private set; }

    private ISaveLoadBackend currentBackend;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        // Default to PlayerPrefs on WebGL, JSON on desktop
#if UNITY_WEBGL
        SetBackend(new PlayerPrefsBackend());
#else
        SetBackend(new JsonFileBackend(Application.persistentDataPath));
#endif
    }

    /// <summary>
    /// Sets the storage backend to use for save/load operations.
    /// </summary>
    /// <param name="backend">The backend to use (e.g., JSON, PlayerPrefs, Custom).</param>
    public void SetBackend(ISaveLoadBackend backend)
    {
        currentBackend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    /// <summary>
    /// Saves data with the specified key using the current backend.
    /// </summary>
    /// <typeparam name="T">The type of data to save, must implement ISaveData.</typeparam>
    /// <param name="key">The unique key for the data.</param>
    /// <param name="data">The data to save.</param>
    public void Save<T>(string key, T data) where T : ISaveData
    {
        currentBackend.Save(key, data);
    }

    /// <summary>
    /// Loads data with the specified key using the current backend.
    /// </summary>
    /// <typeparam name="T">The type of data to load, must implement ISaveData.</typeparam>
    /// <param name="key">The unique key for the data.</param>
    /// <param name="defaultValue">The default value to return if loading fails.</param>
    /// <returns>The loaded data, or the default value if loading fails.</returns>
    public T Load<T>(string key, T defaultValue = default) where T : ISaveData
    {
        return currentBackend.Load(key, defaultValue);
    }

    /// <summary>
    /// Deletes data associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the data to delete.</param>
    public void Delete(string key)
    {
        currentBackend.Delete(key);
    }

    /// <summary>
    /// Checks if data exists for the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if data exists, false otherwise.</returns>
    public bool HasKey(string key)
    {
        return currentBackend.HasKey(key);
    }
}
/// <summary>
/// Interface for save/load backends to store and retrieve data.
/// </summary>
public interface ISaveLoadBackend
{
    /// <summary>
    /// Saves data with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of data to save, must implement ISaveData.</typeparam>
    /// <param name="key">The unique key for the data.</param>
    /// <param name="data">The data to save.</param>
    void Save<T>(string key, T data) where T : ISaveData;

    /// <summary>
    /// Loads data associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of data to load, must implement ISaveData.</typeparam>
    /// <param name="key">The unique key for the data.</param>
    /// <param name="defaultValue">The default value to return if loading fails.</param>
    /// <returns>The loaded data, or the default value if loading fails.</returns>
    T Load<T>(string key, T defaultValue) where T : ISaveData;

    /// <summary>
    /// Deletes data associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the data to delete.</param>
    void Delete(string key);

    /// <summary>
    /// Checks if data exists for the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if data exists, false otherwise.</returns>
    bool HasKey(string key);
}

/// <summary>
/// Backend for saving/loading data to JSON files on disk.
/// </summary>
public class JsonFileBackend : ISaveLoadBackend
{
    private readonly string basePath;

    public JsonFileBackend(string basePath)
    {
        this.basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        Directory.CreateDirectory(basePath); // Ensure directory exists
    }

    public void Save<T>(string key, T data) where T : ISaveData
    {
        string filePath = GetFilePath(key);
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(filePath, json);
    }

    public T Load<T>(string key, T defaultValue) where T : ISaveData
    {
        string filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return defaultValue;
        }

        string json = File.ReadAllText(filePath);
        Debug.Log(json);
        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load JSON data for key '{key}': {e.Message}");
            return defaultValue;
        }
    }

    public void Delete(string key)
    {
        string filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public bool HasKey(string key)
    {
        return File.Exists(GetFilePath(key));
    }

    private string GetFilePath(string key)
    {
        return Path.Combine(basePath, $"{key}.json");
    }
}


/// <summary>
/// Backend for saving/loading data using Unity's PlayerPrefs.
/// </summary>
public class PlayerPrefsBackend : ISaveLoadBackend
{
    public void Save<T>(string key, T data) where T : ISaveData
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public T Load<T>(string key, T defaultValue) where T : ISaveData
    {
        if (!PlayerPrefs.HasKey(key))
        {
            return defaultValue;
        }

        string json = PlayerPrefs.GetString(key);
        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load PlayerPrefs data for key '{key}': {e.Message}");
            return defaultValue;
        }
    }

    public void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    public bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }
}
/// <summary>
/// Interface for data objects that can be saved and loaded.
/// Implement this to ensure compatibility with SaveLoadManager.
/// </summary>
public interface ISaveData
{
    // No methods required; serves as a marker interface.
    // Data should be serializable by JsonUtility or handled by custom backend.
}

/// <summary>
/// Example data class for saving/loading player data.
/// </summary>
[System.Serializable]
public class PlayerData : ISaveData
{
    public string Name;
    public int Health;
    public int Score;
}