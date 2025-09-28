using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VerticalCaveWithTunnels : MonoBehaviour
{
    [Header("Main Shaft")]
    public float depth = 160f;
    public float metersPerRing = 1.0f;
    [Range(8,128)] public int segmentsAround = 64;
    public float baseRadiusTop = 6f, baseRadiusBottom = 10f;
    public int seed = 12345;
    [Range(0f,10f)] public float pathAmplitude = 6f;
    [Range(0.01f,1f)] public float pathFreq = 0.08f;
    [Range(0f,2f)] public float radiusNoiseAmp = 1.4f;
    [Range(0.01f,1f)] public float radiusNoiseFreq = 0.35f;
    [Range(0f,1f)] public float normalSmoothing = 0.35f;

    [Header("Ledges/Alcoves")]
    [Range(0f,1f)] public float ledgeChance = 0.18f;
    public float ledgeSize = 1.0f;
    [Range(0f,1f)] public float alcoveChance = 0.12f;
    public int alcoveWidthSegs = 3;

    [Header("Tunnels")]
    [Range(0,30)] public int tunnelsCount = 8;           // сколько веток
    public Vector2 tunnelLenRange = new Vector2(18, 45); // длина в метрах
    public Vector2 tunnelRadiusRange = new Vector2(2.0f, 3.3f);
    [Range(6,48)] public int tunnelSegmentsAround = 24;
    public float tunnelCurveAmp = 5f;     // насколько изгибается
    public float tunnelCurveFreq = 0.12f; // частота изгиба

    Mesh _shaftMesh;
    Mesh _tunnelMesh;
    MeshCollider _collider;
    System.Random _rng;

    struct Tunnel
    {
        public int ring;          // на каком кольце вход
        public int sector;        // сектор (угловой индекс)
        public int widthSegs;     // ширина проёма (в секторах)
        public float length;
        public float radius;
        public Vector3 mouthPos;  // позиция края проёма
        public Vector3 mouthNormal;
        public Vector3 mouthTangent;
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        _rng = new System.Random(seed);
        var mf = GetComponent<MeshFilter>();
        _shaftMesh = new Mesh { name = "CaveShaft" };
        mf.sharedMesh = _shaftMesh;
        _collider = GetComponent<MeshCollider>();

        // 1) Генерим основной спуск как «трубу с рваным краем»
        List<Tunnel> tunnels;
        GenerateShaft(out var shaftVerts, out var shaftUV, out var shaftNorms, out var shaftTris, out tunnels);

        _shaftMesh.indexFormat = (shaftVerts.Length > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        _shaftMesh.vertices = shaftVerts;
        _shaftMesh.uv = shaftUV;
        _shaftMesh.normals = shaftNorms;
        _shaftMesh.triangles = shaftTris.ToArray();
        _shaftMesh.RecalculateBounds();

        // 2) Генерим тоннели и объединяем в один меш
        _tunnelMesh = BuildTunnelsMesh(tunnels);
        CombineMeshes(_shaftMesh, _tunnelMesh, mf);

        // 3) Коллизия
        _collider.sharedMesh = null;
        _collider.sharedMesh = mf.sharedMesh;

        Debug.Log($"Cave done: tunnels={tunnels.Count}");
    }

    // ---------- SHaft ----------
    void GenerateShaft(out Vector3[] verts, out Vector2[] uvs, out Vector3[] norms, out List<int> tris, out List<Tunnel> tunnels)
    {
        int rings = Mathf.CeilToInt(depth / metersPerRing) + 1;
        int segs = Mathf.Max(8, segmentsAround);
        int vpr = segs + 1; // seam
        float totalH = (rings - 1) * metersPerRing;

        verts = new Vector3[rings * vpr];
        uvs   = new Vector2[verts.Length];
        norms = new Vector3[verts.Length];
        tris  = new List<int>(rings * segs * 6);

        // случайные фазы
        float nr = (float)_rng.NextDouble() * 1000f;
        float nx = (float)_rng.NextDouble() * 1000f;
        float nz = (float)_rng.NextDouble() * 1000f;

        // заранее раскидаем входы тоннелей
        tunnels = PlanTunnels(rings, segs);

        // отмечаем какие сектора «вырезаны» под проёмы
        bool[,] cut = new bool[rings, segs];
        foreach (var t in tunnels)
            for (int k = -t.widthSegs; k <= t.widthSegs; k++)
                cut[t.ring, Mod(t.sector + k, segs)] = true;

        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float y = -t * totalH;
            Vector2 center = MeanderCenter(y, nx, nz);
            float baseR = Mathf.Lerp(baseRadiusTop, baseRadiusBottom, t);

            // для полок
            bool hasLedge = (_rng.NextDouble() < ledgeChance);
            int ledgeIdx = _rng.Next(0, segs);

            for (int i = 0; i <= segs; i++)
            {
                float ang = (i % segs) / (float)segs * Mathf.PI * 2f;
                float radius = baseR
                    + fbm3(Mathf.Cos(ang) * radiusNoiseFreq + nr,
                           Mathf.Sin(ang) * radiusNoiseFreq + nr,
                           (y * 0.1f) * radiusNoiseFreq) * radiusNoiseAmp;

                // ниша (локальное расширение)
                if (_rng.NextDouble() < alcoveChance && i%7==0)
                    radius += 1.0f;

                // полка (выпирает внутрь)
                if (hasLedge && (i % segs) == ledgeIdx) radius = Mathf.Max(0.5f, radius - ledgeSize);

                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector3 p = new Vector3(center.x + dir.x * radius, y, center.y + dir.y * radius);

                int vi = r * vpr + i;
                verts[vi] = p;
                uvs[vi] = new Vector2(i / (float)segs, t);

                Vector3 outward = new Vector3(dir.x, 0, dir.y);
                norms[vi] = -Vector3.Slerp(outward, Vector3.up, normalSmoothing).normalized;
            }
        }

        // треугольники, пропуская окна под тоннели
        for (int r = 0; r < rings - 1; r++)
        {
            int r0 = r * vpr;
            int r1 = (r + 1) * vpr;
            for (int i = 0; i < segs; i++)
            {
                if (cut[r, i] || cut[r+1, i]) continue; // проём
                int a = r0 + i;
                int b = r0 + i + 1;
                int c = r1 + i;
                int d = r1 + i + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        // посчитать геометрию «рта» каждого тоннеля
        for (int tIdx = 0; tIdx < tunnels.Count; tIdx++)
        {
            var tnl = tunnels[tIdx];
            int vi = tnl.ring * vpr + tnl.sector;
            int viNext = tnl.ring * vpr + Mod(tnl.sector+1, segs);
            Vector3 p = verts[vi];
            Vector3 q = verts[viNext];
            Vector3 tangent = (q - p).normalized;
            Vector3 inward = -norms[vi];
            tnl.mouthPos = p;
            tnl.mouthNormal = inward;
            tnl.mouthTangent = tangent;
            tunnels[tIdx] = tnl;
        }
    }

    List<Tunnel> PlanTunnels(int rings, int segs)
    {
        var list = new List<Tunnel>();
        for (int n = 0; n < tunnelsCount; n++)
        {
            int ring = Mathf.Clamp(_rng.Next(4, rings - 6), 0, rings-1);
            int sector = _rng.Next(0, segs);
            var t = new Tunnel
            {
                ring = ring,
                sector = sector,
                widthSegs = Mathf.Clamp(2, 1, 6),
                length = Mathf.Lerp(tunnelLenRange.x, tunnelLenRange.y, (float)_rng.NextDouble()),
                radius = Mathf.Lerp(tunnelRadiusRange.x, tunnelRadiusRange.y, (float)_rng.NextDouble())
            };
            list.Add(t);
        }
        return list;
    }

    // ---------- Tunnels ----------
    Mesh BuildTunnelsMesh(List<Tunnel> tunnels)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        foreach (var t in tunnels)
            BuildOneTunnel(t, verts, uvs, norms, tris);

        var m = new Mesh { name = "CaveTunnels" };
        m.indexFormat = (verts.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetNormals(norms);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    void BuildOneTunnel(Tunnel T, List<Vector3> V, List<Vector2> UV, List<Vector3> N, List<int> Tr)
    {
        int segs = Mathf.Max(6, tunnelSegmentsAround);
        int rings = Mathf.CeilToInt(T.length / metersPerRing) + 1;
        int vpr = segs + 1;

        // базовые ортонормальные оси у устья
        Vector3 forward = (Quaternion.AngleAxis(-90, Vector3.up) * T.mouthTangent); // примерно перпендикул. стене
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.Cross(T.mouthNormal, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;

        int baseIndex = V.Count;

        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float dist = t * T.length;

            // кривизна туннеля
            float side = (fbm1((dist + seed) * tunnelCurveFreq) - 0.5f) * 2f * tunnelCurveAmp;
            float rise = (fbm1((dist + 999 + seed) * tunnelCurveFreq) - 0.5f) * 1.5f;

            Vector3 center = T.mouthPos
                + forward * dist
                + right * side * 0.3f
                + up * rise;

            float radius = T.radius * (0.85f + 0.3f * fbm1((dist + 333) * 0.2f));

            for (int i = 0; i <= segs; i++)
            {
                float ang = (i % segs) / (float)segs * Mathf.PI * 2f;
                Vector3 dir = (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)).normalized;
                Vector3 p = center + dir * radius;

                V.Add(p);
                UV.Add(new Vector2(i / (float)segs, t));
                N.Add(-dir); // внутрь
            }
        }

        for (int r = 0; r < rings - 1; r++)
        {
            int r0 = baseIndex + r * vpr;
            int r1 = baseIndex + (r + 1) * vpr;
            for (int i = 0; i < segs; i++)
            {
                int a = r0 + i;
                int b = r0 + i + 1;
                int c = r1 + i;
                int d = r1 + i + 1;
                Tr.Add(a); Tr.Add(c); Tr.Add(b);
                Tr.Add(b); Tr.Add(c); Tr.Add(d);
            }
        }
    }

    // ---------- Utils ----------
    void CombineMeshes(Mesh a, Mesh b, MeshFilter mf)
    {
        var combines = new CombineInstance[2];
        combines[0] = new CombineInstance { mesh = a, transform = Matrix4x4.identity };
        combines[1] = new CombineInstance { mesh = b, transform = Matrix4x4.identity };
        var merged = new Mesh { name = "CaveMerged" };
        merged.indexFormat = (a.vertexCount + b.vertexCount > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        merged.CombineMeshes(combines, true, true, false);
        mf.sharedMesh = merged;
    }

    Vector2 MeanderCenter(float y, float nx, float nz)
    {
        float f = pathFreq * 0.15f;
        float mx = (fbm1((y + nx) * f) - 0.5f) * 2f * pathAmplitude;
        float mz = (fbm1((y + nz) * f) - 0.5f) * 2f * pathAmplitude;
        return new Vector2(mx, mz);
    }

    float fbm1(float x, int oct = 4, float lac = 2f, float gain = 0.5f)
    {
        float a = 0f, amp = 0.5f, freq = 1f;
        for (int i = 0; i < oct; i++)
        {
            a += Mathf.PerlinNoise(x * freq, 0.1234f) * amp;
            freq *= lac; amp *= gain;
        }
        return a;
    }
    float fbm3(float x, float y, float z, int oct = 4, float lac = 2f, float gain = 0.5f)
    {
        float a = 0f, amp = 0.5f, freq = 1f;
        for (int i = 0; i < oct; i++)
        {
            a += (
                Mathf.PerlinNoise(x * freq, y * freq) +
                Mathf.PerlinNoise(y * freq, z * freq) +
                Mathf.PerlinNoise(z * freq, x * freq)
            ) / 3f * amp;
            freq *= lac; amp *= gain;
        }
        return a;
    }
    int Mod(int x, int m) => (x % m + m) % m;
}