using UnityEngine;
using TMPro;

// เด้งเฉพาะ "ตัวอักษรเดียว" ที่กำลังต้องกดอยู่ ไม่ใช่ทั้งก้อนข้อความ (แก้ปัญหาที่ BeatPulseUI สเกลทั้ง RectTransform)
// อ่าน index ตัวอักษรที่ต้องไฮไลต์จาก GameUIManager.instance.HighlightCharIndex
[RequireComponent(typeof(TMP_Text))]
public class TMPCharacterBeatPulse : MonoBehaviour
{
    [Header("Pulse Scale")]
    [SerializeField] private float restScale = 1f;
    [SerializeField] private float peakScale = 1.25f;

    private TMP_Text textComponent;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void LateUpdate()
    {
        if (BeatManager.instance == null || GameUIManager.instance == null) return;

        int charIndex = GameUIManager.instance.HighlightCharIndex;
        if (charIndex < 0) return;

        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;
        if (charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        // ใช้คลื่น cosine แทนการพับครึ่งแบบเดิม เพราะพับครึ่งจะมีมุมหักตรงจุดตรงบีทพอดี ทำให้ดูเด้งกระตุกลายตา
        // cosine ไล่ระดับใหญ่-เล็ก-ใหญ่-เล็กต่อเนื่องนุ่มนวล ไม่มีจุดหักเลย
        float phase = BeatManager.instance.songPositionInBeats * Mathf.PI * 2f;
        float pulse01 = (Mathf.Cos(phase) + 1f) * 0.5f; // 1 = ตรงบีทพอดี (ใหญ่สุด), 0 = กลางบีท (เล็กสุด)
        float scale = Mathf.Lerp(restScale, peakScale, pulse01);

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;
        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

        Vector3 charMid = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) * 0.5f;

        for (int j = 0; j < 4; j++)
        {
            Vector3 offset = vertices[vertexIndex + j] - charMid;
            vertices[vertexIndex + j] = charMid + offset * scale;
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
