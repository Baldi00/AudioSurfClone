using UnityEngine;

/// <summary>
/// Triggers the subemitter of the current particle system one time each interval step
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class FireworksSubEmitterTriggerer : MonoBehaviour
{
    [SerializeField] private float interval;

    private new ParticleSystem particleSystem;
    private float timer;

    void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        while (timer >= interval)
        {
            particleSystem.TriggerSubEmitter(0);
            timer -= interval;
        }
    }
}
