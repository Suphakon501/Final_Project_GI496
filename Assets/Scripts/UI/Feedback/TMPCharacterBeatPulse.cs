using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPCharacterBeatPulse : MonoBehaviour
{
    [Header("Pulse Scale")]
    [SerializeField] private float restScale = 1f;
    [SerializeField] private float peakScale = 1.25f;

    private TMP_Text textComponent;
    private MiniLipid miniLipid;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        miniLipid = GetComponentInParent<MiniLipid>();
    }

    void LateUpdate()
    {
        if (BeatManager.instance == null || miniLipid == null) return;

        int charIndex = 0;

        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;
        if (charIndex >= textInfo.characterCount) return;

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        float phase = BeatManager.instance.songPositionInBeats * Mathf.PI * 2f;
        float pulse01 = (Mathf.Cos(phase) + 1f) * 0.5f;
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
