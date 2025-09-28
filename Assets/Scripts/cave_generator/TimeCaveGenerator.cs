using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Генерирует вертикальный «ущелье-колодец» с извилистым центром, рваными стенами, полками и нишами.
/// Работает без вокселей: строит меш из колец (rings), соединённых в трубу.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VerticalCaveGenerator : MonoBehaviour
{
    [Header("Размеры")]
    [Min(5)] public float depth = 120f;            // Глубина пещеры (ось -Y)
    [Range(0.5f, 20f)] public float baseRadiusTop = 6f;
    [Range(0.5f, 20f)] public float baseRadiusBottom = 8f; // Можно делать шире внизу
    [Range(0.2f, 4f)] public float metersPerRing = 1.0f;   // Вертикальный шаг

    [Header("Детализация")]
    [Range(8, 128)] public int segmentsAround = 64;         // Кол-во сечений по окружности
    [Range(0f, 1f)] public float normalSmoothing = 0.35f;   // 0 — острые края, 1 — более гладко

    [Header("Извилистость/форма")]
    public int seed = 12345;
    [Range(0f, 10f)] public float pathAmplitude = 6f;       // Насколько центр «гуляет» по XZ
    [Range(0.01f, 1f)] public float pathFreq = 0.08f;       // Частота петляния
    [Range(0f, 3f)] public float radiusNoiseAmp = 1.4f;     // Рваность контура
    [Range(0.01f, 1f)] public float radiusNoiseFreq = 0.35f;
    [Range(0f, 2f)] public float verticalRuggedness = 0.6f; // «ступенчатость» по высоте

    [Header("Геймплейные элементы")]
    [Range(0f, 1f)] public float ledgeChance = 0.18f;       // Шанс полки на кольцо
    [Range(0.2f, 2f)] public float ledgeSize = 1.0f;        // Насколько полка выпирает внутрь
    [Range(0f, 1f)] public float alcoveChance = 0.12f;      // Шанс ниши (локальное расширение)
    [Range(1f, 6f)] public float alcoveWidth = 3f;          // Угловая ширина ниши (в сегментах)

    [Header("Декор (необязательно)")]
    public Transform vinePrefab;     // лиана
    public Transform rootPrefab;     // корень
    public Transform waterfallPrefab;// водопад
    [Range(0f, 1f)] public float decorDensity = 0.2f;

    Mesh _mesh;
    MeshCollider _collider;
    System.Random _rng;

    void OnValidate() { metersPerRing = Mathf.Max(0.2f, metersPerRing); }

    [ContextMenu("Generate")]
    public void Generate()
    {
        _rng = new System.Random(seed);
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        _mesh = new Mesh { name = "CaveShaft" };
        mf.sharedMesh = _mesh;
        _collider = GetComponent<MeshCollider>();

        // ---- параметры сетки ----
        int rings = Mathf.CeilToInt(depth / metersPerRing) + 1;
        int segs = Mathf.Max(8, segmentsAround);
        float totalHeight = (rings - 1) * metersPerRing;

        // Буферы
        int vertsPerRing = segs + 1; // дублируем первый верш. в конце для удобного UV-шва
        Vector3[] verts = new Vector3[rings * vertsPerRing];
        Vector3[] norms = new Vector3[verts.Length];
        Vector2[] uvs = new Vector2[verts.Length];
        List<int> tris = new List<int>(rings * segs * 6);

        // Случайные фазы для шума
        float nx = (float)_rng.NextDouble() * 1000f;
        float nz = (float)_rng.NextDouble() * 1000f;
        float nr = (float)_rng.NextDouble() * 1000f;

        // Предзадания ниш/полок по кольцам
        bool[] ringHasLedge = new bool[rings];
        int[] ringAlcoveStart = new int[rings]; // -1 = нет
        for (int r = 0; r < rings; r++)
        {
            ringHasLedge[r] = Random01() < ledgeChance;
            ringAlcoveStart[r] = (Random01() < alcoveChance) ? _rng.Next(0, segs) : -1;
        }

        // ---- генерация колец ----
        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float y = -t * totalHeight;

            // Извилистый центр
            Vector2 center = MeanderCenter(y, nx, nz);

            // Базовый радиус по глубине (можно расширять книзу)
            float baseR = Mathf.Lerp(baseRadiusTop, baseRadiusBottom, t);

            for (int i = 0; i <= segs; i++)
            {
                float ang = (i % segs) / (float)segs * Mathf.PI * 2f;

                // Радиальный шум (рваность края + вертикальная «шахматность»)
                float radialJitter =
                    fbm3( Mathf.Cos(ang) * radiusNoiseFreq + nr,
                          Mathf.Sin(ang) * radiusNoiseFreq + nr,
                          (y * 0.1f) * radiusNoiseFreq ) * radiusNoiseAmp;

                float vertStep = (Mathf.PerlinNoise(y * 0.1f, ang * 0.5f) - 0.5f) * verticalRuggedness;

                float radius = Mathf.Max(0.5f, baseR + radialJitter + vertStep);

                // Ниша (локальное расширение на части окружности)
                int alcoveStart = ringAlcoveStart[r];
                if (alcoveStart >= 0)
                {
                    int width = Mathf.RoundToInt(alcoveWidth);
                    int di = DeltaAngleIndex(i, alcoveStart, segs);
                    if (Mathf.Abs(di) <= width)
                    {
                        float falloff = 1f - Mathf.Abs(di) / (width + 0.001f);
                        radius += falloff * (1.2f + 0.8f * Random01()); // расширяем стену, т.е. в полость
                    }
                }

                // Полка (выпирает внутрь пещеры => уменьшаем радиус локально на одном секторе)
                if (ringHasLedge[r])
                {
                    int ledgeIndex = (int)(fbm1(y * 0.05f + nr) * segs) % segs;
                    if (i % segs == ledgeIndex)
                        radius = Mathf.Max(0.4f, radius - ledgeSize);
                }

                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector3 p = new Vector3(center.x + dir.x * radius, y, center.y + dir.y * radius);

                int vi = r * vertsPerRing + i;
                verts[vi] = p;

                // UV: V — глубина, U — угол
                uvs[vi] = new Vector2(i / (float)segs, t);

                // Нормаль — от центра наружу (мы смотрим изнутри, значит инвертируем позже)
                Vector3 outward = new Vector3(dir.x, 0, dir.y);
                Vector3 n = Vector3.Slerp(outward, Vector3.up, normalSmoothing).normalized;
                norms[vi] = -n; // инверт внутрь
            }
        }

        // Треугольники
        for (int r = 0; r < rings - 1; r++)
        {
            int r0 = r * vertsPerRing;
            int r1 = (r + 1) * vertsPerRing;
            for (int i = 0; i < segs; i++)
            {
                int a = r0 + i;
                int b = r0 + i + 1;
                int c = r1 + i;
                int d = r1 + i + 1;

                // две триады (внутренняя сторона!)
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        // Собираем меш
        _mesh.Clear();
        _mesh.indexFormat = (verts.Length > 65000) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        _mesh.vertices = verts;
        _mesh.normals = norms;
        _mesh.uv = uvs;
        _mesh.triangles = tris.ToArray();
        _mesh.RecalculateBounds();

        // Коллизия
        _collider.sharedMesh = null;
        _collider.sharedMesh = _mesh;

        // Спавн декора (по желанию)
        SpawnDecorAlongWalls(verts, vertsPerRing, segs, rings);

        // Визуальный контроль
        Debug.Log($"Cave generated: rings={rings}, segs={segs}, verts={verts.Length}, tris={tris.Count/3}");
    }

    // ---- Вспомогательные ----

    Vector2 MeanderCenter(float y, float nx, float nz)
    {
        // Делаем «течь» центра с FBM по XZ
        float f = pathFreq * 0.15f;
        float mx = (fbm1((y + nx) * f) - 0.5f) * 2f * pathAmplitude;
        float mz = (fbm1((y + nz) * f) - 0.5f) * 2f * pathAmplitude;
        return new Vector2(mx, mz);
    }

    // Fractional Brownian Motion (простая)
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
        // 3D noise через «склейку» 2D Perlin (хватает для органики)
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

    float Random01() => (float)_rng.NextDouble();

    int DeltaAngleIndex(int i, int j, int segs)
    {
        int d = Mathf.Abs(i - j);
        return (d <= segs - d) ? (i - j) : (i < j ? i + (segs - j) : i - (j + segs));
    }

    void SpawnDecorAlongWalls(Vector3[] verts, int vertsPerRing, int segs, int rings)
    {
        if (decorDensity <= 0f) return;
        Transform parent = new GameObject("CaveDecor").transform;
        parent.SetParent(transform, false);

        for (int r = 0; r < rings; r++)
        {
            if (Random01() > decorDensity) continue;
            int i = _rng.Next(0, segs);
            int vi = r * vertsPerRing + i;
            Vector3 p = verts[vi];
            Vector3 up = Vector3.up;

            // Оценка крутизны (для водопадов)
            bool steep = (r < rings - 2);
            if (steep)
            {
                Vector3 p2 = verts[(r + 2) * vertsPerRing + i];
                steep = Vector3.Angle((p2 - p).normalized, up) > 20f;
            }

            // Случайно выбираем тип
            float roll = Random01();
            if (waterfallPrefab && steep && roll < 0.25f)
            {
                var t = Instantiate(waterfallPrefab, p + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.zero - p), parent);
                t.localScale *= 1f + Random01();
            }
            else if (vinePrefab && roll < 0.6f)
            {
                var t = Instantiate(vinePrefab, p + Vector3.down * 0.2f, Quaternion.identity, parent);
                t.up = Vector3.down; // «свисает»
                t.localScale *= 0.7f + Random01() * 1.2f;
            }
            else if (rootPrefab)
            {
                var t = Instantiate(rootPrefab, p, Quaternion.identity, parent);
                t.forward = (new Vector3(p.x, 0, p.z) - Vector3.zero).normalized;
                t.localScale *= 0.8f + Random01() * 1.4f;
            }
        }
    }
}