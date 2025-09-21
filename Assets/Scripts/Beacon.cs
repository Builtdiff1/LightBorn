using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Beacon : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 5f;
    public LayerMask playerLayer;

    [Header("UI Settings")]
    public Canvas canvasPrefab;       // World-space Canvas prefab
    public Slider sliderPrefab;       // Circular slider prefab

    [Header("Timing Settings")]
    public float minFillTime = 3f;    // Minimum activation time
    public float maxFillTime = 8f;    // Maximum activation time
    private float fillTime;

    [Header("Colors")]
    public Color beaconDefaultColor = Color.white;
    public Color beaconCompleteColor = Color.green;

    private float progress = 0f;
    private bool fullyActivated = false;

    private Slider progressSlider;
    private TMP_Text progressText;
    private Renderer beaconRenderer;

    public bool FullyActivated => fullyActivated;

    void Start()
    {
        // Pick random activation time
        fillTime = Random.Range(minFillTime, maxFillTime);
        Debug.Log($"{gameObject.name} will take {fillTime:F1} seconds to activate.");

        // Get renderer
        beaconRenderer = GetComponent<Renderer>();
        if (beaconRenderer != null)
            beaconRenderer.material.color = beaconDefaultColor;

        // Instantiate canvas and slider
        if (canvasPrefab != null && sliderPrefab != null)
        {
            Canvas canvas = Instantiate(canvasPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            canvas.transform.SetParent(transform);

            progressSlider = Instantiate(sliderPrefab, canvas.transform);
            progressSlider.gameObject.SetActive(false);

            // Ensure slider 0-100
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 100f;
            progressSlider.value = 0f;

            // Find the countdown TextMeshProUGUI named "Number" recursively
            progressText = FindTMPTextRecursive(progressSlider.transform, "Number");

            if (progressText != null)
                progressText.text = fillTime.ToString("F1"); // Initialize countdown
            else
                Debug.LogWarning("Beacon: Number text not found under slider prefab!");
        }

        // Register with Exit
        Exit.RegisterBeacon(this);
        Debug.Log($"{gameObject.name} connected to Exit!");
    }

    void Update()
    {
        if (fullyActivated) return;

        bool isPlayerInRange = Physics.CheckSphere(transform.position, detectionRange, playerLayer);
        if (progressSlider == null) return;

        if (isPlayerInRange && Input.GetKey(KeyCode.E))
        {
            // Show slider first time
            if (!progressSlider.gameObject.activeSelf)
            {
                progressSlider.gameObject.SetActive(true);
                progress = 0f;
                progressSlider.value = 0f;

                if (progressText != null)
                    progressText.text = fillTime.ToString("F1");

                if (beaconRenderer != null)
                    beaconRenderer.material.color = beaconDefaultColor;
            }

            // Increment progress
            progress += Time.deltaTime;
            float remainingTime = Mathf.Max(fillTime - progress, 0f);

            // Update slider 0-100
            progressSlider.value = (progress / fillTime) * 100f;

            // Update countdown text
            if (progressText != null)
                progressText.text = remainingTime.ToString("F1");

            // Fully activated
            if (progress >= fillTime)
            {
                fullyActivated = true;

                if (beaconRenderer != null)
                    beaconRenderer.material.color = beaconCompleteColor;

                progressSlider.gameObject.SetActive(false);

                if (progressText != null)
                    progressText.text = "0.0";
            }
        }
        else
        {
            // Hide slider if released or player leaves
            if (progressSlider.gameObject.activeSelf && !fullyActivated)
            {
                progressSlider.gameObject.SetActive(false);
                progress = 0f;
                progressSlider.value = 0f;

                if (progressText != null)
                    progressText.text = fillTime.ToString("F1");
            }
        }
    }

    // Recursive search for TextMeshProUGUI named "Number"
    private TMP_Text FindTMPTextRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.GetComponent<TMP_Text>();

            TMP_Text result = FindTMPTextRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
