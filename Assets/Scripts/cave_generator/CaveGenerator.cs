using UnityEngine;

public class CaveGenerator : MonoBehaviour
{
    public int sizeX = 50, sizeY = 50, sizeZ = 50;
    private bool[,,] map;

    public MarchingCubes marchingCubes;

    void Start()
    {
        map = new bool[sizeX, sizeY, sizeZ];
        InitializeMap(0.45f);
        for (int i = 0; i < 5; i++)
            CellularAutomataStep();

        float[,,] densityMap = ConvertBoolToFloat(map);

        // Передаём плотностную карту в ваш генератор
        marchingCubes.DensityGenerator.SetDensityMap(densityMap);

        // Запускаем генерацию меша
        marchingCubes.SetupAndGenerateObject();
    }

    void InitializeMap(float fillProbability)
    {
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                    map[x, y, z] = Random.value < fillProbability;
    }

    int GetNeighborWallCount(int x, int y, int z)
    {
        int wallCount = 0;
        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                for (int k = z - 1; k <= z + 1; k++)
                {
                    if (i == x && j == y && k == z) continue; // пропускаем центр
                    if (i < 0 || j < 0 || k < 0 || i >= sizeX || j >= sizeY || k >= sizeZ)
                    {
                        wallCount++; // считаем выход за границы как стену
                    }
                    else
                    {
                        if (map[i, j, k]) wallCount++;
                    }
                }
            }
        }
        return wallCount;
    }

    void CellularAutomataStep()
    {
        bool[,,] newMap = new bool[sizeX, sizeY, sizeZ];

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int neighbors = GetNeighborWallCount(x, y, z);

                    if (map[x, y, z])
                    {
                        newMap[x, y, z] = neighbors >= 4;
                    }
                    else
                    {
                        newMap[x, y, z] = neighbors >= 5;
                    }
                }
            }
        }

        map = newMap;
    }

    float[,,] ConvertBoolToFloat(bool[,,] boolMap)
    {
        float[,,] floatMap = new float[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                    floatMap[x, y, z] = boolMap[x, y, z] ? 1f : 0f;
        return floatMap;
    }
}


