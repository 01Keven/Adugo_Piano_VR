using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

// Keeps simulated controllers at fixed positions relative to the player's view.
[DefaultExecutionOrder(10000)]
public class SimulatorControllersFollowView : MonoBehaviour
{
    [SerializeField] private Transform playerRig;
    [SerializeField] private Vector3 leftOffset = new Vector3(-0.12f, -0.12f, 0.5f);
    [SerializeField] private Vector3 rightOffset = new Vector3(0.12f, -0.12f, 0.5f);

    private Transform playerCamera;
    private Transform viewAnchor;
    private Transform leftController;
    private Transform rightController;

    private void Start()
    {
        if (playerRig == null)
            return;

        Camera rigCamera = playerRig.GetComponentInChildren<Camera>(true);
        if (rigCamera == null)
            return;

        playerCamera = rigCamera.transform;
        Transform left = null;
        Transform right = null;
        foreach (Transform child in playerRig.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Left Controller") left = child;
            if (child.name == "Right Controller") right = child;
        }

        if (left == null || right == null)
        {
            Debug.LogWarning("Could not find both simulated controllers in the player rig.", this);
            return;
        }

        viewAnchor = new GameObject("Simulated Controllers - View Anchor").transform;
        viewAnchor.SetParent(playerRig, false);
        left.SetParent(viewAnchor, false);
        right.SetParent(viewAnchor, false);
        leftController = left;
        rightController = right;
        ConfigureStraightRays(left);
        ConfigureStraightRays(right);
        FollowView();
    }

    private static void ConfigureStraightRays(Transform controller)
    {
        foreach (XRRayInteractor ray in controller.GetComponentsInChildren<XRRayInteractor>(true))
        {
            ray.lineType = XRRayInteractor.LineType.StraightLine;
            ray.rayOriginTransform = controller;
        }
        foreach (CurveInteractionCaster caster in controller.GetComponentsInChildren<CurveInteractionCaster>(true))
        {
            caster.castOrigin = controller;
            caster.enableStabilization = false;
        }
        foreach (XRInteractorLineVisual line in controller.GetComponentsInChildren<XRInteractorLineVisual>(true))
        {
            line.smoothMovement = false;
            line.lineBendRatio = 1f;
            line.overrideInteractorLineOrigin = true;
            line.lineOriginTransform = controller;
            line.snapEndpointIfAvailable = false;
        }
        foreach (CurveVisualController line in controller.GetComponentsInChildren<CurveVisualController>(true))
        {
            line.overrideLineOrigin = true;
            line.lineOriginTransform = controller;
            line.snapToSelectedAttachIfAvailable = false;
            line.snapToSnapVolumeIfAvailable = false;
            Straighten(line.noValidHitProperties);
            Straighten(line.hoverHitProperties);
            Straighten(line.selectHitProperties);
            Straighten(line.uiHitProperties);
            Straighten(line.uiPressHitProperties);
        }
    }

    private static void Straighten(LineProperties properties)
    {
        if (properties == null) return;
        properties.smoothlyCurveLine = false;
        properties.lineBendRatio = 1f;
    }

    private void OnEnable()
    {
        Application.onBeforeRender += FollowView;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= FollowView;
    }

    private void LateUpdate()
    {
        FollowView();
    }

    private void FollowView()
    {
        if (viewAnchor != null && playerCamera != null)
        {
            viewAnchor.SetPositionAndRotation(playerCamera.position, playerCamera.rotation);
            // Fix the simulated pose to the view so mouse/WASD cannot leave residual drift.
            if (leftController != null)
            {
                leftController.localPosition = leftOffset;
                leftController.localRotation = Quaternion.identity;
            }
            if (rightController != null)
            {
                rightController.localPosition = rightOffset;
                rightController.localRotation = Quaternion.identity;
            }
        }
    }
}
