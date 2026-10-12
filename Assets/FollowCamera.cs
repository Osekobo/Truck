using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    public enum CameraMode { Follow, Static, Hood, Wheel, Bumper, Interior }

    [Tooltip("Object to follow. If empty, the TruckController in the scene is used.")]
    public Transform target;

    [Header("Mode")]
    public CameraMode currentMode = CameraMode.Follow;
    public KeyCode toggleKey = KeyCode.C;

    [Header("Static view (world space)")]
    public Vector3 staticPosition = new Vector3(0f, 15f, -25f);
    public Vector3 staticLookAt = new Vector3(0f, 0f, 0f);

    [Header("Mode transition")]
    public float transitionSmoothTime = 0.7f;
    public float transitionDuration = 1.0f;

    [Header("Hood/Bumper framing (metres, in the target's local frame)")]
    public Vector3 hoodOffset = new Vector3(0f, 1.6f, 0.6f);
    public Vector3 bumperOffset = new Vector3(0f, 0.5f, 2.6f);

    [Header("Wheel framing")]
    [Tooltip("Transform of the wheel to look at. Drag FL_Collider (or any wheel) here.")]
    public Transform wheelLookTarget;
    [Tooltip("Camera position offset from the wheel, in the target's local frame. X negative = left side of the truck.")]
    public Vector3 wheelOffset = new Vector3(-1.0f, 0.35f, 0.2f);

    [Header("Follow framing")]
    public Vector3 offset = new Vector3(0f, 4.5f, -13f);
    public float lookAtHeight = 2.2f;
    public float minHeight = 1.0f;

    [Header("Interior framing")]
    [Tooltip("Driver's eye position, in the target's local frame (metres).")]
    public Vector3 interiorOffset = new Vector3(-0.9f, 2.05f, 0.55f);
    [Tooltip("Extra local pitch (deg) applied when looking out of the cab. Negative looks down.")]
    public float interiorPitch = -4f;
    [Tooltip("Max yaw (deg) the view swings to either side at full steering lock.")]
    public float interiorSteerYaw = 15f;
    [Tooltip("Enable manual mouse/drag look-around while in Interior mode.")]
    public bool allowInteriorLook = true;
    [Tooltip("Manual look-around sensitivity for mouse/touch drag (deg per pixel-ish).")]
    public float interiorLookSensitivity = 0.3f;
    [Tooltip("Seconds after the last manual look input before the view eases back to centre.")]
    public float interiorLookReturnDelay = 1.5f;
    [Tooltip("Speed (deg/sec) at which manual look eases back to centre.")]
    public float interiorLookReturnSpeed = 90f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.12f;
    public float rotationSharpness = 6f;

    [Header("Orbit (Follow mode)")]
    public bool allowOrbit = true;
    public float orbitSensitivity = 0.3f;
    public float orbitReturnDelay = 3f;
    public float orbitReturnSpeed = 60f;

    Vector3 _velocity;
    float _cameraYaw = 0f;
    float _interiorLookYaw = 0f;
    float _lastInteriorLookTime = -999f;
    float _lastOrbitInputTime = -999f;
    float _modeSwitchTime = -999f;
    TruckController _truck;

    void Reset() { AutoFindTarget(); }
    void Start() { AutoFindTarget(); SnapToTarget(); }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) ToggleCameraMode();

        if (allowOrbit && currentMode == CameraMode.Follow)
        {
            bool hadInput = false;

            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved)
                {
                    _cameraYaw += t.deltaPosition.x * orbitSensitivity;
                    hadInput = true;
                }
            }

            #if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                _cameraYaw += Input.GetAxis("Mouse X") * orbitSensitivity * 5f;
                hadInput = true;
            }
            #endif

            if (hadInput) _lastOrbitInputTime = Time.time;
        }

        if (allowInteriorLook && currentMode == CameraMode.Interior)
        {
            bool hadInput = false;

            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved)
                {
                    _interiorLookYaw += t.deltaPosition.x * interiorLookSensitivity;
                    hadInput = true;
                }
            }

            #if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                _interiorLookYaw += Input.GetAxis("Mouse X") * interiorLookSensitivity * 5f;
                hadInput = true;
            }
            #endif

            if (hadInput) _lastInteriorLookTime = Time.time;
        }
    }

    public void ToggleCameraMode()
    {
        currentMode = (CameraMode)(((int)currentMode + 1) % System.Enum.GetValues(typeof(CameraMode)).Length);
        _modeSwitchTime = Time.time;
    }

    void AutoFindTarget()
    {
        var truck = FindFirstObjectByType<TruckController>();
        if (target == null && truck != null) target = truck.transform;
        if (_truck == null) _truck = truck;
    }

    Quaternion HeadingFrame()
    {
        return Quaternion.Euler(0f, target.eulerAngles.y, 0f);
    }

    Vector3 InteriorLookDir()
    {
        float steerNorm = 0f;
        if (_truck != null && _truck.maxSteeringAngle > 0.0001f)
            steerNorm = Mathf.Clamp(_truck.CurrentSteerAngle / _truck.maxSteeringAngle, -1f, 1f);

        float yaw = steerNorm * interiorSteerYaw + _interiorLookYaw;
        return Quaternion.AngleAxis(yaw, target.up) * (Quaternion.AngleAxis(interiorPitch, target.right) * target.forward);
    }

    Vector3 DesiredPosition()
    {
        Quaternion yawFrame = Quaternion.Euler(0f, target.eulerAngles.y + _cameraYaw, 0f);
        var p = target.position + yawFrame * offset;
        p.y = Mathf.Max(p.y, minHeight);
        return p;
    }

    public void SnapToTarget()
    {
        if (currentMode == CameraMode.Static)
        {
            transform.position = staticPosition;
            var dir = staticLookAt - staticPosition;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            _velocity = Vector3.zero;
            return;
        }
        if (target == null) return;
        if (currentMode == CameraMode.Interior)
        {
            transform.position = target.TransformPoint(interiorOffset);
            transform.rotation = Quaternion.LookRotation(InteriorLookDir(), Vector3.up);
            _velocity = Vector3.zero;
            return;
        }
        transform.position = DesiredPosition();
        transform.rotation = Quaternion.LookRotation(
            target.position + Vector3.up * lookAtHeight - transform.position, Vector3.up);
        _velocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (currentMode == CameraMode.Static)
        {
            transform.position = Vector3.Lerp(transform.position, staticPosition, 4f * Time.deltaTime);
            var staticDir = staticLookAt - transform.position;
            if (staticDir.sqrMagnitude > 0.0001f)
            {
                var staticWanted = Quaternion.LookRotation(staticDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, staticWanted, 4f * Time.deltaTime);
            }
            return;
        }

        if (target == null) return;

        if (currentMode == CameraMode.Interior)
        {
            if (allowInteriorLook && Time.time - _lastInteriorLookTime > interiorLookReturnDelay)
                _interiorLookYaw = Mathf.MoveTowards(_interiorLookYaw, 0f, interiorLookReturnSpeed * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, target.TransformPoint(interiorOffset), 25f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(InteriorLookDir(), Vector3.up), 25f * Time.deltaTime);
            return;
        }

        if (currentMode == CameraMode.Hood)
        {
            transform.position = Vector3.Lerp(transform.position, target.TransformPoint(hoodOffset), 25f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.forward, Vector3.up), 25f * Time.deltaTime);
            return;
        }

        if (currentMode == CameraMode.Bumper)
        {
            transform.position = Vector3.Lerp(transform.position, target.TransformPoint(bumperOffset), 25f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.forward, Vector3.up), 25f * Time.deltaTime);
            return;
        }

        if (currentMode == CameraMode.Wheel)
        {
            Vector3 wheelWorldPos = wheelLookTarget != null
                ? wheelLookTarget.position
                : target.position + new Vector3(-0.895f, 0.617f, 1.251f);

            Vector3 cameraWorldPos = wheelWorldPos + target.TransformDirection(wheelOffset);

            transform.position = Vector3.Lerp(transform.position, cameraWorldPos, 20f * Time.deltaTime);

            Vector3 wheelLook = wheelWorldPos - transform.position;
            if (wheelLook.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(wheelLook, Vector3.up), 20f * Time.deltaTime);
            }
            return;
        }

        bool transitioning = Time.time - _modeSwitchTime < transitionDuration;
        float posSmooth = transitioning ? transitionSmoothTime : positionSmoothTime;
        float rotSharp = transitioning ? rotationSharpness * 0.4f : rotationSharpness;

        if (allowOrbit && Time.time - _lastOrbitInputTime > orbitReturnDelay)
        {
            _cameraYaw = Mathf.MoveTowards(_cameraYaw, 0f, orbitReturnSpeed * Time.deltaTime);
        }

        Vector3 desiredPos = DesiredPosition();
        Vector3 lookPoint = target.position + Vector3.up * lookAtHeight;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, posSmooth);

        var lookDir = lookPoint - transform.position;
        if (lookDir.sqrMagnitude < 0.0001f) return;
        var wanted = Quaternion.LookRotation(lookDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, wanted, 1f - Mathf.Exp(-rotSharp * Time.deltaTime));
    }
}