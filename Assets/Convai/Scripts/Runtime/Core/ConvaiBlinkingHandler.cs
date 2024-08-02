using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    ///     Controls the blinking behavior of a character model in Unity.
    /// </summary>
    /// <remarks>
    ///     Instructions to find the index of left / right eyelids in BlendShapes:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Select your character model in the scene which has the SkinnedMeshRenderer component.</description>
    ///         </item>
    ///         <item>
    ///             <description>Look for the blend shapes in the SkinnedMeshRenderer component in the Inspector window.</description>
    ///         </item>
    ///         <item>
    ///             <description> The count (from 0) of blend shape until "EyeBlink_L" or similar is the index of the left eyelid. </description>
    ///         </item>
    ///         <item>
    ///             <description> The count (from 0) of blend shape until "EyeBlink_R" or similar is the index of the right eyelid. </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Character Blinking")]
    public class ConvaiBlinkingHandler : MonoBehaviour
    {
        private const string LEFT_EYELID_REGEX = @"(eye).*(blink).*(l|left)";
        private const string RIGHT_EYELID_REGEX = @"(eye).*(blink).*(r|right)";
        private const string FACE_MESH_REGEX = @"(.*_Head|CC_Base_Body)";

        [SerializeField] [Tooltip("The SkinnedMeshRenderer for the character's face")]
        private SkinnedMeshRenderer faceSkinnedMeshRenderer;

        [SerializeField] [Tooltip("The index of the left eyelid blend shape in the SkinnedMeshRenderer")]
        private int indexOfLeftEyelid = -1;

        [SerializeField] [Tooltip("The index of the right eyelid blend shape in the SkinnedMeshRenderer")]
        private int indexOfRightEyelid = -1;

        [SerializeField] [Tooltip("Maximum value of the blendshape of the eye lid")]
        private float maxBlendshapeWeight = 1;

        [SerializeField] [Tooltip("The minimum amount of time, in seconds, for a blink. Positive values only.")] [Range(0.1f, 1f)]
        private float minBlinkDuration = 0.2f;

        [SerializeField] [Range(0.1f, 1f)] [Tooltip("The maximum amount of time, in seconds, for a blink. Must be greater than the minimum blink duration.")]
        private float maxBlinkDuration = 0.3f;

        [SerializeField] [Tooltip("The minimum amount of time, in seconds, between blinks. Positive values only.")] [Range(1f, 10f)]
        private float minBlinkInterval = 2;

        [SerializeField] [Range(1f, 10f)] [Tooltip("The maximum amount of time, in seconds, between blinks. Must be greater than the minimum blink interval.")]
        private float maxBlinkInterval = 3;


        /// <summary>
        ///     Initializes the settings for eyelid blinking on a character's SkinnedMeshRenderer blend shapes.
        /// </summary>
        /// <remarks> This method executes the following sequence of operations:
        ///     <list type="bullet">
        ///         <item>
        ///             <description> Checks if the SkinnedMeshRenderer is associated with the character's face. If it is not found, it logs an error and returns. </description>
        ///         </item>
        ///         <item>
        ///             <description>If the indices of the left and right eyelids are not set (i.e., they are -1), it iterates over the blend shapes of the SkinnedMeshRenderer to find these indices. It uses regex to match blend shapes' names, looking for "eye" and "blink" in combination with either "_l" for left or "_r" for right indicators. The appropriate indices found are stored in PlayerPrefs for caching purposes. </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        private void Start()
        {
            InitializeBlinkingSettings();
            StartCoroutine(BlinkCoroutine());
        }

        private void OnValidate()
        {
            maxBlinkDuration = Mathf.Max(minBlinkDuration, maxBlinkDuration);
            maxBlinkInterval = Mathf.Max(minBlinkInterval, maxBlinkInterval);
        }

        private void InitializeBlinkingSettings()
        {
            string npcName = GetComponent<ConvaiNPC>().characterName;
            string leftBlinkKey = $"{npcName}LeftEyelid";
            string rightBlinkKey = $"{npcName}RightEyelid";

            // Try to load eyelid indices from PlayerPrefs
            indexOfLeftEyelid = indexOfLeftEyelid == -1 ? PlayerPrefs.GetInt(leftBlinkKey, -1) : indexOfLeftEyelid;
            indexOfRightEyelid = indexOfRightEyelid == -1 ? PlayerPrefs.GetInt(rightBlinkKey, -1) : indexOfRightEyelid;

            // Find SkinnedMeshRenderer if not set
            faceSkinnedMeshRenderer ??= GetSkinnedMeshRendererWithRegex(transform);

            if (faceSkinnedMeshRenderer == null)
            {
                ConvaiLogger.Error("No SkinnedMeshRenderer found with matching name.", ConvaiLogger.LogCategory.Character);
                return;
            }

            // Find eyelid indices if not set or loaded from PlayerPrefs
            if (indexOfLeftEyelid == -1 || indexOfRightEyelid == -1)
            {
                FindAndSetEyelidIndices(leftBlinkKey, rightBlinkKey);
            }
        }

        private void FindAndSetEyelidIndices(string leftBlinkKey, string rightBlinkKey)
        {
            for (int i = 0; i < faceSkinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                string blendShapeName = faceSkinnedMeshRenderer.sharedMesh.GetBlendShapeName(i).ToLower();
                if (indexOfLeftEyelid == -1 && Regex.IsMatch(blendShapeName, LEFT_EYELID_REGEX))
                {
                    indexOfLeftEyelid = i;
                    PlayerPrefs.SetInt(leftBlinkKey, i);
                }
                else if (indexOfRightEyelid == -1 && Regex.IsMatch(blendShapeName, RIGHT_EYELID_REGEX))
                {
                    indexOfRightEyelid = i;
                    PlayerPrefs.SetInt(rightBlinkKey, i);
                }
            }

            if (indexOfLeftEyelid == -1 || indexOfRightEyelid == -1)
            {
                ConvaiLogger.Error("Left and/or Right eyelid blend shapes not found!", ConvaiLogger.LogCategory.Character);
            }
        }

        private SkinnedMeshRenderer GetSkinnedMeshRendererWithRegex(Transform parentTransform)
        {
            Regex regexPattern = new(FACE_MESH_REGEX);

            return (from Transform child in parentTransform where regexPattern.IsMatch(child.name) select child.GetComponent<SkinnedMeshRenderer>()).FirstOrDefault(renderer =>
                renderer != null);
        }

        /// <summary>
        ///     Coroutine that controls the blinking behavior of the character.
        /// </summary>
        /// <remarks>
        ///     This coroutine is designed to perform a sequence of blinking actions where it:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>Closes the eyes smoothly over half of the defined 'blinkDuration'</description>
        ///         </item>
        ///         <item>
        ///             <description>Waits for the defined 'blinkDuration'</description>
        ///         </item>
        ///         <item>
        ///             <description>Opens the eyes smoothly over half of the defined 'blinkDuration'</description>
        ///         </item>
        ///         <item>
        ///             <description>Waits for a randomized interval time before repeating the blinking process</description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <returns>Enumerator to control the sequence of this coroutine</returns>
        private IEnumerator BlinkCoroutine()
        {
            while (true)
            {
                float blinkDuration = Random.Range(minBlinkDuration, maxBlinkDuration);
                float blinkInterval = Random.Range(minBlinkInterval, maxBlinkInterval);

                yield return StartCoroutine(PerformBlink(blinkDuration));
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        private IEnumerator PerformBlink(float blinkDuration)
        {
            // Close eyes
            yield return StartCoroutine(AnimateEyelids(0f, maxBlendshapeWeight, blinkDuration / 2f));

            // Keep eyes closed
            yield return new WaitForSeconds(blinkDuration);

            // Open eyes
            yield return StartCoroutine(AnimateEyelids(maxBlendshapeWeight, 0f, blinkDuration / 2f));
        }

        private IEnumerator AnimateEyelids(float startWeight, float endWeight, float duration)
        {
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float weight = Mathf.Lerp(startWeight, endWeight, t);
                SetEyelidsBlendShapeWeight(weight);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            SetEyelidsBlendShapeWeight(endWeight);
        }

        /// <summary>
        ///     Sets the same weight to both eyelids' blend shape.
        /// </summary>
        private void SetEyelidsBlendShapeWeight(float weight)
        {
            faceSkinnedMeshRenderer.SetBlendShapeWeight(indexOfLeftEyelid, weight);
            faceSkinnedMeshRenderer.SetBlendShapeWeight(indexOfRightEyelid, weight);
        }
    }
}