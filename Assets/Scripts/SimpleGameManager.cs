using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SimpleGameManager : MonoBehaviour
{
    [Header("References")]
    public HoleController holeController;
    public Text scoreText;
    [FormerlySerializedAs("itemAboveHoleMaterial")]
    public Material idleItemMaterial;
    [FormerlySerializedAs("itemBelowHoleMaterial")]
    public Material fallingItemMaterial;

    [Header("Tuning Knobs")]
    public int spawnCount = 260;
    public Vector2 spawnAreaSize = new Vector2(36f, 36f);
    public float fallSpeed = 5.5f;
    public float collectDepth = -5f;
    public float minItemScale = 0.6f;
    public float maxItemScale = 1.3f;
    public bool spawnOnStart = true;

    private readonly List<CollectibleItem> activeItems = new List<CollectibleItem>(512);
    private Transform itemsRoot;
    private int score;

    public List<CollectibleItem> ActiveItems => activeItems;

    private void Awake()
    {
        if (holeController == null)
        {
            holeController = FindObjectOfType<HoleController>();
        }

        if (holeController != null)
        {
            holeController.SetGameManager(this);
            holeController.movementAreaHalfExtents = spawnAreaSize * 0.5f;
        }

        EnsureItemsRoot();
        UpdateScoreUI();
    }

    private void Start()
    {
        if (spawnOnStart && activeItems.Count == 0)
        {
            SpawnItems(spawnCount);
        }
    }

    public void SpawnItems(int count)
    {
        ClearItems();
        EnsureItemsRoot();

        float halfX = spawnAreaSize.x * 0.5f;
        float halfZ = spawnAreaSize.y * 0.5f;

        for (int i = 0; i < count; i++)
        {
            PrimitiveType primitive = (Random.value > 0.5f) ? PrimitiveType.Cube : PrimitiveType.Sphere;
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = "Item_" + i;
            go.transform.SetParent(itemsRoot, false);

            float scale = Random.Range(minItemScale, maxItemScale);
            go.transform.localScale = Vector3.one * scale;

            float y = scale * 0.5f;
            float x = Random.Range(-halfX, halfX);
            float z = Random.Range(-halfZ, halfZ);
            go.transform.position = new Vector3(x, y, z);

            CollectibleItem item = go.AddComponent<CollectibleItem>();
            float approxRadius = Mathf.Max(0.05f, scale * 0.45f);

            item.Initialize(
                this,
                idleItemMaterial,
                fallingItemMaterial,
                fallSpeed,
                collectDepth,
                approxRadius);

            activeItems.Add(item);
        }

        score = 0;
        UpdateScoreUI();
    }

    public void ClearItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            CollectibleItem item = activeItems[i];
            if (item == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(item.gameObject);
            }
            else
            {
                DestroyImmediate(item.gameObject);
            }
        }

        activeItems.Clear();
    }

    public void NotifyItemCollected(CollectibleItem item)
    {
        int index = activeItems.IndexOf(item);
        if (index >= 0)
        {
            int last = activeItems.Count - 1;
            activeItems[index] = activeItems[last];
            activeItems.RemoveAt(last);
        }

        score++;
        UpdateScoreUI();

        if (item != null)
        {
            if (Application.isPlaying)
            {
                Destroy(item.gameObject);
            }
            else
            {
                DestroyImmediate(item.gameObject);
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    private void EnsureItemsRoot()
    {
        if (itemsRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("Items");
        if (existing != null)
        {
            itemsRoot = existing;
            return;
        }

        GameObject root = new GameObject("Items");
        root.transform.SetParent(transform, false);
        itemsRoot = root.transform;
    }
}
