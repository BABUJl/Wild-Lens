using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Handles camera zoom functionality for photography gameplay
/// Simulates telephoto lens by adjusting field of view
/// </summary>
public class CameraZoom : MonoBehaviour
{
    public static CameraZoom Instance { get; private set; }

    [Header("Zoom Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float[] zoomLevels = { 60f, 30f, 15f, 7.5f }; // FOV values for 1x, 2x, 4x, 8x
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float zoomSmoothTime = 0.2f;

    [Header("Zoom UI Feedback")]
    [SerializeField] private bool showDebugZoomLevel = true;

    // Current zoom state
    private int currentZoomIndex = 0;
    private float targetFOV;
    private float currentFOVVelocity;

    // Public properties
    public int CurrentZoomLevel => currentZoomIndex + 1; // 1-based for display
    public float CurrentZoomMultiplier => zoomLevels[0] / zoomLevels[currentZoomIndex];

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Set initial zoom to 1x
        targetFOV = zoomLevels[0];
        playerCamera.fieldOfView = targetFOV;
    }

    private void Update()
    {
        // Smooth zoom transition
        if (Mathf.Abs(playerCamera.fieldOfView - targetFOV) > 0.01f)
        {
            playerCamera.fieldOfView = Mathf.SmoothDamp(
                playerCamera.fieldOfView,
                targetFOV,
                ref currentFOVVelocity,
                zoomSmoothTime
            );
        }
    }

    #region Zoom Controls

    /// <summary>
    /// Zoom in to next level
    /// </summary>
    public void ZoomIn()
    {
        if (currentZoomIndex < zoomLevels.Length - 1)
        {
            currentZoomIndex++;
            targetFOV = zoomLevels[currentZoomIndex];
            OnZoomChanged();
        }
    }

    /// <summary>
    /// Zoom out to previous level
    /// </summary>
    public void ZoomOut()
    {
        if (currentZoomIndex > 0)
        {
            currentZoomIndex--;
            targetFOV = zoomLevels[currentZoomIndex];
            OnZoomChanged();
        }
    }

    /// <summary>
    /// Set zoom to specific level (0-3 for 1x, 2x, 4x, 8x)
    /// </summary>
    public void SetZoomLevel(int level)
    {
        if (level >= 0 && level < zoomLevels.Length)
        {
            currentZoomIndex = level;
            targetFOV = zoomLevels[currentZoomIndex];
            OnZoomChanged();
        }
    }

    /// <summary>
    /// Reset to 1x zoom
    /// </summary>
    public void ResetZoom()
    {
        SetZoomLevel(0);
    }

    #endregion

    #region Input System Callbacks
    // Called when scroll wheel is used (desktop testing)
    public void OnZoomScroll(InputAction.CallbackContext context)
    {
        Debug.Log("Function Called " + context);
        float scrollValue = context.ReadValue<float>();

        if (scrollValue > 0)
            ZoomIn();
        else if (scrollValue < 0)
            ZoomOut();
    }

    #endregion

    #region Events & Feedback

    private void OnZoomChanged()
    {
        // Play zoom sound effect here when you add audio
        // AudioManager.Instance?.PlaySound("CameraZoom");

        if (showDebugZoomLevel)
        {
            Debug.Log($"Zoom Level: {CurrentZoomLevel}x ({CurrentZoomMultiplier}x magnification)");
        }

        // Trigger event for UI updates
        ZoomLevelChanged?.Invoke(CurrentZoomLevel, CurrentZoomMultiplier);
    }

    // Event that other systems can subscribe to
    public System.Action<int, float> ZoomLevelChanged;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if at maximum zoom
    /// </summary>
    public bool IsMaxZoom()
    {
        return currentZoomIndex >= zoomLevels.Length - 1;
    }

    /// <summary>
    /// Check if at minimum zoom
    /// </summary>
    public bool IsMinZoom()
    {
        return currentZoomIndex == 0;
    }

    /// <summary>
    /// Get current FOV (useful for photo quality calculations)
    /// </summary>
    public float GetCurrentFOV()
    {
        return playerCamera.fieldOfView;
    }

    #endregion
}