using UnityEngine;

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
