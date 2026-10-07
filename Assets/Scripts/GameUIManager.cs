using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    void Awake()
    {
        instance = this;
    }

    public void HideAllUI()
    {
    }
}
