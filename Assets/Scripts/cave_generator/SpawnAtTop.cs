using UnityEngine;

public class SpawnAtTop : MonoBehaviour
{
    public Transform player;
    public float offsetY = 2f;

    void Start()
    {
        var mc = GetComponent<MeshCollider>();
        if (mc == null || player == null) return;

        // берём верхнюю грань меша (bounds.max)
        var b = mc.sharedMesh.bounds;
        Vector3 topWorld = transform.TransformPoint(new Vector3(b.center.x, b.max.y, b.center.z));
        player.position = topWorld + Vector3.up * offsetY;
    }
}
