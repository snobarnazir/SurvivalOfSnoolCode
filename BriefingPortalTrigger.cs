using UnityEngine;

public class BriefingPortalTrigger : MonoBehaviour
{
    [Header("Sequence Settings")]
    public BriefingBox.SequenceType sequenceToPlay;

    [Header("Loading Delays")]
    public float startDelay = 7f; // Delay before message typing starts

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            BriefingBox briefing = other.GetComponentInChildren<BriefingBox>(true);

            if (briefing != null)
            {
                briefing.PlaySequence(sequenceToPlay, startDelay);
            }
        }
    }
}