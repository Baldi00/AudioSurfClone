using UnityEngine;

public class FPSShow : MonoBehaviour
{
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 50), $"{Time.unscaledDeltaTime * 1000}ms, {1 / Time.unscaledDeltaTime}fps");
    }


}
