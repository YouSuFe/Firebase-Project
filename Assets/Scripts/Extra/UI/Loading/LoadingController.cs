using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles visual loading animation.
/// Rotates a spinner image while active.
/// </summary>
public sealed class LoadingController : MonoBehaviour
{
    #region Serialized Fields
    [Header("Loading Settings")]
    [SerializeField, Tooltip("Rotation speed of the loading spinner (degrees per second).")]
    private float rotationSpeed = 180f;

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        transform.localRotation *= Quaternion.Euler(0f, 0f, -rotationSpeed * Time.deltaTime);
    }

    #endregion
}
