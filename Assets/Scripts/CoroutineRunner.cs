using UnityEngine;
using System.Collections;

// This is a small "helper robot" that lives quietly in the scene.
// Our other scripts (like SnapChecker) can ask THIS robot to smoothly
// move something for them, since they themselves can't run animations directly.
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner instance;

    // Finds (or creates) the one-and-only CoroutineRunner in the scene
    public static CoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject runnerObject = new GameObject("CoroutineRunner");
                instance = runnerObject.AddComponent<CoroutineRunner>();
            }
            return instance;
        }
    }

    // Smoothly moves a transform from where it currently is, to a target position,
    // over a short duration - like a gentle glide instead of a instant teleport
    public void SmoothMoveTo(Transform target, Vector3 destination, float duration)
    {
        StartCoroutine(SmoothMoveRoutine(target, destination, duration));
    }

    private IEnumerator SmoothMoveRoutine(Transform target, Vector3 destination, float duration)
    {
        Vector3 startPosition = target.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Safety check: if the object got destroyed mid-animation, just stop
            if (target == null) yield break;

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // "SmoothStep" makes it ease in and slow down at the end,
            // instead of moving at a constant boring speed
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            target.position = Vector3.Lerp(startPosition, destination, easedProgress);
            yield return null; // wait one frame, then continue
        }

        // Make sure it ends up EXACTLY at the destination (no tiny rounding gaps)
        if (target != null)
        {
            target.position = destination;
        }
    }
}