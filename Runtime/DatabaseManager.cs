using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A static class for managing data loaded from CSV files into in-memory dictionaries.
/// Supports generic data types with reflection-based instantiation.
/// </summary>
public static class DatabaseManager
{
    private static Dictionary<Type, object> _databases = new Dictionary<Type, object>();

    /// <summary>
    /// Loads data from a CSV file into a dictionary for the specified type.
    /// Must be called before retrieving data with <see cref="GetData{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of data to load, which must implement <see cref="IData"/>.</typeparam>
    /// <param name="resourcePath">The path to the CSV file in the Resources folder, without extension.</param>
    /// <exception cref="ArgumentException">Thrown if the type T does not have a constructor accepting a string array.</exception>
    public static void LoadDatabase<T>(string resourcePath) where T : IData
    {
        Dictionary<int, T> database = new Dictionary<int, T>();
        TextAsset csv = Resources.Load<TextAsset>(resourcePath);
        if (csv == null)
        {
            Debug.LogError($"CSV file at '{resourcePath}' not found!");
            return;
        }

        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] tokens = line.Split(',');
            try
            {
                T data = (T)Activator.CreateInstance(typeof(T), (object)tokens);
                database.Add(data.ID, data);
            }
            catch (MissingMethodException)
            {
                throw new ArgumentException($"Type {typeof(T).Name} must have a constructor accepting a string array.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse line {i} in '{resourcePath}': {e.Message}");
            }
        }

        _databases[typeof(T)] = database;
        Debug.Log($"Database for {typeof(T).Name} loaded from '{resourcePath}' with {database.Count} entries.");
    }

    /// <summary>
    /// Retrieves data of type T by ID from the preloaded database.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve, which must implement <see cref="IData"/>.</typeparam>
    /// <param name="id">The unique identifier of the data.</param>
    /// <returns>The data associated with the specified ID, or null if not found or if the database is not loaded.</returns>
    public static T GetData<T>(int id) where T : IData
    {
        if (!_databases.TryGetValue(typeof(T), out object dbObject) || dbObject == null)
        {
            Debug.LogError($"Database for {typeof(T).Name} not loaded. Call LoadDatabase<T> first.");
            return default;
        }

        var database = (Dictionary<int, T>)dbObject;
        if (database.TryGetValue(id, out T data))
        {
            return data;
        }

        Debug.LogWarning($"Data of type {typeof(T).Name} with ID {id} not found in database.");
        return default;
    }

    /// <summary>
    /// Searches the preloaded database of type T using one or more filters.
    /// </summary>
    /// <typeparam name="T">The type of data to search, which must implement <see cref="IData"/>.</typeparam>
    /// <param name="any">If true, returns items matching any filter; if false, requires all filters to match.</param>
    /// <param name="filters">An array of predicates to filter the data.</param>
    /// <returns>A list of IDs of data items that match the filters, or an empty list if the database is not loaded.</returns>
    public static List<int> SearchData<T>(bool any = false, params Predicate<T>[] filters) where T : IData
    {
        if (!_databases.TryGetValue(typeof(T), out object dbObject) || dbObject == null)
        {
            Debug.LogError($"Database for {typeof(T).Name} not loaded. Call LoadDatabase<T> first.");
            return new List<int>();
        }

        var database = (Dictionary<int, T>)dbObject;
        List<int> results = new List<int>();

        foreach (var kvp in database)
        {
            bool matches = any ? false : true;
            foreach (var filter in filters)
            {
                bool isMatch = filter(kvp.Value);
                if (any && isMatch)
                {
                    matches = true;
                    break;
                }
                else if (!any && !isMatch)
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
            {
                results.Add(kvp.Key);
            }
        }

        return results;
    }
}

/// <summary>
/// Defines a contract for data objects that can be managed by <see cref="DatabaseManager"/>.
/// </summary>
public interface IData
{
    /// <summary>
    /// Gets the unique identifier of the data object.
    /// </summary>
    int ID { get; }

    /// <summary>
    /// Gets the name of the data object.
    /// </summary>
    string Name { get; }
}