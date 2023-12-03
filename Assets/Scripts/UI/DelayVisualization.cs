using UnityEngine;

public class DelayVisualization : MonoBehaviour
{
    [SerializeField] private int delayFrames;
    [SerializeField] private MonoBehaviour toVisualize;

    private int frameCount;

    void OnEnable()
    {
        frameCount = 0;
        toVisualize.enabled = false;
    }

    void Update()
    {
        frameCount++;
        if (frameCount >= delayFrames)
        {
            toVisualize.enabled = true;
            enabled = false;
        }
    }
}
