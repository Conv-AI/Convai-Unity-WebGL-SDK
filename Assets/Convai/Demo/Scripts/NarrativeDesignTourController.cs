using Convai.Scripts.Runtime.Features;
using UnityEngine;

public class NarrativeDesignTourController : MonoBehaviour
{
    private ConvaiActionsHandler _convaiActionsHandler;

    private void Awake()
    {
        _convaiActionsHandler = GetComponent<ConvaiActionsHandler>();
    }

    public void MoveToTargetPoint(GameObject targetPoint)
    {
        StartCoroutine(_convaiActionsHandler.MoveTo(targetPoint));
    }
}