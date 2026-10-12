using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TruckController : MonoBehaviour
{
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight;
    public float maxMotorTorque = 2000f;
    public float maxSteeringAngle = 30f;

    public float steerInput;
    public float throttleInput;

    [Header("Braking")]
    [Tooltip("Maximum service brake torque per wheel (Nm).")]
    public float maxBrakeTorque = 12000f;
    [Tooltip("Handbrake torque per rear wheel (Nm).")]
    public float handbrakeTorque = 25000f;
    [Tooltip("How fast the brakes ramp up when pressed (Nm/s).")]
    public float brakeResponseRate = 40000f;
    [Tooltip("Auto-brake torque applied when no throttle is held (Nm).")]
    public float coastBrakeTorque = 1500f;

    [Header("Road-vehicle limits")]
    [Tooltip("Governed top speed in km/h.")]
    public float speedLimitKmh = 90f;
    [Tooltip("Steering is limited so lateral acceleration stays below this (m/s^2).")]
    public float maxLateralAccel = 4f;
    [Tooltip("Distance between front and rear axles (m).")]
    public float wheelbase = 3.09f;

    [Header("Wheel visuals (optional)")]
    public Transform frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh;

    [Header("Live readout")]
    public float forwardSpeedKmh;

    [Header("Engine torque curve")]
    [Tooltip("Normalised torque (0-1) versus normalised RPM (0=idle, 1=redline). Peaks in the mid-range.")]
    public AnimationCurve torqueCurve = BuildDefaultTorqueCurve();

    [Header("Gearbox")]
    [Tooltip("Gear ratios from first to top gear. 0 is reverse.")]
    public float[] gearRatios = new float[] { -3.5f, 4.2f, 2.6f, 1.7f, 1.2f, 1.0f };
    [Tooltip("Final drive ratio (differential).")]
    public float finalDriveRatio = 3.7f;
    [Tooltip("RPM at which the automatic gearbox upshifts.")]
    public float upshiftRpm = 2200f;
    [Tooltip("RPM at which the automatic gearbox downshifts.")]
    public float downshiftRpm = 1100f;
    [Tooltip("Time (s) during which motorTorque is zero during a shift.")]
    public float shiftTime = 0.3f;

    [Header("Traction")]
    [Tooltip("Slip threshold above which motor torque is reduced. 0 = never, 1 = heavily.")]
    public float maxSlipRatio = 0.35f;
    [Tooltip("How aggressively torque is cut when slipping (0-1). 1 = full cut, 0 = no cut.")]
    public float tractionControlStrength = 0.7f;
    [Tooltip("Minimum torque multiplier when slipping so the truck can still crawl.")]
    public float minTractionMultiplier = 0.25f;
    [Tooltip("Maximum lateral slip allowed before steering authority is reduced.")]
    public float maxLateralSlip = 0.4f;

    [Header("Engine inertia")]
    [Tooltip("How fast engine RPM tracks wheel RPM. Higher = more responsive.")]
    public float engineResponse = 8f;

    [Header("Engine / RPM readout")]
    public float idleRpm = 800f;
    public float redlineRpm = 2500f;
    public float maxSpeedKmh = 90f;
    public float currentRpm;

    Rigidbody _rb;
    WheelCollider[] _cols;
    Transform[] _meshes;
    Quaternion[] _rotOffset;
    Vector3[] _pivotOffset;

    float _currentBrakeTorque;
    bool _touchBrake;

    int _currentGear = 1;
    float _shiftTimer;
    float _engineRpm;

    WheelHit _hitFL, _hitFR, _hitRL, _hitRR;
    float _tractionMultiplier = 1f;
    float _currentSteerAngle;

    static AnimationCurve BuildDefaultTorqueCurve()
    {
        var curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f, 0.55f));
        curve.AddKey(new Keyframe(0.5f, 1.0f));
        curve.AddKey(new Keyframe(1f, 0.6f));
        for (int i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0f);
        return curve;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _cols = new[] { frontLeft, frontRight, rearLeft, rearRight };
        _meshes = new[] { frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh };
        _rotOffset = new Quaternion[4];
        _pivotOffset = new Vector3[4];

        _engineRpm = idleRpm;

        for (int i = 0; i < 4; i++)
        {
            if (_cols[i] == null || _meshes[i] == null) continue;
            Quaternion colRot = _cols[i].transform.rotation;
            _rotOffset[i] = Quaternion.Inverse(colRot) * _meshes[i].rotation;
            Vector3 local = Quaternion.Inverse(colRot) * (_meshes[i].position - _cols[i].transform.position);
            local.y = 0f;
            _pivotOffset[i] = local;
        }
    }

    float ComputeSlip(WheelCollider col, out WheelHit hit)
    {
        hit = new WheelHit();
        if (col == null || !col.GetGroundHit(out hit)) return 0f;
        float longitudinal = Mathf.Abs(hit.forwardSlip);
        float lateral = Mathf.Abs(hit.sidewaysSlip);
        return Mathf.Max(longitudinal, lateral);
    }

    void FixedUpdate()
    {
        float effectiveThrottle = Mathf.Abs(throttleInput) > 0.01f ? throttleInput : Input.GetAxis("Vertical");

        forwardSpeedKmh = Vector3.Dot(_rb.linearVelocity, transform.forward) * 3.6f;

        // --- RPM tracking ---
        // Wheel RPM derived from forward speed and wheel radius.
        float wheelRadius = frontLeft.radius;
        float wheelRpm = Mathf.Abs(forwardSpeedKmh) / 3.6f / (2f * Mathf.PI * wheelRadius) * 60f;

        // Engine RPM = wheel RPM × gear ratio × final drive.
        float gearRatio = gearRatios[Mathf.Clamp(_currentGear, 0, gearRatios.Length - 1)];
        float targetRpm = Mathf.Max(idleRpm, wheelRpm * Mathf.Abs(gearRatio) * finalDriveRatio);
        _engineRpm = Mathf.Lerp(_engineRpm, targetRpm, engineResponse * Time.fixedDeltaTime);

        currentRpm = _engineRpm;

        // --- Automatic gearbox ---
        if (_shiftTimer > 0f)
        {
            _shiftTimer -= Time.fixedDeltaTime;
        }
        else
        {
            // Shift based on throttle demand only when moving in the same direction as the gear
            if (Mathf.Abs(forwardSpeedKmh) > 3f)
            {
                if (_engineRpm > upshiftRpm && _currentGear < gearRatios.Length - 1)
                {
                    _currentGear++;
                    _shiftTimer = shiftTime;
                }
                else if (_engineRpm < downshiftRpm && _currentGear > 1)
                {
                    _currentGear--;
                    _shiftTimer = shiftTime;
                }
            }

            // Handle reverse / neutral based on throttle input direction
            if (effectiveThrottle < -0.1f && Mathf.Abs(forwardSpeedKmh) < 3f)
                _currentGear = 0;
            else if (effectiveThrottle > 0.1f && _currentGear == 0 && Mathf.Abs(forwardSpeedKmh) < 3f)
                _currentGear = 1;
        }

        // --- Engine torque from curve ---
        float rpmNorm = Mathf.InverseLerp(idleRpm, redlineRpm, _engineRpm);
        float torqueMultiplier = torqueCurve.Evaluate(rpmNorm);
        float availableTorque = maxMotorTorque * torqueMultiplier;

        // Gear multiplication.
        float finalTorque = _shiftTimer > 0f ? 0f : availableTorque * Mathf.Abs(gearRatio) * finalDriveRatio * effectiveThrottle;

        // Speed governor.
        if (Mathf.Abs(forwardSpeedKmh) >= speedLimitKmh && Mathf.Sign(finalTorque) == Mathf.Sign(forwardSpeedKmh))
            finalTorque = 0f;

        float motor = finalTorque;

        // --- Traction control ---
        float slipFL = ComputeSlip(frontLeft,  out _hitFL);
        float slipFR = ComputeSlip(frontRight, out _hitFR);
        float slipRL = ComputeSlip(rearLeft,   out _hitRL);
        float slipRR = ComputeSlip(rearRight,  out _hitRR);

        float maxSlip = Mathf.Max(Mathf.Max(slipFL, slipFR), Mathf.Max(slipRL, slipRR));

        float targetMultiplier = 1f;
        if (maxSlip > maxSlipRatio)
        {
            float excess = Mathf.InverseLerp(maxSlipRatio, maxSlipRatio * 2f, maxSlip);
            targetMultiplier = Mathf.Lerp(1f, minTractionMultiplier, excess * tractionControlStrength);
        }
        _tractionMultiplier = Mathf.MoveTowards(_tractionMultiplier, targetMultiplier, 3f * Time.fixedDeltaTime);

        motor *= _tractionMultiplier;

        float speed = Mathf.Abs(forwardSpeedKmh) / 3.6f;
        float lockAngle = maxSteeringAngle;
        if (speed > 0.1f)
            lockAngle = Mathf.Min(maxSteeringAngle,
                Mathf.Atan(maxLateralAccel * wheelbase / (speed * speed)) * Mathf.Rad2Deg);
        float effectiveSteer = Mathf.Abs(steerInput) > 0.01f ? steerInput : Input.GetAxis("Horizontal");
        float steering = lockAngle * effectiveSteer;
        _currentSteerAngle = steering;

        frontLeft.steerAngle = steering;
        frontRight.steerAngle = steering;

        // motorTorque is scaled by _tractionMultiplier above.
        frontLeft.motorTorque = motor;
        frontRight.motorTorque = motor;
        rearLeft.motorTorque = motor * 0.5f;  // rear gets half, front-biased
        rearRight.motorTorque = motor * 0.5f;

        float targetBrake = 0f;
        bool handbrakePressed = Input.GetKey(KeyCode.Space) || _touchBrake;

        if (handbrakePressed)
            targetBrake = maxBrakeTorque;
        else if (Mathf.Abs(effectiveThrottle) < 0.01f && Mathf.Abs(forwardSpeedKmh) > 2f)
            targetBrake = coastBrakeTorque;

        _currentBrakeTorque = Mathf.MoveTowards(
            _currentBrakeTorque, targetBrake, brakeResponseRate * Time.fixedDeltaTime);

        frontLeft.brakeTorque = _currentBrakeTorque;
        frontRight.brakeTorque = _currentBrakeTorque;
        float rearTorque = handbrakePressed ? handbrakeTorque : _currentBrakeTorque;
        rearLeft.brakeTorque = rearTorque;
        rearRight.brakeTorque = rearTorque;
    }

    void LateUpdate()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_cols[i] == null || _meshes[i] == null) continue;

            _cols[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            Quaternion steerFrame = _cols[i].transform.rotation * Quaternion.Euler(0f, _cols[i].steerAngle, 0f);
            _meshes[i].SetPositionAndRotation(pos + steerFrame * _pivotOffset[i], rot * _rotOffset[i]);
        }
    }

    // --- Read-only state for interior / HUD ---
    public int CurrentGear => _currentGear;
    public float CurrentSteerAngle => _currentSteerAngle;

    public void SteerLeftDown()    { steerInput = -1f; }
    public void SteerRightDown()   { steerInput = 1f; }
    public void SteerRelease()     { steerInput = 0f; }
    public void ThrottleDown()     { throttleInput = 1f; }
    public void ReverseDown()      { throttleInput = -1f; }
    public void ThrottleRelease()  { throttleInput = 0f; }

    public void BrakeDown() { _touchBrake = true; }
    public void BrakeUp()   { _touchBrake = false; }

    public void ShiftUp()   { if (_currentGear < gearRatios.Length - 1) { _currentGear++; _shiftTimer = shiftTime; } }
    public void ShiftDown() { if (_currentGear > 1) { _currentGear--; _shiftTimer = shiftTime; } }
}