using UnityEngine;

public class LipidSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject lipidPrefab;

    [Header("Wave Pattern (ปรับแต่งกลุ่มที่มาต่อกัน)")]
    public int lipidsPerWave = 3;           
    public float beatsBetweenSpawns = 1.0f; 
    public float restBeats = 4.0f;          

    private float nextTargetBeat = 4.0f;
    private int spawnedInCurrentWave = 0;

    void Update()
    {
        if (HeartbeatManager.instance == null || lipidPrefab == null) return;

        float currentBeat = HeartbeatManager.instance.heartPositionInBeats;

        float beatsToReach = 4f;
        if (lipidPrefab.GetComponent<LipidMovement>() != null)
        {
            beatsToReach = lipidPrefab.GetComponent<LipidMovement>().beatsToReachTarget;
        }

       
        if (currentBeat >= nextTargetBeat - beatsToReach)
        {
            SpawnLipid();
        }
    }

    void SpawnLipid()
    {
        
        GameObject newLipid = Instantiate(lipidPrefab, transform.position, Quaternion.identity);
        newLipid.GetComponent<LipidMovement>().targetBeat = nextTargetBeat;

        spawnedInCurrentWave++;

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