using UnityEngine;

public class LipidDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float lifetime = 2.0f;
    private float timer = 0f;
    private bool isCollected = false;

    void Update()
    {
        if (PlayerController.isGameOver || isCollected) return;

        timer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CollectDrop();
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void CollectDrop()
    {
        isCollected = true;

        if (HealthBarUI.instance != null)
        {
            HealthBarUI.instance.AddHealth(15f);
            Debug.Log("Lipid drop collected +15 HP");
        }

        Destroy(gameObject);
    }
}
