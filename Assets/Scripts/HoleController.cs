using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HoleController : MonoBehaviour
{
    [Header("Tuning")]
    public float holeRadius = 1.5f;
    public float moveSpeed = 16f;
    public float surfaceY = 0.01f;

    [Header("Optional Movement Clamp")]
    public bool clampToArea = true;
    public Vector2 movementAreaHalfExtents = new Vector2(25f, 25f);

    private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
    private SimpleGameManager gameManager;
    private Camera cachedCamera;
    private Vector3 targetPosition;

    public void SetGameManager(SimpleGameManager manager)
    {
        gameManager = manager;
    }

    private void Awake()
    {
        cachedCamera = Camera.main;
        targetPosition = transform.position;
        targetPosition.y = surfaceY;
        transform.position = targetPosition;

    }

    private void LateUpdate()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        ReadInput();
        MoveHole();
        DetectItemsOverHole();
    }

    private void ReadInput()
    {
        // Mouse drag on ground plane (y = 0)
        if (Input.GetMouseButton(0) && cachedCamera != null)
        {
            Ray ray = cachedCamera.ScreenPointToRay(Input.mousePosition);
            if (groundPlane.Raycast(ray, out float hitDistance))
            {
                Vector3 hit = ray.GetPoint(hitDistance);
                targetPosition.x = hit.x;
                targetPosition.z = hit.z;
            }
        }

        // WASD / Arrow fallback
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (horizontal != 0f || vertical != 0f)
        {
            Vector3 dir = new Vector3(horizontal, 0f, vertical).normalized;
            targetPosition += dir * moveSpeed * Time.deltaTime;
        }

        if (clampToArea)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, -movementAreaHalfExtents.x, movementAreaHalfExtents.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -movementAreaHalfExtents.y, movementAreaHalfExtents.y);
        }

        targetPosition.y = surfaceY;
    }

    private void MoveHole()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    private void DetectItemsOverHole()
    {
        if (gameManager == null)
        {
            return;
        }

        List<CollectibleItem> items = gameManager.ActiveItems;
        if (items == null)
        {
            return;
        }

        Vector3 holePos = transform.position;

        // Backwards iteration is robust if list changes during frame.
        for (int i = items.Count - 1; i >= 0; i--)
        {
            CollectibleItem item = items[i];
            if (item == null || !item.IsIdle)
            {
                continue;
            }

            Vector3 itemPos = item.transform.position;
            float dx = itemPos.x - holePos.x;
            float dz = itemPos.z - holePos.z;

            float triggerRadius = holeRadius - item.ItemRadiusApprox;
            if (triggerRadius < 0.05f)
            {
                triggerRadius = 0.05f;
            }

            float sqrDist = dx * dx + dz * dz;
            if (sqrDist <= triggerRadius * triggerRadius)
            {
                item.StartFalling();
            }
        }
    }
}
