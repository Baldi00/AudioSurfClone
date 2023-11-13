using UnityEngine;

public class BlockTriggerHandler : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private new Renderer renderer;
    [SerializeField] private Collider myCollider;
    [SerializeField] private float goingUpAnimationSpeed;
    [SerializeField] private float goingUpAnimationDuration;

    private bool isPicked;

    private GameManager gameManager;
    private Transform playerTransform;

    private float timer;
    private float xOffset;
    private float yOffset;

    void Awake()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
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
            isPicked = true;
            myCollider.enabled = false;
            xOffset = renderer.transform.position.x - playerTransform.position.x;
            yOffset = renderer.transform.position.y - playerTransform.position.y;
            gameManager.BlockPicked();
        }
        else if (other.CompareTag("MissTrigger") && !isPicked)
            gameManager.BlockMissed();
    }
}
