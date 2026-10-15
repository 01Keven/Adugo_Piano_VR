using UnityEngine;

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
        FollowView();
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
            // Tracking input can translate these transforms every frame (including WASD).
            // Restore only their positions so simulated aiming and selection still work.
            if (leftController != null)
                leftController.localPosition = leftOffset;
            if (rightController != null)
                rightController.localPosition = rightOffset;
        }
    }
}
