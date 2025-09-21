using System.Collections.Generic;
using UnityEngine;

public class Exit : MonoBehaviour
{
    private static List<Beacon> beacons = new List<Beacon>();

    // Called by beacons when they spawn
    public static void RegisterBeacon(Beacon beacon)
    {
        if (!beacons.Contains(beacon))
        {
            beacons.Add(beacon);
        }
    }

    void Update()
    {
        if (beacons.Count == 0) return;

        bool allActivated = true;
        foreach (Beacon b in beacons)
        {
            if (b != null && !b.FullyActivated)
            {
                allActivated = false;
                break;
            }
        }

        if (allActivated)
        {
            OpenExit();
        }
    }

    void OpenExit()
    {
        gameObject.SetActive(false); // Exit disappears
        Debug.Log("All beacons activated! Exit is open!");
        enabled = false; // Stop checking
    }
}
