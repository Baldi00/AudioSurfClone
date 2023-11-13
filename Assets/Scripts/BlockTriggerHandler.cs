using UnityEngine;

public class BlockTriggerHandler : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private new Renderer renderer;
    [SerializeField] private Collider myCollider;
    [SerializeField] private float goingUpAnimationSpeed;
    [SerializeField] private float goingUpAnimationDuration;

    private bool isPicked;

    private float timer;
    private Transform playerTransform;

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (!isPicked)
            return;

        timer += Time.deltaTime;
        renderer.transform.position += goingUpAnimationSpeed * Time.deltaTime * renderer.transform.up;

        renderer.material.SetFloat("_Alpha", Mathf.InverseLerp(goingUpAnimationDuration, 0, timer));

        if (timer >= goingUpAnimationDuration)
        {
            Destroy(renderer.gameObject);
            container.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickTrigger"))
        {
            Debug.Log("Pick");

            renderer.transform.parent = playerTransform;

            isPicked = true;
            myCollider.enabled = false;
        }
        else if (other.CompareTag("MissTrigger") && !isPicked)
            Debug.Log("Miss");
    }
}
