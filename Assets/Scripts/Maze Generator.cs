using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    public int width = 21;     // must be odd
    public int height = 21;    // must be odd
    public int gladeSize = 5;  // central spawn area (must be odd)

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab; // optimized floor tiles
    public GameObject exitPrefab;
    public GameObject beaconPrefab;
    public GameObject playerPrefab; // NEW

    [Header("Generation Settings")]
    public bool generateOnStart = true;
    public int seed = 0; // 0 = random
    public float cellSize = 1f;   // scale of each maze cell
    public int beaconCount = 3;    // number of beacons

    private int[,] maze;
    private Vector2Int exitPosition;
    private List<GameObject> floorInstances = new List<GameObject>();

    void Start()
    {
        if (generateOnStart)
        {
            GenerateMaze(seed);
        }
    }

    public void GenerateMaze(int customSeed = 0)
    {
        if (customSeed != 0) Random.InitState(customSeed);
        else Random.InitState(System.DateTime.Now.Millisecond);

        maze = new int[width, height];

        // fill with walls
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                maze[x, y] = 1;

        // carve maze recursively
        Carve(1, 1);

        // carve central glade
        CreateGlade();

        // place exit
        PlaceExit();

        // spawn walls and floor
        DrawMaze();

        // AI wall adjustments + beacon placement
        MazeDifficultyAI ai = GetComponent<MazeDifficultyAI>();
        if (ai != null)
        {
            ai.beaconPrefab = beaconPrefab;
            ai.beaconCount = beaconCount;
            ai.AdjustMaze(maze, width, height, cellSize);
        }

        // finally, spawn player in exact center of maze
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

            if (nx > 0 && nx < width - 1 && ny > 0 && ny < height - 1)
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
        int cx = width / 2;
        int cy = height / 2;
        int r = gladeSize / 2;

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (cx + x > 0 && cx + x < width && cy + y > 0 && cy + y < height)
                {
                    maze[cx + x, cy + y] = 0;
                }
            }
        }
    }

    void PlaceExit()
    {
        List<Vector2Int> edges = new List<Vector2Int>();

        for (int x = 1; x < width - 1; x++)
        {
            if (maze[x, 1] == 0) edges.Add(new Vector2Int(x, 0));
            if (maze[x, height - 2] == 0) edges.Add(new Vector2Int(x, height - 1));
        }
        for (int y = 1; y < height - 1; y++)
        {
            if (maze[1, y] == 0) edges.Add(new Vector2Int(0, y));
            if (maze[width - 2, y] == 0) edges.Add(new Vector2Int(width - 1, y));
        }

        exitPosition = edges[Random.Range(0, edges.Count)];
        maze[exitPosition.x, exitPosition.y] = 0;

        GameObject exit = Instantiate(exitPrefab, new Vector3(exitPosition.x * cellSize, 0, exitPosition.y * cellSize), Quaternion.identity, transform);
        exit.transform.localScale = Vector3.one * cellSize;
    }

    void DrawMaze()
    {
        // clear old children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        floorInstances.Clear();

        // spawn optimized floor tiles
        SpawnOptimizedFloor();

        // spawn walls
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (maze[x, y] == 1)
                {
                    Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);

                    // scale wall correctly (height stays consistent)
                    Vector3 scale = new Vector3(cellSize, wall.transform.localScale.y, cellSize);
                    wall.transform.localScale = scale;
                }
            }
        }
    }

    void SpawnOptimizedFloor()
    {
        if (floorPrefab == null) return;

        Vector3 prefabSize = floorPrefab.GetComponent<Renderer>().bounds.size;

        int tilesX = Mathf.CeilToInt((width * cellSize) / prefabSize.x);
        int tilesZ = Mathf.CeilToInt((height * cellSize) / prefabSize.z);

        Vector3 startPos = new Vector3(prefabSize.x / 2f, 0, prefabSize.z / 2f);

        for (int x = 0; x < tilesX; x++)
        {
            for (int z = 0; z < tilesZ; z++)
            {
                Vector3 pos = new Vector3(x * prefabSize.x, 0, z * prefabSize.z) + startPos;
                GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                floorInstances.Add(floor);
            }
        }
    }

void SpawnPlayer()
{
    if (playerPrefab == null) return;

    // true center of the maze
    float spawnX = ((width - 1) / 2f) * cellSize + (cellSize / 2f);
    float spawnZ = ((height - 1) / 2f) * cellSize + (cellSize / 2f);
    float spawnY = 2f; // spawn slightly above floor

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
