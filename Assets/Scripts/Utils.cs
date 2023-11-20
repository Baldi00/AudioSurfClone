using UnityEngine;

public class Utils
{
    public static GameManager GetGameManager()
    {
        return GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }
}
