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
    public Vector3 positionOffset = Vector3.zero;

    [Header("Marker Limits")]
    public int maxMarkers = 5;

    [Header("Cooldown Settings")]
    public float cooldownTime = 1f;
    private float lastMarkerTime = -Mathf.Infinity;

    [Header("Shrink Settings")]
    public float shrinkSpeed = 5f;

    [Header("UI Elements")]
    public GameObject readyImage;    // Active when marker is ready
    public GameObject cooldownImage; // Active when on cooldown

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

    private void PlaceMarker()
    {
        if (markerPrefab == null || player == null) return;

        // Determine spawn position (raycast down)
        Vector3 spawnOrigin = player.position + positionOffset;
        Vector3 spawnPosition = spawnOrigin;
        if (Physics.Raycast(spawnOrigin, Vector3.down, out RaycastHit hit, 100f))
        {
            spawnPosition = hit.point;
        }

        // Camera-facing rotation
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Quaternion rot = Quaternion.LookRotation(camForward, Vector3.up);

        // Spawn new marker
        GameObject newMarker = Instantiate(markerPrefab, spawnPosition, rot);
        activeMarkers.Add(newMarker);

        // Set shrink speed on cylinder
        MarkerLife newLife = newMarker.GetComponentInChildren<MarkerLife>();
        if (newLife != null)
        {
            newLife.shrinkSpeed = shrinkSpeed;
            Debug.Log($"Spawned marker: {newMarker.name} with cylinder {newLife.gameObject.name}");
        }

        // Stair-step shrink: each older marker shrinks by fixed step
        float shrinkStep = 1f / maxMarkers;
        int count = activeMarkers.Count;
        for (int i = 0; i < count; i++)
        {
            GameObject marker = activeMarkers[i];
            if (marker != null)
            {
                MarkerLife life = marker.GetComponentInChildren<MarkerLife>();
                if (life != null)
                {
                    float fraction = Mathf.Clamp01(1f - shrinkStep * (count - i - 1));
                    life.ShrinkToFraction(fraction);
                }
            }
        }

        // Remove oldest if exceeding max
        if (activeMarkers.Count > maxMarkers)
        {
            Destroy(activeMarkers[0]);
            activeMarkers.RemoveAt(0);
        }
    }

    private void UpdateCooldownUI()
    {
        bool isReady = Time.time >= lastMarkerTime + cooldownTime;

        if (readyImage != null) readyImage.SetActive(isReady);
        if (cooldownImage != null) cooldownImage.SetActive(!isReady);
    }
}
