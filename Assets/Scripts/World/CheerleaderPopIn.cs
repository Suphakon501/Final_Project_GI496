using System.Collections;
using UnityEngine;

/// <summary>
/// สุ่มให้ Cheer_boy / Cheer_girl โผล่มาที่ขอบจอระหว่างเล่นเกม
/// เด้งขึ้นมาแบบการ์ตูน (ยืดตัว/บี้) แล้วโยกซ้าย-ขวาก่อนจางหายไป
/// ตั้งโอกาสการโผล่ (%), ช่วงเวลารอ และเวลาที่โผล่ก่อนหายได้ใน Inspector
/// </summary>
public class CheerleaderPopIn : MonoBehaviour
{
    [Header("Characters")]
    [Tooltip("พรีแฟบที่จะสุ่มโผล่ (Cheer_boy / Cheer_girl)")]
    [SerializeField] private GameObject[] cheerleaders;

    [Header("Chance")]
    [Tooltip("โอกาสที่จะโผล่ในแต่ละรอบ (เปอร์เซ็นต์)")]
    [SerializeField, Range(0f, 100f)] private float appearChance = 45f;
    [Tooltip("ช่วงเวลารอสุ่มก่อนจับสลากรอบถัดไป (วินาที)")]
    [SerializeField] private Vector2 intervalRange = new Vector2(6f, 12f);
    [Tooltip("รอตอนเริ่มเพลงก่อนจับสลากครั้งแรก (วินาที)")]
    [SerializeField, Min(0f)] private float startDelay = 2f;
    [Tooltip("จำนวนตัวที่โผล่พร้อมกันได้สูงสุด")]
    [SerializeField, Min(1)] private int maxVisible = 1;

    [Header("Duration")]
    [Tooltip("เวลาที่โผล่ก่อนหายไป (วินาที) — สุ่มในช่วงนี้ (นับตอนโยกซ้าย-ขวา)")]
    [SerializeField] private Vector2 visibleDuration = new Vector2(2.5f, 4.5f);

    [Header("Cartoon Bounce (เด้งขึ้นมา)")]
    [Tooltip("เวลาเด้งขึ้นมาจากด้านล่าง (วินาที)")]
    [SerializeField, Min(0.01f)] private float popUpTime = 0.3f;
    [Tooltip("เด้งขึ้นมาจากต่ำกว่าตำแหน่งจริงกี่หน่วย")]
    [SerializeField, Min(0f)] private float popFromBelow = 1.1f;
    [Tooltip("การยืด/บี้ตัวตอนเด้ง (0 = ไม่ยืดเลย)")]
    [SerializeField, Range(0f, 0.6f)] private float squash = 0.2f;

    [Header("Sway (โยกซ้าย-ขวา)")]
    [Tooltip("เอียงตัวซ้าย-ขวากี่องศา")]
    [SerializeField, Min(0f)] private float swayAngle = 5f;
    [Tooltip("ส่ายตำแหน่งซ้าย-ขวากี่หน่วย")]
    [SerializeField, Min(0f)] private float swayDistance = 0.12f;
    [Tooltip("จำนวนรอบการโยกต่อวินาที")]
    [SerializeField, Min(0.05f)] private float swaySpeed = 1.2f;
    [Tooltip("เด้งขึ้นลงเบา ๆ ประกอบการโยก (หน่วย)")]
    [SerializeField, Min(0f)] private float swayLift = 0.08f;

    [Header("Exit")]
    [Tooltip("เวลาที่ค่อย ๆ จางหาย")]
    [SerializeField, Min(0.01f)] private float fadeOut = 0.3f;
    [Tooltip("ระยะลอยขึ้นตอนจางหาย (หน่วย)")]
    [SerializeField] private float floatUp = 0.25f;

    [Header("Side Swap (สลับฝั่งซ้าย-ขวา)")]
    [Tooltip("โอกาสที่จะสลับฝั่ง (เปอร์เซ็นต์) — 0 = ไม่สลับเลย, 100 = สลับทุกครั้ง")]
    [SerializeField, Range(0f, 100f)] private float swapSideChance = 50f;
    [Tooltip("กลับด้านรูปให้หันเข้าหากลางจอเมื่อสลับฝั่ง")]
    [SerializeField] private bool flipWhenSwapped = true;

    [Header("Placement")]
    [Tooltip("ขยับตำแหน่งที่โผล่เพิ่มจากตำแหน่งในพรีแฟบ")]
    [SerializeField] private Vector2 worldOffset = Vector2.zero;
    [Tooltip("ลำดับการวาด — ตั้งสูงมากไว้หน้าสุด ไม่ให้อะไรในฉากบัง (ปุ่มโน้ตอยู่ที่ 7)")]
    [SerializeField] private int sortingOrder = 30000;

    Coroutine routine;
    int visibleCount;
    bool warnedNoPrefab;

    void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoLoop());
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    IEnumerator CoLoop()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return new WaitForSeconds(RandomInterval());

            if (!CanSpawn()) continue;
            if (appearChance < 100f && Random.value * 100f > appearChance) continue;

            GameObject prefab = PickPrefab();
            if (prefab == null) continue;

            StartCoroutine(CoShow(prefab));
        }
    }

    bool CanSpawn()
    {
        if (cheerleaders == null || cheerleaders.Length == 0) return false;
        if (Camera.main == null) return false;
        if (BossMashManager.IsMashActive) return false;
        if (visibleCount >= Mathf.Max(1, maxVisible)) return false;
        if (PlayerController.isGameOver) return false;
        if (LevelManager.instance != null && LevelManager.instance.IsFinished) return false;
        return true;
    }

    /// <summary>สุ่มเลือกพรีแฟบที่ใช้งานได้ (ข้ามช่องว่างหรือ reference ที่หลุด)</summary>
    GameObject PickPrefab()
    {
        if (cheerleaders == null || cheerleaders.Length == 0) return null;

        int start = Random.Range(0, cheerleaders.Length);
        for (int i = 0; i < cheerleaders.Length; i++)
        {
            GameObject candidate = cheerleaders[(start + i) % cheerleaders.Length];
            if (IsUsable(candidate)) return candidate;
        }

        if (!warnedNoPrefab)
        {
            warnedNoPrefab = true;
            Debug.LogWarning("[CheerleaderPopIn] ไม่พบพรีแฟบที่ใช้ได้ — ลาก Cheer_boy / Cheer_girl ใส่ช่อง Cheerleaders ใน Inspector ใหม่");
        }

        return null;
    }

    /// <summary>reference ของ Unity อาจเป็น "missing" (asset หาย/ถูกลบ) แล้วโยน MissingReferenceException ตอนเข้าถึง</summary>
    static bool IsUsable(GameObject prefab)
    {
        if (prefab == null) return false;

        try
        {
            return prefab.transform != null;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    float RandomInterval()
    {
        return Random.Range(Mathf.Min(intervalRange.x, intervalRange.y), Mathf.Max(intervalRange.x, intervalRange.y));
    }

    IEnumerator CoShow(GameObject prefab)
    {
        if (!IsUsable(prefab)) yield break;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;

        Transform cam = mainCamera.transform;
        visibleCount++;
        GameObject go = null;

        try
        {
            // ตำแหน่งจากพรีแฟบ (อยู่ติดขอบจอซ้าย/ขวา) แล้วยึดกับกล้อง
            Vector3 authored = prefab.transform.position;

            // บางครั้งสลับข้าง: บอยไปฝั่งขวา / สาวไปฝั่งซ้าย
            bool swapped = swapSideChance > 0f && Random.value * 100f < swapSideChance;
            float sideX = swapped ? -authored.x : authored.x;
            Vector3 restPos = new Vector3(
                cam.position.x + sideX + worldOffset.x,
                cam.position.y + authored.y + worldOffset.y,
                cam.position.z + 10f);

            Vector3 startPos = restPos + Vector3.down * popFromBelow;

            // สลับฝั่งให้เอียงตัวไปทางเดิมเมื่อเทียบกับกลางจอ
            Quaternion spawnRot = prefab.transform.rotation;
            if (swapped)
            {
                float tilt = prefab.transform.eulerAngles.z;
                if (tilt > 180f) tilt -= 360f;
                spawnRot = Quaternion.Euler(0f, 0f, -tilt);
            }

            go = Instantiate(prefab, startPos, spawnRot);
            go.name = prefab.name + "_popin";

            SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
            if (sr != null && swapped && flipWhenSwapped) sr.flipX = true;

            Vector3 baseScale = go.transform.localScale;
            Quaternion baseRot = go.transform.rotation;
            Color tint = sr != null ? sr.color : Color.white;
            float hold = Random.Range(Mathf.Min(visibleDuration.x, visibleDuration.y), Mathf.Max(visibleDuration.x, visibleDuration.y));

            // 1) เด้งขึ้นมาจากด้านล่างแบบการ์ตูน: ตัวยืดตอนพุ่งขึ้น แล้วบี้ตอนลงถึงที่
            float t = 0f;
            while (t < popUpTime)
            {
                if (BossMashManager.IsMashActive) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / popUpTime);
                float ease = EaseOutBack(k);

                go.transform.position = new Vector3(startPos.x, Mathf.Lerp(startPos.y, restPos.y, ease), startPos.z);

                float stretch = Mathf.Sin(Mathf.PI * k);
                go.transform.localScale = new Vector3(
                    baseScale.x * (1f - squash * 0.55f * stretch),
                    baseScale.y * (1f + squash * stretch),
                    baseScale.z);

                if (sr != null) sr.color = WithAlpha(tint, Mathf.Clamp01(k * 1.6f));
                yield return null;
            }

            go.transform.position = restPos;
            go.transform.localScale = baseScale;
            if (sr != null) sr.color = WithAlpha(tint, 1f);

            // 2) โยกซ้าย-ขวา (พร้อมเด้งขึ้นลงเบา ๆ)
            t = 0f;
            while (t < hold)
            {
                if (BossMashManager.IsMashActive) yield break;
                t += Time.deltaTime;
                float sway = Mathf.Sin(t * swaySpeed * Mathf.PI * 2f);

                go.transform.position = restPos + new Vector3(sway * swayDistance, Mathf.Abs(sway) * swayLift, 0f);
                go.transform.rotation = baseRot * Quaternion.Euler(0f, 0f, sway * swayAngle);
                yield return null;
            }

            // 3) จางหาย + ลอยขึ้น (ยังโยกต่อให้ดูต่อเนื่อง)
            t = 0f;
            while (t < fadeOut)
            {
                if (BossMashManager.IsMashActive) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeOut);
                float sway = Mathf.Sin((hold + t) * swaySpeed * Mathf.PI * 2f);

                go.transform.position = restPos + new Vector3(sway * swayDistance * (1f - k), floatUp * k, 0f);
                go.transform.rotation = baseRot * Quaternion.Euler(0f, 0f, sway * swayAngle * (1f - k));
                if (sr != null) sr.color = WithAlpha(tint, 1f - k * k);
                yield return null;
            }
        }
        finally
        {
            if (go != null) Destroy(go);
            visibleCount = Mathf.Max(0, visibleCount - 1);
        }
    }

    /// <summary>โค้งเด้งเลยเป้านิดหน่อยแล้วตกลงมา (ให้ความรู้สึกการ์ตูน)</summary>
    static float EaseOutBack(float k)
    {
        const float c1 = 1.5f;
        const float c3 = c1 + 1f;
        float x = k - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
