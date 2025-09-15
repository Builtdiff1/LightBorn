using System.Collections.Generic;
using UnityEngine;

public class MazeDifficultyAI : MonoBehaviour
{
    [Header("Beacon Settings")]
    public GameObject beaconPrefab;
    public int beaconCount = 3;
    public float minDistanceFromExit = 0.3f; 
    public float minDistanceBetweenBeacons = 0.2f; 

    [Header("Difficulty Settings")]
    [Range(0f, 1f)]
    public float wallRemovalChance = 0.1f;

    [Header("Dungeon Settings")]
    public List<GameObject> dungeonPrefabs;
    public int dungeonCount = 2;

    [Header("Den Settings")]
    public List<GameObject> denPrefabs;
    public int denCount = 2;

    [Header("Structure Settings")]
    public List<GameObject> structurePrefabs;
    public int structureCount = 2;
    public Vector2Int structureSize = new Vector2Int(3, 3); // reserved size in maze cells

    public void AdjustMaze(int[,] maze, int width, int height, float cellSize)
    {
        AdjustWalls(maze, width, height);
        PlaceBeacons(maze, width, height, cellSize);
        PlaceDungeons(maze, width, height, cellSize);
        PlaceDens(maze, width, height, cellSize);
        PlaceStructures(maze, width, height, cellSize);
    }

    void AdjustWalls(int[,] maze, int width, int height)
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (maze[x, y] == 1)
                {
                    int openCount = 0;
                    if (maze[x + 1, y] == 0) openCount++;
                    if (maze[x - 1, y] == 0) openCount++;
                    if (maze[x, y + 1] == 0) openCount++;
                    if (maze[x, y - 1] == 0) openCount++;

                    if (openCount <= 1 && Random.value < wallRemovalChance)
                    {
                        maze[x, y] = 0;
                    }
                }
            }
        }
    }

    void PlaceBeacons(int[,] maze, int width, int height, float cellSize)
    {
        if (beaconPrefab == null || beaconCount <= 0) return;

        List<Vector2Int> deadEnds = FindDeadEnds(maze, width, height);
        List<Vector2Int> placedBeacons = new List<Vector2Int>();
        Vector2Int exitPos = FindExit(maze, width, height);

        for (int i = 0; i < beaconCount; i++)
        {
            Vector2Int chosen = Vector2Int.zero;
            int tries = 0;

            while (tries < 100)
            {
                if (deadEnds.Count == 0) break;

                chosen = deadEnds[Random.Range(0, deadEnds.Count)];

                if (Vector2Int.Distance(chosen, exitPos) < Mathf.Max(width, height) * minDistanceFromExit)
                {
                    tries++;
                    continue;
                }

                bool tooClose = false;
                foreach (var b in placedBeacons)
                {
                    if (Vector2Int.Distance(chosen, b) < Mathf.Max(width, height) * minDistanceBetweenBeacons)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    tries++;
                    continue;
                }

                break;
            }

            placedBeacons.Add(chosen);
            Vector3 pos = new Vector3(chosen.x * cellSize, 0, chosen.y * cellSize);
            Instantiate(beaconPrefab, pos, Quaternion.identity, transform);
        }
    }

    void PlaceDungeons(int[,] maze, int width, int height, float cellSize)
    {
        if (dungeonPrefabs.Count == 0 || dungeonCount <= 0) return;

        for (int i = 0; i < dungeonCount; i++)
        {
            Vector2Int pos = FindWallWithPath(maze, width, height);
            if (pos == Vector2Int.zero) continue;

            GameObject dungeonPrefab = dungeonPrefabs[Random.Range(0, dungeonPrefabs.Count)];
            Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            Instantiate(dungeonPrefab, spawnPos, Quaternion.identity, transform);

            maze[pos.x, pos.y] = 0; // carve entrance
        }
    }

    void PlaceDens(int[,] maze, int width, int height, float cellSize)
    {
        if (denPrefabs.Count == 0 || denCount <= 0) return;

        for (int i = 0; i < denCount; i++)
        {
            Vector2Int pos = FindWallWithPath(maze, width, height);
            if (pos == Vector2Int.zero) continue;

            GameObject denPrefab = denPrefabs[Random.Range(0, denPrefabs.Count)];
            Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            Instantiate(denPrefab, spawnPos, Quaternion.identity, transform);

            maze[pos.x, pos.y] = 0; // carve entrance
        }
    }

    void PlaceStructures(int[,] maze, int width, int height, float cellSize)
    {
        if (structurePrefabs.Count == 0 || structureCount <= 0) return;

        for (int i = 0; i < structureCount; i++)
        {
            Vector2Int pos = FindOpenArea(maze, width, height, structureSize);
            if (pos == Vector2Int.zero) continue;

            GameObject structurePrefab = structurePrefabs[Random.Range(0, structurePrefabs.Count)];
            Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            Instantiate(structurePrefab, spawnPos, Quaternion.identity, transform);

            // clear reserved space
            for (int x = 0; x < structureSize.x; x++)
            {
                for (int y = 0; y < structureSize.y; y++)
                {
                    int gx = pos.x + x;
                    int gy = pos.y + y;
                    if (gx < width && gy < height)
                    {
                        maze[gx, gy] = 0;
                    }
                }
            }
        }
    }

    List<Vector2Int> FindDeadEnds(int[,] maze, int width, int height)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (maze[x, y] == 0)
                {
                    int walls = 0;
                    if (maze[x + 1, y] == 1) walls++;
                    if (maze[x - 1, y] == 1) walls++;
                    if (maze[x, y + 1] == 1) walls++;
                    if (maze[x, y - 1] == 1) walls++;

                    if (walls >= 3) deadEnds.Add(new Vector2Int(x, y));
                }
            }
        }
        return deadEnds;
    }

    Vector2Int FindExit(int[,] maze, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            if (maze[x, 0] == 0) return new Vector2Int(x, 0);
            if (maze[x, height - 1] == 0) return new Vector2Int(x, height - 1);
        }
        for (int y = 0; y < height; y++)
        {
            if (maze[0, y] == 0) return new Vector2Int(0, y);
            if (maze[width - 1, y] == 0) return new Vector2Int(width - 1, y);
        }
        return new Vector2Int(width / 2, height / 2);
    }

    Vector2Int FindWallWithPath(int[,] maze, int width, int height)
    {
        for (int tries = 0; tries < 100; tries++)
        {
            int x = Random.Range(1, width - 1);
            int y = Random.Range(1, height - 1);

            if (maze[x, y] == 1)
            {
                if (maze[x + 1, y] == 0 || maze[x - 1, y] == 0 || maze[x, y + 1] == 0 || maze[x, y - 1] == 0)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return Vector2Int.zero;
    }

    Vector2Int FindOpenArea(int[,] maze, int width, int height, Vector2Int size)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            int x = Random.Range(1, width - size.x - 1);
            int y = Random.Range(1, height - size.y - 1);

            bool fits = true;
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    if (maze[x + dx, y + dy] != 0)
                    {
                        fits = false;
                        break;
                    }
                }
                if (!fits) break;
            }

            if (fits) return new Vector2Int(x, y);
        }
        return Vector2Int.zero;
    }
}
