
using UnityEngine;
public class Density : MonoBehaviour
{
    private float[,,] densityMap;

    public void SetDensityMap(float[,,] map)
    {
        densityMap = map;
    }

    public virtual float GetValue(int x, int y, int z)
    {
        if (densityMap == null)
            return 0f;
        if (x < 0 || y < 0 || z < 0 || x >= densityMap.GetLength(0) || y >= densityMap.GetLength(1) || z >= densityMap.GetLength(2))
            return 0f;
        return densityMap[x, y, z];
    }

    public virtual Vector3Int GetBounds()
    {
        if (densityMap == null)
            return Vector3Int.zero;
        return new Vector3Int(densityMap.GetLength(0), densityMap.GetLength(1), densityMap.GetLength(2));
    }
}
