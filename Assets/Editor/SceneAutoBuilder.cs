#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneAutoBuilder
{
    private const int HoleBaseQueue = 3000;
    private const int GroundQueue = 2000;
    private const int HoleMaskQueue = HoleBaseQueue + 0;
    private const int FallingQueue = HoleBaseQueue + 5;
    private const int RimQueue = HoleBaseQueue + 10;
    private const int IdleQueue = HoleBaseQueue + 20;

    [MenuItem("Tools/Hole Game/Create Test Scene")]
    public static void CreateTestScene()
    {
        int choice = EditorUtility.DisplayDialogComplex(
            "Hole Game",
            "Create a new test scene (recommended), or clear current scene and rebuild?",
            "Create New Scene",
            "Cancel",
            "Clear Current Scene");

        if (choice == 1)
        {
            Debug.Log("Hole Game scene build canceled.");
            return;
        }

        if (choice == 0)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        else
        {
            ClearCurrentSceneObjects();
        }

        EnsureFolders("Assets/HoleGameGenerated");
        EnsureFolders("Assets/HoleGameGenerated/Materials");

        Material groundMat = CreateOrLoadMaterial(
            "Assets/HoleGameGenerated/Materials/GroundMaterial.mat",
            FindBestOpaqueShader(),
            new Color(0.26f, 0.34f, 0.24f),
            GroundQueue);

        Material holeCircleMat = CreateOrLoadMaterial(
            "Assets/HoleGameGenerated/Materials/HoleMaskDisc.mat",
            FindRequiredShader("HoleGame/HoleMask"),
            new Color(0.05f, 0.05f, 0.05f, 0.85f),
            HoleMaskQueue);

        Material fallingItemMat = CreateOrLoadMaterial(
            "Assets/HoleGameGenerated/Materials/FallingItemMat.mat",
            FindRequiredShader("HoleGame/FallingStencil"),
            new Color(0.95f, 0.55f, 0.2f, 1f),
            FallingQueue);

        Material holeOutlineMat = CreateOrLoadMaterial(
            "Assets/HoleGameGenerated/Materials/HoleOutlineMat.mat",
            FindBestOpaqueShader(),
            new Color(0.0f, 0.0f, 0.0f, 1f),
            RimQueue);

        Material idleItemMat = CreateOrLoadMaterial(
            "Assets/HoleGameGenerated/Materials/IdleItemMat.mat",
            FindBestOpaqueShader(),
            new Color(0.95f, 0.55f, 0.2f, 1f),
            IdleQueue);

        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        camGO.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 22f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.transform.position = new Vector3(0f, 30f, 0f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            groundRenderer.sharedMaterial = groundMat;
        }

        GameObject holeGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        holeGO.name = "Hole";
        HoleController holeController = holeGO.AddComponent<HoleController>();
        holeController.holeRadius = 1.5f;
        holeController.moveSpeed = 16f;
        holeController.surfaceY = 0.01f;
        holeController.clampToArea = true;
        holeGO.transform.position = new Vector3(0f, holeController.surfaceY, 0f);
        holeGO.transform.localScale = new Vector3(holeController.holeRadius * 2f, 0.01f, holeController.holeRadius * 2f);
        Renderer holeRenderer = holeGO.GetComponent<Renderer>();
        if (holeRenderer != null)
        {
            holeRenderer.sharedMaterial = holeCircleMat;
        }

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "HoleRing";
        ring.transform.SetParent(holeGO.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.005f, 0f);
        ring.transform.localScale = new Vector3(1.08f, 0.2f, 1.08f);
        Renderer ringRenderer = ring.GetComponent<Renderer>();
        if (ringRenderer != null)
        {
            ringRenderer.sharedMaterial = holeOutlineMat;
        }

        Object.DestroyImmediate(ring.GetComponent<Collider>());

        GameObject gmGO = new GameObject("GameManager");
        SimpleGameManager gameManager = gmGO.AddComponent<SimpleGameManager>();
        gameManager.holeController = holeController;
        gameManager.idleItemMaterial = idleItemMat;
        gameManager.fallingItemMaterial = fallingItemMat;
        gameManager.spawnCount = 260;
        gameManager.spawnAreaSize = new Vector2(36f, 36f);
        gameManager.fallSpeed = 5.5f;
        gameManager.collectDepth = -5f;
        gameManager.minItemScale = 0.6f;
        gameManager.maxItemScale = 1.3f;
        gameManager.spawnOnStart = false;

        Text scoreText = CreateScoreUI();
        gameManager.scoreText = scoreText;

        gameManager.SpawnItems(gameManager.spawnCount);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject = gmGO;

        Debug.Log(
            "Hole test scene created. Render queues: " +
            "Ground=2000, HoleMaskDisc=3000, FallingItemMat=3005, HoleOutlineMat=3010, IdleItemMat=3020.");
    }

    private static void ClearCurrentSceneObjects()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Object.DestroyImmediate(roots[i]);
        }
    }

    private static Text CreateScoreUI()
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("ScoreText");
        textGO.transform.SetParent(canvasGO.transform, false);

        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        text.text = "Score: 0";

        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(500f, 80f);

        return text;
    }

    private static void EnsureFolders(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string leaf = Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolders(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static Material CreateOrLoadMaterial(string assetPath, Shader shader, Color color, int renderQueue)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.name = Path.GetFileNameWithoutExtension(assetPath);
        SetMaterialColor(mat, color);
        mat.renderQueue = renderQueue;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private static Shader FindBestOpaqueShader()
    {
        string[] shaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Standard",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color"
        };

        for (int i = 0; i < shaderNames.Length; i++)
        {
            Shader s = Shader.Find(shaderNames[i]);
            if (s != null)
            {
                return s;
            }
        }

        return Shader.Find("Standard");
    }

    private static Shader FindRequiredShader(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new System.Exception("Required shader not found: " + shaderName);
        }

        return shader;
    }

    private static void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
    }
}
#endif
