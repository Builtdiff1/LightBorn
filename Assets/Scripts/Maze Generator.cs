using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    [Range(20, 200)]
    public int width = 20; // manual width

    [Range(20, 200)]
    public int height = 20;
    public int gladeSize = 5; // central spawn area (must be odd)

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab; // per-cell floor
    public GameObject exitPrefab;
    public GameObject beaconPrefab;
    public GameObject playerPrefab;

    [Header("Generation Settings")]
    public bool generateOnStart = true;
    public string seed = "0"; // format: width-seed-height
    public float cellSize = 1f;
    public int beaconCount = 3;

    private int[,] maze;
    private Vector2Int exitPosition;
    private List<GameObject> floorInstances = new List<GameObject>();
    private float wallHeight = 30f;

    private int usedWidth;
    private int usedHeight;
    private int usedSeed;

    void Start()
    {
        if (generateOnStart)
            GenerateMaze(seed);
    }

    public void GenerateMaze(string customSeed = null)
    {
        string seedStr = customSeed ?? seed;

        if (seedStr == "0")
        {
            // New random maze using current inspector width/height
            usedWidth = width;
            usedHeight = height;
            usedSeed = Random.Range(int.MinValue, int.MaxValue);
            seed = $"{usedWidth}-{usedSeed}-{usedHeight}";
        }
        else
        {
            // Parse format: width-seed-height
            int firstDash = seedStr.IndexOf('-');
            int lastDash = seedStr.LastIndexOf('-');

            if (firstDash == -1 || lastDash == -1 || firstDash == lastDash)
            {
                Debug.LogError("Invalid seed format, using defaults.");
                usedWidth = width;
                usedHeight = height;
                usedSeed = int.Parse(seedStr);
            }
            else
            {
                usedWidth = int.Parse(seedStr.Substring(0, firstDash));
                usedSeed = int.Parse(seedStr.Substring(firstDash + 1, lastDash - firstDash - 1));
                usedHeight = int.Parse(seedStr.Substring(lastDash + 1));
            }
        }

        Random.InitState(usedSeed);

        // Initialize maze
        maze = new int[usedWidth, usedHeight];
        for (int x = 0; x < usedWidth; x++)
            for (int y = 0; y < usedHeight; y++)
                maze[x, y] = 1;

        Carve(1, 1);
        CreateGlade();
        PickExitCell();
        DrawMaze();
        SpawnExitPrefab();

        // Apply AI deterministically
        MazeDifficultyAI ai = GetComponent<MazeDifficultyAI>();
        if (ai != null)
        {
            ai.beaconPrefab = beaconPrefab;
            ai.beaconCount = beaconCount;
            System.Random rng = new System.Random(usedSeed);
            ai.AdjustMaze(maze, usedWidth, usedHeight, cellSize, rng);
        }

        SpawnPlayer();
    }

    void Carve(int x, int y)
    {
        int[] dirs = { 0, 1, 2, 3 };
        Shuffle(dirs);

        foreach (int dir in dirs)
        {
            int dx = 0, dy = 0;
            switch (dir)
            {
                case 0: dx = 1; break;
                case 1: dx = -1; break;
                case 2: dy = 1; break;
                case 3: dy = -1; break;
            }

            int nx = x + dx * 2;
            int ny = y + dy * 2;

            if (nx > 0 && nx < usedWidth - 1 && ny > 0 && ny < usedHeight - 1)
            {
                if (maze[nx, ny] == 1)
                {
                    maze[nx - dx, ny - dy] = 0;
                    maze[nx, ny] = 0;
                    Carve(nx, ny);
                }
            }
        }
    }

    void CreateGlade()
    {
        int cx = usedWidth / 2;
        int cy = usedHeight / 2;
        int r = gladeSize / 2;

        for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
                if (cx + x > 0 && cx + x < usedWidth && cy + y > 0 && cy + y < usedHeight)
                    maze[cx + x, cy + y] = 0;
    }

    void PickExitCell()
    {
        List<Vector2Int> edges = new List<Vector2Int>();

        for (int x = 1; x < usedWidth - 1; x++)
        {
            if (maze[x, 1] == 0) edges.Add(new Vector2Int(x, 0));
            if (maze[x, usedHeight - 2] == 0) edges.Add(new Vector2Int(x, usedHeight - 1));
        }
        for (int y = 1; y < usedHeight - 1; y++)
        {
            if (maze[1, y] == 0) edges.Add(new Vector2Int(0, y));
            if (maze[usedWidth - 2, y] == 0) edges.Add(new Vector2Int(usedWidth - 1, y));
        }

        if (edges.Count == 0) return;

        exitPosition = edges[Random.Range(0, edges.Count)];
        maze[exitPosition.x, exitPosition.y] = 0;
    }

    void DrawMaze()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        SpawnFloorsPerCell();

        for (int x = 0; x < usedWidth; x++)
            for (int y = 0; y < usedHeight; y++)
            {
                if (x == exitPosition.x && y == exitPosition.y) continue;

                if (maze[x, y] == 1)
                {
                    Vector3 pos = new Vector3(x * cellSize, wallHeight / 2f, y * cellSize);
                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                    wall.transform.localScale = new Vector3(cellSize, wall.transform.localScale.y, cellSize);
                }
            }
    }

    void SpawnExitPrefab()
    {
        if (exitPrefab == null) return;

        Vector3 pos = new Vector3(exitPosition.x * cellSize, wallHeight / 2f, exitPosition.y * cellSize);
        GameObject exit = Instantiate(exitPrefab, pos, Quaternion.identity, transform);
        exit.transform.localScale = new Vector3(cellSize, wallPrefab.transform.localScale.y, cellSize);
    }

    void SpawnFloorsPerCell()
    {
        if (floorPrefab == null) return;

        floorInstances.Clear();

        for (int x = 0; x < usedWidth; x++)
            for (int y = 0; y < usedHeight; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0f, y * cellSize);
                GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);

                Vector3 prefabSize = floorPrefab.GetComponent<Renderer>().bounds.size;
                floor.transform.localScale = new Vector3(cellSize / prefabSize.x, 1f, cellSize / prefabSize.z);

                floorInstances.Add(floor);
            }
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        float spawnX = ((usedWidth - 1) / 2f) * cellSize + (cellSize / 2f);
        float spawnZ = ((usedHeight - 1) / 2f) * cellSize + (cellSize / 2f);
        float spawnY = wallHeight;

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);
        Instantiate(playerPrefab, spawnPos, Quaternion.identity);
    }

    void Shuffle(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
    }
}
