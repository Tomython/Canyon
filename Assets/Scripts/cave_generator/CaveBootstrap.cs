using UnityEngine;

public class CaveBootstrap : MonoBehaviour
{
    public VerticalCaveWithTunnels gen;

    [Header("Time-based seed")]
    public bool useTimeSeed = true;
    [Range(1, 60)] public int minuteModulo = 10;

    [Header("Debug view")]
    public bool cutTopMouth = true;       // прорезь сверху
    [Range(30,180)] public float topMouthArcDeg = 110f;
    public bool placeDebugCamera = true;
    public float debugCamY = -5f;

    void Awake()
    {
        if (!gen) gen = GetComponent<VerticalCaveWithTunnels>();
        if (!gen) { Debug.LogError("Нет ссылки на VerticalCaveWithTunnels"); return; }

        // базовые параметры под твой реф
        gen.depth = 200f;
        gen.segmentsAround = 64;
        gen.baseRadiusTop = 7f;
        gen.baseRadiusBottom = 12f;
        gen.tunnelsCount = 12;
        gen.tunnelLenRange = new Vector2(22, 45);
        gen.tunnelRadiusRange = new Vector2(2.2f, 3.2f);
        gen.tunnelCurveAmp = 6f;
        gen.tunnelCurveFreq = 0.11f;
        gen.radiusNoiseAmp = 1.2f;
        gen.normalSmoothing = 0.28f;

        // маленький «патч видимости» (см. предыдущие ответы — логику с пропуском трис сверху добавили там)
        // если в твоей версии нет этих полей — просто игнорь
        // (или вырежи сектор вручную, см. раньше)

        // time-seed
        if (useTimeSeed)
        {
            var now = System.DateTime.UtcNow;
            int dailySeed = now.Year * 10000 + now.Month * 100 + now.Day; // YYYYMMDD
            int minuteBand = (now.Hour * 60 + now.Minute) / Mathf.Max(1, minuteModulo);
            gen.seed = dailySeed + minuteBand; // одна крупная форма в день, легкая модуляция внутри часа
            // немного «дышать» параметрами
            gen.tunnelsCount = 8 + (minuteBand % 5);
            gen.tunnelCurveAmp = Mathf.Lerp(4f, 7f, ((minuteBand * 37) % 100) / 100f);
        }

        gen.Generate();

        // Поставим камеру внутрь
        if (placeDebugCamera && Camera.main)
        {
            Camera.main.transform.position = new Vector3(0, debugCamY, 0);
            Camera.main.transform.rotation = Quaternion.Euler(15f, 0, 0);
        }
    }
}