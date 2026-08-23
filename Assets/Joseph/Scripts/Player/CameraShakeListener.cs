using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeListener : MonoBehaviour
{
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        FishingManager.OnCameraShakeRequested += HandleCameraShake;
    }

    private void OnDisable()
    {
        FishingManager.OnCameraShakeRequested -= HandleCameraShake;
    }

    private void HandleCameraShake(float duration, float magnitude)
    {
        if (impulseSource == null) return;

        Vector3 defaultVelocity = impulseSource.DefaultVelocity;
        impulseSource.GenerateImpulseWithForce(magnitude);
    }
}