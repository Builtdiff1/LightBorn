using UnityEngine;

public class MarkerLife : MonoBehaviour
{
    public float shrinkSpeed = 5f; // Smooth shrink speed
    private Vector3 initialScale;
    private Vector3 targetScale;
    private bool shrinking = false;

    private void Awake()
    {
        initialScale = transform.localScale;
        targetScale = initialScale;
    }

    private void Update()
    {
        if (shrinking)
        {
            Vector3 newScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * shrinkSpeed);
            transform.localScale = new Vector3(newScale.x, initialScale.y, newScale.z);

            if (Mathf.Approximately(transform.localScale.x, targetScale.x) &&
                Mathf.Approximately(transform.localScale.z, targetScale.z))
            {
                shrinking = false;
            }
        }
    }

    // Shrink the cylinder to a specific fraction
    public void ShrinkToFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        targetScale = new Vector3(initialScale.x * fraction, initialScale.y, initialScale.z * fraction);
        shrinking = true;
    }
}
