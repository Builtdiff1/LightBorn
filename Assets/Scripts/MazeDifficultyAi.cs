using System.Collections.Generic;
using UnityEngine;

public class MazeDifficultyAI : MonoBehaviour
{
    [Header("Beacon Settings")]
    public GameObject beaconPrefab;
    public int beaconCount = 3;
    public float minDistanceFromExit = 0.3f; 
    public float minDistanceBetweenBeacons = 0.2f; 
    public int beaconExtensionLengthMin = 3;
    public int beaconExtensionLengthMax = 7;

    [Header("Difficulty Settings")]
    [Range(0f, 1f)]
    public float wallRemovalChance = 0.05f;

    [Header("Dungeon Settings")]
    public List<GameObject> dungeonPrefabs;
    public int dungeonCount = 2;

    [Header("Den Settings")]
    public List<GameObject> denPrefabs;
    public int denCount = 2;

    [Header("Structure Settings")]
    public List<GameObject> structurePrefabs;
    public int structureCount = 2;
    public Vector2Int structureSize = new Vector2Int(3, 3); 

    private System.Random rng;

    public void AdjustMaze(int[,] maze, int width, int height, float cellSize, System.Random rngInput)
    {
        rng = rngInput;
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

                    if (openCount <= 1 && rng.NextDouble() < wallRemovalChance)
                        maze[x, y] = 0;
                }
            }
        }
    }

    void PlaceBeacons(int[,] maze, int width, int height, float cellSize)
    {
        if (beaconPrefab == null || beaconCount <= 0) return;

        Vector2Int playerStart = new Vector2Int(width / 2, height / 2);
        Vector2Int exitPos = FindExit(maze, width, height);
        List<Vector2Int> deadEnds = FindDeadEnds(maze, width, height);
        List<Vector2Int> placedBeacons = new List<Vector2Int>();

        // Sort dead ends by “path length to nearest junction” descending
        deadEnds.Sort((a, b) => ScoreDeadEnd(maze, a).CompareTo(ScoreDeadEnd(maze, b)));
        deadEnds.Reverse();

        for (int i = 0; i < beaconCount && deadEnds.Count > 0; i++)
        {
            Vector2Int chosen = Vector2Int.zero;
            int tries = 0;

            while (tries < deadEnds.Count)
            {
                chosen = deadEnds[tries];
                tries++;

                // Far from exit & player
                if (Vector2Int.Distance(chosen, exitPos) < Mathf.Max(width, height) * minDistanceFromExit)
                    continue;
                if (Vector2Int.Distance(chosen, playerStart) < Mathf.Max(width, height) * minDistanceFromExit)
                    continue;

                // Far from other beacons
                bool tooClose = false;
                foreach (var b in placedBeacons)
                {
                    if (Vector2Int.Distance(chosen, b) < Mathf.Max(width, height) * minDistanceBetweenBeacons)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                break;
            }

            if (chosen == Vector2Int.zero) continue;

            // Optional: create mini-corridor to hide beacon
            Vector2Int beaconPos = ExtendDeadEnd(maze, chosen);

            placedBeacons.Add(beaconPos);
            Vector3 pos = new Vector3(beaconPos.x * cellSize, 0, beaconPos.y * cellSize);
            Instantiate(beaconPrefab, pos, Quaternion.identity, transform);
        }
    }

    int ScoreDeadEnd(int[,] maze, Vector2Int pos)
    {
        int distance = 0;
        Vector2Int current = pos;

        while (true)
        {
            int paths = 0;
            if (maze[current.x + 1, current.y] == 0) paths++;
            if (maze[current.x - 1, current.y] == 0) paths++;
            if (maze[current.x, current.y + 1] == 0) paths++;
            if (maze[current.x, current.y - 1] == 0) paths++;

            if (paths != 1) break; // reached junction
            distance++;

            // Move to next open neighbor
            if (maze[current.x + 1, current.y] == 0) current.x++;
            else if (maze[current.x - 1, current.y] == 0) current.x--;
            else if (maze[current.x, current.y + 1] == 0) current.y++;
            else if (maze[current.x, current.y - 1] == 0) current.y--;
        }

        return distance;
    }

    Vector2Int ExtendDeadEnd(int[,] maze, Vector2Int start)
    {
        Vector2Int current = start;
        int length = rng.Next(beaconExtensionLengthMin, beaconExtensionLengthMax + 1);

        for (int i = 0; i < length; i++)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            if (maze[current.x + 1, current.y] == 1) neighbors.Add(new Vector2Int(current.x + 1, current.y));
            if (maze[current.x - 1, current.y] == 1) neighbors.Add(new Vector2Int(current.x - 1, current.y));
            if (maze[current.x, current.y + 1] == 1) neighbors.Add(new Vector2Int(current.x, current.y + 1));
            if (maze[current.x, current.y - 1] == 1) neighbors.Add(new Vector2Int(current.x, current.y - 1));

            if (neighbors.Count == 0) break;

            Vector2Int next = neighbors[rng.Next(neighbors.Count)];
            maze[next.x, next.y] = 0; // carve path
            current = next;
        }

        return current;
    }

    // --- Dungeons, Dens, Structures remain the same ---
    void PlaceDungeons(int[,] maze, int width, int height, float cellSize)
    {
        if (dungeonPrefabs.Count == 0 || dungeonCount <= 0) return;

        for (int i = 0; i < dungeonCount; i++)
        {
            Vector2Int pos = FindWallWithPath(maze, width, height);
            if (pos == Vector2Int.zero) continue;

            GameObject dungeonPrefab = dungeonPrefabs[rng.Next(dungeonPrefabs.Count)];
            Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            Instantiate(dungeonPrefab, spawnPos, Quaternion.identity, transform);

            maze[pos.x, pos.y] = 0;
        }
    }

    void PlaceDens(int[,] maze, int width, int height, float cellSize)
    {
        if (denPrefabs.Count == 0 || denCount <= 0) return;

        for (int i = 0; i < denCount; i++)
        {
            Vector2Int pos = FindWallWithPath(maze, width, height);
            if (pos == Vector2Int.zero) continue;

            GameObject denPrefab = denPrefabs[rng.Next(denPrefabs.Count)];
            Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            Instantiate(denPrefab, spawnPos, Quaternion.identity, transform);

            maze[pos.x, pos.y] = 0;
        }
    }

    void PlaceStructures(int[,] maze, int width, int height, float cellSize)
    {
        if (structurePrefabs.Count == 0 || structureCount <= 0) return;

        for (int i = 0; i < structureCount; i++)
        {
            Vector2Int pos = FindOpenArea(maze, width, height, structureSize);
            if (pos == Vector2Int.zero) continue;

            GameObject structurePrefab = structurePrefabs[rng.Next(structurePrefabs.Count)];
            Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            Instantiate(structurePrefab, spawnPos, Quaternion.identity, transform);

            for (int x = 0; x < structureSize.x; x++)
                for (int y = 0; y < structureSize.y; y++)
                    if (pos.x + x < width && pos.y + y < height)
                        maze[pos.x + x, pos.y + y] = 0;
        }
    }

    List<Vector2Int> FindDeadEnds(int[,] maze, int width, int height)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
                if (maze[x, y] == 0)
                {
                    int walls = 0;
                    if (maze[x + 1, y] == 1) walls++;
                    if (maze[x - 1, y] == 1) walls++;
                    if (maze[x, y + 1] == 1) walls++;
                    if (maze[x, y - 1] == 1) walls++;

                    if (walls >= 3) deadEnds.Add(new Vector2Int(x, y));
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
            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);

            if (maze[x, y] == 1 && (maze[x + 1, y] == 0 || maze[x - 1, y] == 0 || maze[x, y + 1] == 0 || maze[x, y - 1] == 0))
                return new Vector2Int(x, y);
        }
        return Vector2Int.zero;
    }

    Vector2Int FindOpenArea(int[,] maze, int width, int height, Vector2Int size)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            int x = rng.Next(1, width - size.x - 1);
            int y = rng.Next(1, height - size.y - 1);

            bool fits = true;
            for (int dx = 0; dx < size.x; dx++)
                for (int dy = 0; dy < size.y; dy++)
                    if (maze[x + dx, y + dy] != 0) fits = false;

            if (fits) return new Vector2Int(x, y);
        }
        return Vector2Int.zero;
    }
}
