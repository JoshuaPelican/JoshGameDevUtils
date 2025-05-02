using UnityEngine;

/// <summary>
/// A generic 2D grid system for storing and manipulating data of type T.
/// Supports world-to-grid coordinate conversion and grid resizing.
/// </summary>
/// <typeparam name="T">The type of data stored in the grid.</typeparam>
public class Grid<T>
{
    /// <summary>
    /// Gets the width of the grid.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the grid.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets the size of each cell in world units.
    /// </summary>
    public float CellSize { get; private set; }

    /// <summary>
    /// Gets the origin position of the grid in world space.
    /// </summary>
    public Vector3 OriginPosition { get; private set; }

    /// <summary>
    /// Delegate for handling grid value change events.
    /// </summary>
    /// <param name="x">The x-coordinate of the changed cell.</param>
    /// <param name="y">The y-coordinate of the changed cell.</param>
    public delegate void GridEvent(int x, int y);

    /// <summary>
    /// Event invoked when a grid value changes.
    /// </summary>
    public event GridEvent OnGridValueChanged;

    private T[,] grid;

    /// <summary>
    /// Initializes a new instance of the Grid class.
    /// </summary>
    /// <param name="width">The width of the grid.</param>
    /// <param name="height">The height of the grid.</param>
    /// <param name="cellSize">The size of each cell in world units (default is 1).</param>
    /// <param name="originPosition">The origin position of the grid in world space (default is Vector3.zero).</param>
    public Grid(int width, int height, float cellSize = 1, Vector3 originPosition = default)
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        OriginPosition = originPosition;

        grid = new T[Width, Height];
    }

    /// <summary>
    /// Gets or sets the value at the specified grid coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The value at the specified coordinates.</returns>
    public T this[int x, int y]
    {
        get { return GetValue(x, y); }
        set { SetValue(x, y, value); }
    }

    /// <summary>
    /// Gets or sets the value at the grid coordinates corresponding to the specified world position.
    /// </summary>
    /// <param name="pos">The world position.</param>
    /// <returns>The value at the corresponding grid coordinates.</returns>
    public T this[Vector3 pos]
    {
        get { return GetValue(pos); }
        set { SetValue(pos, value); }
    }

    /// <summary>
    /// Returns a string representation of the grid.
    /// </summary>
    /// <returns>A string representing the grid's contents.</returns>
    public override string ToString()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int y = 0; y < Height; y++)
        {
            builder.Append("{");
            for (int x = 0; x < Width; x++)
                builder.Append($"{grid[x, y]}");
            builder.Append("}\n");
        }
        return builder.ToString();
    }

    /// <summary>
    /// Checks if the specified coordinates are within the grid bounds.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>True if the coordinates are valid, false otherwise.</returns>
    public bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// Checks if the specified world position corresponds to a valid grid cell.
    /// </summary>
    /// <param name="worldPosition">The world position to check.</param>
    /// <returns>True if the position corresponds to a valid cell, false otherwise.</returns>
    public bool ValidCell(Vector3 worldPosition)
    {
        int x, y;
        GetCoordinates(worldPosition, out x, out y);
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    /// <summary>
    /// Converts grid coordinates to a world position.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="centered">If true, returns the center of the cell; otherwise, returns the bottom-left corner.</param>
    /// <returns>The world position corresponding to the grid coordinates.</returns>
    public Vector3 GetWorldPosition(int x, int y, bool centered)
    {
        Vector3 worldPos = new Vector3(x - (Width / 2f), y - (Height / 2f)) * CellSize + OriginPosition;
        if (centered)
            worldPos += new Vector3(CellSize / 2f, CellSize / 2f);
        return worldPos;
    }

    /// <summary>
    /// Converts a world position to the nearest grid cell's world position.
    /// </summary>
    /// <param name="worldPosition">The world position to convert.</param>
    /// <param name="centered">If true, returns the center of the cell; otherwise, returns the bottom-left corner.</param>
    /// <returns>The world position of the nearest grid cell.</returns>
    public Vector3 GetWorldPosition(Vector3 worldPosition, bool centered)
    {
        GetCoordinates(worldPosition, out int x, out int y);
        return GetWorldPosition(x, y, centered);
    }

    /// <summary>
    /// Converts a world position to grid coordinates.
    /// </summary>
    /// <param name="worldPosition">The world position to convert.</param>
    /// <param name="x">The output x-coordinate.</param>
    /// <param name="y">The output y-coordinate.</param>
    public void GetCoordinates(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt(((worldPosition - OriginPosition).x / CellSize) + (Width / 2f));
        y = Mathf.FloorToInt(((worldPosition - OriginPosition).y / CellSize) + (Height / 2f));
    }

    /// <summary>
    /// Checks if the specified grid cell is empty (null).
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>True if the cell is empty, false otherwise.</returns>
    public bool IsEmpty(int x, int y)
    {
        return grid[x, y] == null;
    }

    /// <summary>
    /// Gets an alternation value (0 or 1) for the specified cell, useful for checkerboard patterns.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="invert">If true, inverts the alternation pattern.</param>
    /// <returns>0 or 1 based on the cell's position and inversion setting.</returns>
    public int GetCellAlternation(int x, int y, bool invert)
    {
        if ((x + y) % 2 == 0)
            return invert ? 0 : 1;
        return invert ? 1 : 0;
    }

    /// <summary>
    /// Updates the grid's origin position and cell size.
    /// </summary>
    /// <param name="origin">The new origin position.</param>
    /// <param name="cellSize">The new cell size.</param>
    public void SetOriginAndCellSize(Vector3 origin, float cellSize)
    {
        OriginPosition = origin;
        CellSize = cellSize;
    }

    /// <summary>
    /// Resizes the grid, preserving existing data where possible.
    /// </summary>
    /// <param name="newWidth">The new width of the grid.</param>
    /// <param name="newHeight">The new height of the grid.</param>
    public void Resize(int newWidth, int newHeight)
    {
        T[,] resizedGrid = new T[newWidth, newHeight];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                resizedGrid[x, y] = grid[x, y];
            }
        }

        Width = newWidth;
        Height = newHeight;

        grid = resizedGrid;
    }

    private void SetValue(int x, int y, T value)
    {
        if (!IsValidCell(x, y))
            return;

        grid[x, y] = value;
        OnGridValueChanged?.Invoke(x, y);
    }

    private void SetValue(Vector3 worldPosition, T value)
    {
        GetCoordinates(worldPosition, out int x, out int y);
        SetValue(x, y, value);
    }

    private T GetValue(int x, int y)
    {
        if (!IsValidCell(x, y))
            return default;

        return grid[x, y];
    }

    private T GetValue(Vector3 worldPosition)
    {
        GetCoordinates(worldPosition, out int x, out int y);
        return GetValue(x, y);
    }
}