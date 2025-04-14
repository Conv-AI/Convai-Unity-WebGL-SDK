using System;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Attributes;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    /// Manages which ConvaiNPC is currently active based on player's direct line of sight (Raycast)
    /// and maintains the active state within a specified angle and distance threshold (Persistence).
    /// Implements Singleton pattern. Core component for the Convai SDK interaction flow.
    /// </summary>
    [DefaultExecutionOrder(-101)] // Ensure this runs before components that depend on the active NPC
    public class ConvaiNPCManager : MonoBehaviour
    {
        // Singleton Instance
        public static ConvaiNPCManager Instance { get; private set; }

        [Header("Detection Settings")]
        [Tooltip("Length of the ray used for initial NPC detection via direct hit and max distance check.")]
        [SerializeField] private float detectionDistance = 3.0f;

        [Tooltip("Total angle (degrees) of the cone. If the player looks away from the active NPC beyond half this angle, it deactivates.")]
        [SerializeField] private float detectionFOVAngle = 120f; // Persistence cone angle

        // State (Read Only in Inspector)
        [Header("Current State")]
        [Tooltip("Reference to the NPC currently considered active for interaction.")]
        [ReadOnly] public ConvaiNPC activeConvaiNPC;

        // Internal References & Cache
        private Camera _mainCamera;
        // Cache for ConvaiNPC components to avoid repeated GetComponent calls
        private readonly Dictionary<GameObject, ConvaiNPC> _convaiNPCCache = new();
        // Reference to the NPC that was last determined to be active (either by direct hit or persistence).
        private ConvaiNPC _lastHitNpc;
        // Reusable buffer for RaycastNonAlloc to avoid GC allocations
        private static readonly RaycastHit[] RaycastHits = new RaycastHit[1];

        // Event
        /// <summary>
        /// Fired when the active NPC changes. Passes the new active NPC (or null).
        /// Consumed by systems needing to know the current interaction target (e.g., GRPCWebAPI).
        /// </summary>
        public event Action<ConvaiNPC> OnActiveNPCChanged;

        #region Unity Lifecycle Methods

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                ConvaiLogger.Warn($"Duplicate instance of {nameof(ConvaiNPCManager)} detected on {gameObject.name}. Destroying this one.", ConvaiLogger.LogCategory.Character);
                Destroy(gameObject);
                return;
            }

            // Cache the main camera reference for efficiency
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                ConvaiLogger.Error($"Requires a Camera tagged as 'MainCamera' in the scene. Component disabled.", ConvaiLogger.LogCategory.Character, this);
                enabled = false; // Disable the component if the main camera is missing
            }
        }

        // Using LateUpdate as NPC activation often depends on final camera position/rotation for the frame
        private void LateUpdate()
        {
            // Only run detection logic if the component is enabled (e.g., camera found)
            if (!enabled) return;

            DetectAndMaintainActiveNPC_RaycastPersistence();
        }

        #endregion

        #region NPC Detection Logic (Raycast + Persistence)

        /// <summary>
        /// Detects NPCs via direct raycast and maintains/updates the active NPC
        /// based on angle and distance thresholds if the raycast doesn't hit.
        /// </summary>
        private void DetectAndMaintainActiveNPC_RaycastPersistence()
        {
            if (_mainCamera == null) return;

            Transform cameraTransform = _mainCamera.transform;
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            bool foundConvaiNPCViaRay = false; // Track if the direct ray hit an NPC this frame
            ConvaiNPC nearbyNPCOnRay = null;   // Store the NPC hit by the ray, if any

            // --- Stage 1: Check for direct Raycast hit ---
            // NOTE: Add LayerMask parameter to RaycastNonAlloc for optimization if needed
            if (Physics.RaycastNonAlloc(ray, RaycastHits, detectionDistance) > 0)
            {
                RaycastHit hit = RaycastHits[0];
                nearbyNPCOnRay = GetOrCacheConvaiNPC(hit.transform.gameObject);

                if (nearbyNPCOnRay != null)
                {
                    foundConvaiNPCViaRay = true;
                    if (_lastHitNpc != nearbyNPCOnRay)
                    {
                        ConvaiLogger.DebugLog($"[{nameof(ConvaiNPCManager)}] Player view targeted: {nearbyNPCOnRay.name}", ConvaiLogger.LogCategory.Character);
                        UpdateActiveNPCState(nearbyNPCOnRay); // Activate the newly hit NPC
                    }
                }
            }

            // --- Stage 2: Handle cases where Raycast did NOT hit an NPC (Persistence Check) ---
            if (!foundConvaiNPCViaRay && _lastHitNpc != null)
            {
                Vector3 rayOrigin = ray.origin;
                Vector3 lastNPCPosition = _lastHitNpc.transform.position;
                Vector3 toLastHitNPCDirection = lastNPCPosition - rayOrigin;
                float distanceToLastHitNPC = toLastHitNPCDirection.magnitude;

                bool distanceOutOfRange = distanceToLastHitNPC > detectionDistance * 1.2f;
                bool angleOutOfRange = false;

                if (!distanceOutOfRange)
                {
                    float angleToLastHitNPC = Vector3.Angle(ray.direction, toLastHitNPCDirection.normalized);
                    angleOutOfRange = angleToLastHitNPC > (detectionFOVAngle / 2.0f);
                }

                if (angleOutOfRange || distanceOutOfRange)
                {
                    ConvaiLogger.DebugLog($"[{nameof(ConvaiNPCManager)}] Player left: {(_lastHitNpc != null ? _lastHitNpc.name : "NPC")} - (Angle/Dist out of range)", ConvaiLogger.LogCategory.Character);
                    UpdateActiveNPCState(null); // Deactivate the NPC
                }
            }
        }

        #endregion

        #region State Management & Component Cache

        /// <summary>
        /// Central method to update the active NPC state. Handles setting references,
        /// flags, and invoking the OnActiveNPCChanged event.
        /// </summary>
        /// <param name="newActiveNPC">The NPC to set as active (can be null to deactivate).</param>
        private void UpdateActiveNPCState(ConvaiNPC newActiveNPC)
        {
            if (activeConvaiNPC != newActiveNPC)
            {
                if (activeConvaiNPC != null)
                {
                    activeConvaiNPC.isCharacterActive = false;
                }

                activeConvaiNPC = newActiveNPC;
                _lastHitNpc = newActiveNPC; // Keep _lastHitNpc synced with active NPC

                if (activeConvaiNPC != null)
                {
                    activeConvaiNPC.isCharacterActive = true;
                    ConvaiLogger.DebugLog($"[{nameof(ConvaiNPCManager)}] Active NPC set to: {(activeConvaiNPC != null ? activeConvaiNPC.name : "None")}", ConvaiLogger.LogCategory.Character);
                }
                else
                {
                    ConvaiLogger.DebugLog($"[{nameof(ConvaiNPCManager)}] Active NPC cleared.", ConvaiLogger.LogCategory.Character);
                }

                // Notify subscribers
                try
                {
                    OnActiveNPCChanged?.Invoke(activeConvaiNPC);
                }
                catch (Exception ex)
                {
                    ConvaiLogger.Error($"Error invoking OnActiveNPCChanged event: {ex.Message}", ConvaiLogger.LogCategory.Character, this);
                }
            }
        }

        /// <summary>
        /// Gets the ConvaiNPC component from a GameObject using an internal cache for optimization.
        /// </summary>
        private ConvaiNPC GetOrCacheConvaiNPC(GameObject obj)
        {
            if (obj == null) return null;
            if (_convaiNPCCache.TryGetValue(obj, out ConvaiNPC npc))
            {
                if (npc != null) return npc;
                else _convaiNPCCache.Remove(obj);
            }
            npc = obj.GetComponent<ConvaiNPC>();
            if (npc != null)
            {
                _convaiNPCCache[obj] = npc;
            }
            return npc;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Allows manually setting the active NPC, bypassing the automatic detection logic.
        /// </summary>
        public void SetActiveConvaiNPC(ConvaiNPC newActiveNPC)
        {
            UpdateActiveNPCState(newActiveNPC);
        }

        /// <summary>
        /// Gets the currently active ConvaiNPC as determined by the manager.
        /// </summary>
        public ConvaiNPC GetActiveConvaiNPC()
        {
            return activeConvaiNPC;
        }

        #endregion

        #region Gizmos (Editor Visualization)

        // Draw visual aids in the Scene view to represent the detection parameters
        private void OnDrawGizmos()
        {
            // Ensure a camera reference exists, try finding MainCamera if needed (for editor viewing)
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return; // Exit if no camera found
            }

            Transform cameraTransform = _mainCamera.transform;
            Vector3 origin = cameraTransform.position;
            Vector3 direction = cameraTransform.forward;

            Color previousColor = Gizmos.color; // Store the current Gizmo color

            // Draw the main detection ray (used for activation) in blue
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(origin, origin + direction * detectionDistance);

            // Draw the vision cone used for *maintaining* the active state (persistence) in yellow
            // Only draw if the angle is > 0 AND there is currently an active (_lastHitNpc) NPC
            if (detectionFOVAngle > 0 && _lastHitNpc != null)
            {
                DrawVisionConeGizmo(origin, direction, cameraTransform.up, cameraTransform.right);
            }

            Gizmos.color = previousColor; // Restore the original Gizmo color
        }

        // Helper method to draw the wireframe for the persistence cone
        private void DrawVisionConeGizmo(Vector3 origin, Vector3 direction, Vector3 up, Vector3 right)
        {
            float halfFOV = detectionFOVAngle / 2.0f;
            Quaternion lRot = Quaternion.AngleAxis(-halfFOV, up); Quaternion rRot = Quaternion.AngleAxis(halfFOV, up);
            Quaternion tRot = Quaternion.AngleAxis(halfFOV, right); Quaternion bRot = Quaternion.AngleAxis(-halfFOV, right);
            Vector3 lDir = lRot * direction; Vector3 rDir = rRot * direction; Vector3 tDir = tRot * direction; Vector3 bDir = bRot * direction;

            Gizmos.color = Color.yellow; // Use yellow to differentiate the persistence cone
            float lineLength = detectionDistance * 1.2f; // Visualize the extended distance check
            Gizmos.DrawRay(origin, lDir * lineLength); Gizmos.DrawRay(origin, rDir * lineLength);
            Gizmos.DrawRay(origin, tDir * lineLength); Gizmos.DrawRay(origin, bDir * lineLength);
            DrawArcGizmo(origin, direction, up, lineLength, detectionFOVAngle);    // Horizontal arc
            DrawArcGizmo(origin, direction, right, lineLength, detectionFOVAngle); // Vertical arc
        }

        // Helper method to draw an arc for the Gizmo cone visualization
        private static void DrawArcGizmo(Vector3 origin, Vector3 forward, Vector3 axis, float radius, float angle)
        {
            if (radius <= 0 || angle <= 0) return; // Basic validation
            const int segments = 20; // Number of line segments used to approximate the arc
            float segmentAngle = angle / segments; // Angle step per segment
            Vector3 startDirection = Quaternion.AngleAxis(-angle / 2.0f, axis) * forward;
            Vector3 previousPoint = origin + startDirection * radius;
            Gizmos.color = Color.yellow; // Ensure color consistency for the arc
            for (int i = 1; i <= segments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(segmentAngle * i - angle / 2.0f, axis);
                Vector3 nextPoint = origin + rotation * forward * radius;
                Gizmos.DrawLine(previousPoint, nextPoint); // Draw the segment
                previousPoint = nextPoint; // Update previous point for the next iteration
            }
        }
        #endregion
    }
}