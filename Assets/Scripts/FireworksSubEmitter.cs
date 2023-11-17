using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class FireworksSubEmitter : MonoBehaviour
{
    [SerializeField] private float interval;

    private ParticleSystem ps;
    private float timer;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        while (timer >= interval)
        {
            ps.TriggerSubEmitter(0);
            timer -= interval;
        }
    }
}
