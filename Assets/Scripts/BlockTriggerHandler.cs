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
    private float xOffset;
    private float yOffset;

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (!isPicked)
            return;

        timer += Time.deltaTime;

        renderer.transform.position = new Vector3(
            playerTransform.position.x + xOffset,
            playerTransform.position.y + yOffset + goingUpAnimationSpeed * timer,
            renderer.transform.position.z);

        renderer.material.SetFloat("_Alpha", Mathf.InverseLerp(goingUpAnimationDuration, 0, timer));

        if (timer >= goingUpAnimationDuration)
            container.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickTrigger"))
        {
            Debug.Log("Pick");
            isPicked = true;
            myCollider.enabled = false;
            xOffset = renderer.transform.position.x - playerTransform.position.x;
            yOffset = renderer.transform.position.y - playerTransform.position.y;
        }
        else if (other.CompareTag("MissTrigger") && !isPicked)
            Debug.Log("Miss");
    }
}
