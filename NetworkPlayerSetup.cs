using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class NetworkPlayerSetup : NetworkBehaviour
{
    private PlayerController controllerScript;
    private Camera playerCamera;
    private CollideAndSlide motor;
    public void Awake()
    {
        controllerScript = GetComponent<PlayerController>();
        playerCamera = GetComponentInChildren<Camera>();
        motor = GetComponent<CollideAndSlide>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if(controllerScript) controllerScript.enabled = false;
            if(playerCamera) playerCamera.enabled = false;
            if(motor) motor.enabled = false;
        }
        else
        {
            if(controllerScript) controllerScript.enabled = true;
            if(playerCamera) playerCamera.enabled = true;
            if(motor) motor.enabled = true;

            MeshRenderer[] myRenderers = GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in myRenderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }
}
