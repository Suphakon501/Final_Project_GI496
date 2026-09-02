using UnityEngine;

public class LipidSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject lipidPrefab; // Prefab ก้อนไขมัน

    [Header("Wave Pattern")]
    public int lipidsPerWave = 3;
    public float beatsBetweenSpawns = 1f;
    public float restBeats = 4f;

    private float nextTargetBeat = 4f; 
    private int spawnedInCurrentWave = 0; // ตัวนับว่าเกิดไปกี่ตัวแล้วในชุดนี้

    void Update()
    {
        if (HeartbeatManager.instance == null || lipidPrefab == null) return;

        float currentBeat = HeartbeatManager.instance.heartPositionInBeats;
        float beatsToReach = lipidPrefab.GetComponent<LipidMovement>().beatsToReachTarget;

        if (currentBeat >= nextTargetBeat - beatsToReach)
        {
            SpawnLipid();
        }
    }

    void SpawnLipid()
    {
        //เสกไขมัน
        GameObject newLipid = Instantiate(lipidPrefab, transform.position, Quaternion.identity);
        newLipid.GetComponent<LipidMovement>().targetBeat = nextTargetBeat;

        // นับเว่าเกิดไปแล้วกี่ตัว
        spawnedInCurrentWave++;

        //เกิดตัวถัดไป
        if (spawnedInCurrentWave >= lipidsPerWave)
        {
            nextTargetBeat += restBeats;
            spawnedInCurrentWave = 0;
        }
        else
        {
            nextTargetBeat += beatsBetweenSpawns;
        }
    }
}