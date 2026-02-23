using UnityEngine;

[DisallowMultipleComponent]
public class CollectibleItem : MonoBehaviour
{
    public enum ItemState
    {
        Idle,
        Falling,
        Collected
    }

    [Header("Runtime State (Read Only)")]
    [SerializeField] private ItemState state = ItemState.Idle;

    [Header("Tuning")]
    [SerializeField] private float itemRadiusApprox = 0.25f;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float collectDepth = -5f;
    [SerializeField] private float scaleDownRate = 0.35f;

    private Renderer cachedRenderer;
    private Material aboveHoleMaterial;
    private Material belowHoleMaterial;
    private SimpleGameManager gameManager;
    private Vector3 initialScale = Vector3.one;

    public bool IsIdle => state == ItemState.Idle;
    public float ItemRadiusApprox => itemRadiusApprox;
    public ItemState State => state;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        initialScale = transform.localScale;
        AutoEstimateRadiusIfNeeded();
    }

    public void Initialize(
        SimpleGameManager manager,
        Material itemAboveMaterial,
        Material itemBelowMaterial,
        float itemFallSpeed,
        float itemCollectDepth,
        float radiusApprox)
    {
        gameManager = manager;
        aboveHoleMaterial = itemAboveMaterial;
        belowHoleMaterial = itemBelowMaterial;
        fallSpeed = itemFallSpeed;
        collectDepth = itemCollectDepth;

        if (radiusApprox > 0f)
        {
            itemRadiusApprox = radiusApprox;
        }
        else
        {
            AutoEstimateRadiusIfNeeded();
        }

        state = ItemState.Idle;
        initialScale = transform.localScale;

        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        if (cachedRenderer != null && aboveHoleMaterial != null)
        {
            cachedRenderer.sharedMaterial = aboveHoleMaterial;
        }

        gameObject.SetActive(true);
        enabled = true;
    }

    public void StartFalling()
    {
        if (state != ItemState.Idle)
        {
            return;
        }

        state = ItemState.Falling;

        if (cachedRenderer != null && belowHoleMaterial != null)
        {
            cachedRenderer.sharedMaterial = belowHoleMaterial;
        }
    }

    private void Update()
    {
        if (state != ItemState.Falling)
        {
            return;
        }

        Vector3 p = transform.position;
        p.y -= fallSpeed * Time.deltaTime;
        transform.position = p;

        if (scaleDownRate > 0f)
        {
            float t = scaleDownRate * Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, initialScale * 0.2f, t);
        }

        if (p.y <= collectDepth)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (state == ItemState.Collected)
        {
            return;
        }

        state = ItemState.Collected;

        if (gameManager != null)
        {
            gameManager.NotifyItemCollected(this);
        }

        gameObject.SetActive(false);
    }

    private void AutoEstimateRadiusIfNeeded()
    {
        if (itemRadiusApprox > 0f)
        {
            return;
        }

        Collider c = GetComponent<Collider>();
        if (c == null)
        {
            itemRadiusApprox = 0.25f;
            return;
        }

        Vector3 ext = c.bounds.extents;
        itemRadiusApprox = Mathf.Max(0.05f, Mathf.Max(ext.x, ext.z));
    }
}
