using UnityEngine;

public class CaveGeneratorTest : MonoBehaviour
{
    public int width = 30;
    public int height = 15;
    public int depth = 30;

    public int randomFillPercent = 45;
    public int smoothIterations = 3;
    public int randomWalkSteps = 1000;

    public GameObject wallPrefab;

    int[,,] map;

    void Start()
    {
        GenerateMap();
        InvertMap();
        CreateEntrance(width / 2, height - 3, depth / 2, 5);
        DrawMap();
    }

    void GenerateMap()
    {
        map = new int[width, height, depth];

        // Изначально заполнение случайное
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < depth; z++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1 || z == 0 || z == depth - 1)
                        map[x, y, z] = 1; // Стены по границам
                    else
                        map[x, y, z] = Random.Range(0, 100) < randomFillPercent ? 1 : 0;
                }

        RandomWalkTunnel();

        for (int i = 0; i < smoothIterations; i++)
        {
            SmoothMap();
        }
    }

    void CreateEntrance(int centerX, int centerY, int centerZ, int radius)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth)
                    {
                        float dist = Vector3.Distance(new Vector3(x, y, z), new Vector3(centerX, centerY, centerZ));
                        if (dist <= radius)
                        {
                            map[x, y, z] = 0; // Вырезаем пустоту — вход
                        }
                    }
                }
            }
        }
    }
  

    void RandomWalkTunnel()
    {
        int x = width / 2;
        int y = height / 2;
        int z = depth / 2;

        for (int i = 0; i < randomWalkSteps; i++)
        {
            map[x, y, z] = 0; // Выкапываем пустоту

            // Случайное направление в 3D: +/- x, y или z
            int dir = Random.Range(0, 6);
            switch (dir)
            {
                case 0: if (x + 1 < width - 1) x++; break;
                case 1: if (x - 1 > 0) x--; break;
                case 2: if (y + 1 < height - 1) y++; break;
                case 3: if (y - 1 > 0) y--; break;
                case 4: if (z + 1 < depth - 1) z++; break;
                case 5: if (z - 1 > 0) z--; break;
            }
        }
    }

    void SmoothMap()
    {
        int[,,] newMap = new int[width, height, depth];

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    int neighbourWallCount = GetSurroundingWallCount(x, y, z);
                    if (neighbourWallCount > 13)
                        newMap[x, y, z] = 1;
                    else if (neighbourWallCount < 13)
                        newMap[x, y, z] = 0;
                    else
                        newMap[x, y, z] = map[x, y, z];
                }
            }
        }
        map = newMap;
    }

    int GetSurroundingWallCount(int x, int y, int z)
    {
        int count = 0;
        for (int nx = x - 1; nx <= x + 1; nx++)
            for (int ny = y - 1; ny <= y + 1; ny++)
                for (int nz = z - 1; nz <= z + 1; nz++)
                {
                    if (nx == x && ny == y && nz == z) continue;
                    if (nx < 0 || ny < 0 || nz < 0 || nx >= width || ny >= height || nz >= depth)
                        count++;
                    else if (map[nx, ny, nz] == 1)
                        count++;
                }
        return count;
    }

    void InvertMap()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < depth; z++)
                {
                    map[x, y, z] = 1 - map[x, y, z]; // инверсия: 0 -> 1, 1 -> 0
                }
    }

    void DrawMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (map[x, y, z] == 1)
                    {
                        Vector3 pos = new Vector3(x, y, z);
                        Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                    }
                }
            }
        }
    }
}
