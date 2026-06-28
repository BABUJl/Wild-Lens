using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.InputSystem;

/// <summary>
/// Handles photo capture mechanics and subject detection
/// Analyzes what animals are in frame and calculates photo quality
/// </summary>
public class PhotoCapture : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CameraZoom cameraZoom;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask animalLayer; // Set animals to a specific layer
    [SerializeField] private float maxDetectionDistance = 50f;
    [SerializeField] private float minAnimalScreenSize = 0.05f; // 5% of screen
    [SerializeField] private float optimalAnimalScreenSize = 0.3f; // 30% of screen

    [Header("Photo Settings")]
    [SerializeField] private int photoWidth = 1920;
    [SerializeField] private int photoHeight = 1080;
    [SerializeField] private bool savePhotosToDisk = true;
    [SerializeField] private string photoSaveFolder = "WildLensPhotos";

    [Header("Capture Feedback")]
    [SerializeField] private float captureFlashDuration = 0.1f;
    [SerializeField] private AnimationCurve captureFlashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // Capture state
    private bool isCapturing = false;
    private List<AnimalData> detectedAnimals = new List<AnimalData>();

    // Photo statistics
    private int totalPhotosTaken = 0;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (cameraZoom == null)
            cameraZoom = GetComponent<CameraZoom>();

        // Create photo save directory
        if (savePhotosToDisk)
        {
            string savePath = Path.Combine(Application.persistentDataPath, photoSaveFolder);
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }
    }

    private void Update()
    {
        // Continuously detect what's in frame for UI feedback
        DetectAnimalsInView();
    }

    #region Animal Detection

    /// <summary>
    /// Detect all animals currently visible in camera view
    /// </summary>
    private void DetectAnimalsInView()
    {
        detectedAnimals.Clear();

        // Find all animal game objects in scene
        GameObject[] animals = GameObject.FindGameObjectsWithTag("Animal");

        foreach (GameObject animalObj in animals)
        {
            // Check if animal is in camera frustum
            Renderer animalRenderer = animalObj.GetComponent<Renderer>();
            if (animalRenderer == null) continue;

            // Check if visible to camera
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, animalRenderer.bounds))
                continue;

            // Check distance
            float distance = Vector3.Distance(playerCamera.transform.position, animalObj.transform.position);
            if (distance > maxDetectionDistance)
                continue;

            // Calculate screen size
            Vector3 screenPos = playerCamera.WorldToViewportPoint(animalRenderer.bounds.center);

            // Check if in front of camera
            if (screenPos.z < 0)
                continue;

            float screenSize = CalculateScreenSize(animalRenderer.bounds);

            // Check if large enough to photograph
            if (screenSize < minAnimalScreenSize)
                continue;

            // Get animal data component
            AnimalData animalData = animalObj.GetComponent<AnimalData>();
            if (animalData != null)
            {
                detectedAnimals.Add(animalData);
            }
        }
    }

    /// <summary>
    /// Calculate what percentage of screen the animal occupies
    /// </summary>
    private float CalculateScreenSize(Bounds bounds)
    {
        Vector3[] boundPoints = new Vector3[8];
        boundPoints[0] = bounds.min;
        boundPoints[1] = bounds.max;
        boundPoints[2] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        boundPoints[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        boundPoints[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        boundPoints[5] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        boundPoints[6] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        boundPoints[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);

        Vector2 min = Vector2.one;
        Vector2 max = Vector2.zero;

        foreach (Vector3 point in boundPoints)
        {
            Vector3 screenPoint = playerCamera.WorldToViewportPoint(point);
            if (screenPoint.z > 0) // In front of camera
            {
                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }
        }

        float width = max.x - min.x;
        float height = max.y - min.y;
        return width * height; // Returns 0-1 value
    }

    #endregion

    #region Photo Capture

    /// <summary>
    /// Capture a photo and analyze its quality
    /// </summary>
    public void CapturePhoto()
    {
        if (isCapturing) return;

        StartCoroutine(CapturePhotoCoroutine());
    }

    private System.Collections.IEnumerator CapturePhotoCoroutine()
    {
        isCapturing = true;

        // Analyze photo before capture
        PhotoScore score = AnalyzePhoto();

        // Play shutter animation/sound
        yield return StartCoroutine(PlayCaptureFlash());

        // Capture screenshot
        if (savePhotosToDisk || score.hasSubject)
        {
            Texture2D screenshot = CaptureScreenshot();

            if (savePhotosToDisk && screenshot != null)
            {
                SavePhotoToDisk(screenshot, score);
            }

            // Clean up texture
            if (screenshot != null)
                Destroy(screenshot);
        }

        totalPhotosTaken++;

        // Notify other systems
        OnPhotoTaken?.Invoke(score);

        Debug.Log($"Photo captured! Score: {score.totalScore}, Animals: {score.animalsCaptured.Count}");

        isCapturing = false;
    }

    /// <summary>
    /// Analyze current frame and calculate photo quality
    /// </summary>
    private PhotoScore AnalyzePhoto()
    {
        PhotoScore score = new PhotoScore();
        score.captureTime = System.DateTime.Now;
        score.zoomLevel = cameraZoom != null ? cameraZoom.CurrentZoomLevel : 1;

        // Check what animals are in frame
        foreach (AnimalData animal in detectedAnimals)
        {
            score.animalsCaptured.Add(animal);

            // Calculate composition score (how well framed is the animal)
            Renderer renderer = animal.GetComponent<Renderer>();
            if (renderer != null)
            {
                float screenSize = CalculateScreenSize(renderer.bounds);
                float compositionScore = CalculateCompositionScore(screenSize);
                score.compositionScore += compositionScore;

                // Distance bonus (closer = better, but not too close)
                float distance = Vector3.Distance(playerCamera.transform.position, animal.transform.position);
                float distanceScore = CalculateDistanceScore(distance, animal.animalInfo.optimalPhotoDistance);
                score.distanceScore += distanceScore;

                // Rarity bonus
                score.rarityBonus += animal.animalInfo.rarityScore;
            }
        }

        // Calculate total score
        score.hasSubject = score.animalsCaptured.Count > 0;

        if (score.hasSubject)
        {
            // Average scores per animal
            score.compositionScore /= score.animalsCaptured.Count;
            score.distanceScore /= score.animalsCaptured.Count;

            // Weighted total
            score.totalScore =
                (score.compositionScore * 40f) +  // 40% composition
                (score.distanceScore * 30f) +      // 30% distance
                (score.rarityBonus * 20f) +        // 20% rarity
                (score.zoomLevel * 10f);           // 10% zoom bonus

            // Clamp to 0-100
            score.totalScore = Mathf.Clamp(score.totalScore, 0f, 100f);
        }

        return score;
    }

    /// <summary>
    /// Calculate how well the subject is framed (0-1)
    /// </summary>
    private float CalculateCompositionScore(float screenSize)
    {
        // Optimal is around 30% of screen
        // Too small = hard to see, too large = cropped
        float deviation = Mathf.Abs(screenSize - optimalAnimalScreenSize);
        float score = 1f - (deviation / optimalAnimalScreenSize);
        return Mathf.Clamp01(score);
    }

    /// <summary>
    /// Calculate distance score based on optimal range (0-1)
    /// </summary>
    private float CalculateDistanceScore(float actualDistance, float optimalDistance)
    {
        float deviation = Mathf.Abs(actualDistance - optimalDistance);
        float score = 1f - (deviation / optimalDistance);
        return Mathf.Clamp01(score);
    }

    #endregion

    #region Screenshot Capture

    /// <summary>
    /// Capture screenshot from camera view
    /// </summary>
    private Texture2D CaptureScreenshot()
    {
        RenderTexture renderTexture = new RenderTexture(photoWidth, photoHeight, 24);
        RenderTexture previousRT = playerCamera.targetTexture;

        playerCamera.targetTexture = renderTexture;
        playerCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(photoWidth, photoHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, photoWidth, photoHeight), 0, 0);
        screenshot.Apply();

        playerCamera.targetTexture = previousRT;
        RenderTexture.active = null;
        Destroy(renderTexture);

        return screenshot;
    }

    /// <summary>
    /// Save photo to persistent storage
    /// </summary>
    private void SavePhotoToDisk(Texture2D photo, PhotoScore score)
    {
        byte[] bytes = photo.EncodeToPNG();
        string filename = $"Photo_{totalPhotosTaken}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string savePath = Path.Combine(Application.persistentDataPath, photoSaveFolder, filename);

        File.WriteAllBytes(savePath, bytes);
        Debug.Log($"Photo saved to: {savePath}");
    }

    #endregion

    #region Visual Feedback

    /// <summary>
    /// Play camera flash effect
    /// </summary>
    private System.Collections.IEnumerator PlayCaptureFlash()
    {
        // TODO: Add white flash overlay UI
        // For now just wait for shutter sound duration
        yield return new WaitForSeconds(0.1f);
    }

    #endregion

    #region Input Callbacks

    public void OnCapturePhoto(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CapturePhoto();
        }
    }

    #endregion

    #region Public API

    public List<AnimalData> GetDetectedAnimals() => detectedAnimals;
    public int GetPhotoCount() => totalPhotosTaken;
    public bool HasAnimalInFrame() => detectedAnimals.Count > 0;

    // Event when photo is taken
    public System.Action<PhotoScore> OnPhotoTaken;

    #endregion
}

/// <summary>
/// Data structure for photo scoring
/// </summary>
[System.Serializable]
public class PhotoScore
{
    public System.DateTime captureTime;
    public int zoomLevel;
    public bool hasSubject;
    public List<AnimalData> animalsCaptured = new List<AnimalData>();

    // Score components (0-1 each)
    public float compositionScore;
    public float distanceScore;
    public float rarityBonus;

    // Final score (0-100)
    public float totalScore;
}