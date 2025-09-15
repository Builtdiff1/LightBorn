using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarkerPlacement : MonoBehaviour
{
    [Header("Input Settings")]
    public InputActionReference markerAction;

    [Header("Marker Settings")]
    public GameObject markerPrefab;
    public Transform player;
    public Vector3 positionOffset = Vector3.zero;   // extra tweak if needed (e.g. Vector3.down * 0.5f)

    [Header("Cooldown Settings")]
    public float cooldownTime = 1f;
    private float lastMarkerTime = -Mathf.Infinity;

    [Header("UI Elements")]
    public GameObject readyImage;
    public GameObject cooldownImage;

    private readonly List<GameObject> activeMarkers = new List<GameObject>();

    private void Awake()
    {
        if (player == null) player = transform;
    }

    private void OnEnable()
    {
        if (markerAction != null && markerAction.action != null)
        {
            markerAction.action.performed += OnMarkerAction;
            markerAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (markerAction != null && markerAction.action != null)
        {
            markerAction.action.performed -= OnMarkerAction;
            markerAction.action.Disable();
        }
    }

    private void Update()
    {
        UpdateCooldownUI();
    }

    private void OnMarkerAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (Time.time < lastMarkerTime + cooldownTime) return;

        PlaceMarker();
        lastMarkerTime = Time.time;
    }

    // Optional: call this from other scripts/UI
    public void TriggerMarker()
    {
        if (Time.time < lastMarkerTime + cooldownTime) return;
        PlaceMarker();
        lastMarkerTime = Time.time;
    }

    private void PlaceMarker()
    {
        if (player == null || markerPrefab == null) return;

        // Spawn directly under the player
        Vector3 spawnPosition = player.position + positionOffset;
        Quaternion rot = Quaternion.identity; // keep flat rotation

        CleanupMarkers();

        GameObject newMarkerObj = Instantiate(markerPrefab, spawnPosition, rot);
        activeMarkers.Add(newMarkerObj);
    }

    private void CleanupMarkers()
    {
        for (int i = activeMarkers.Count - 1; i >= 0; i--)
        {
            var go = activeMarkers[i];
            if (go == null)
            {
                activeMarkers.RemoveAt(i);
                continue;
            }

            // If your marker script has ReduceLife(), call it without requiring it.
            go.SendMessage("ReduceLife", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void UpdateCooldownUI()
    {
        bool isReady = Time.time >= lastMarkerTime + cooldownTime;
        if (readyImage) readyImage.SetActive(isReady);
        if (cooldownImage) cooldownImage.SetActive(!isReady);
    }
}
