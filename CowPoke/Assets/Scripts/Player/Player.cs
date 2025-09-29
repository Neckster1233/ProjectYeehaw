using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickPlayerExample : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 8f;
    public VariableJoystick variableJoystick;
    public Rigidbody rb;

    [Header("Aiming / Firing")]
    public VariableJoystick aimJoystick; // second joystick to control aim
    [Tooltip("Particle system used to visually represent projectiles. It should use World simulation space.")]
    public ParticleSystem projectileParticleSystem;
    [Tooltip("Initial speed applied to emitted particles (in world units/sec)")]
    public float projectileSpeed = 15f;
    [Tooltip("How many particles to emit per shot")]
    public int particlesPerShot = 1;
    public float fireRate = 6f; // shots per second (auto-fire)

    // internal
    private Vector2 moveInput = Vector2.zero;
    private Vector3 aimDirection = Vector3.forward;
    private float fireTimer = 0f;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        // Enable interpolation to smooth out movement between FixedUpdate and LateUpdate (camera)
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        ReadInput();
        UpdateAim();
        HandleAutoFire();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    // Read movement from joystick and keyboard. Combine and normalize so diagonal speed isn't faster.
    void ReadInput()
    {
        float h = 0f;
        float v = 0f;

        if (variableJoystick != null)
        {
            h += variableJoystick.Horizontal;
            v += variableJoystick.Vertical;
        }

        // keyboard / WASD fallback (use GetAxisRaw for instant 8-dir feel)
        h += Input.GetAxisRaw("Horizontal");
        v += Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(h, v);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
    }

    // Aim with a second joystick; fall back to movement direction.
    void UpdateAim()
    {
        Vector3 newAim = Vector3.zero;

        if (aimJoystick != null)
        {
            Vector2 a = new Vector2(aimJoystick.Horizontal, aimJoystick.Vertical);
            if (a.sqrMagnitude > 0.0001f)
            {
                newAim = new Vector3(a.x, 0f, a.y).normalized;
            }
        }

        // if aim joystick not available or too small, fall back to movement direction
        if (newAim.sqrMagnitude < 0.001f)
        {
            if (moveInput.sqrMagnitude > 0.001f)
            {
                newAim = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }
            else
            {
                newAim = transform.forward;
            }
        }

        aimDirection = newAim;

        // smoothly face aim direction (instant rotation can feel too snappy)
        Vector3 face = new Vector3(aimDirection.x, 0f, aimDirection.z);
        if (face.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 0.35f);
        }
    }

    void ApplyMovement()
    {
        // convert 2D moveInput to 3D XZ space
        Vector3 velocity = new Vector3(moveInput.x, 0f, moveInput.y) * speed;

        if (rb != null)
        {
            // For kinematic rigidbodies, MovePosition produces smoother results when camera interpolates.
            if (rb.isKinematic)
            {
                rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
            }
            else
            {
                // set velocity directly for immediate, arcade-like movement (Vampire Survivors style)
                rb.linearVelocity = velocity;
            }
        }
        else
        {
            // fallback: move transform (non-physics)
            transform.position += velocity * Time.fixedDeltaTime;
        }
    }

    void HandleAutoFire()
    {
        if (projectileParticleSystem == null || fireRate <= 0f) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile(aimDirection);
            fireTimer = 1f / fireRate;
        }
    }

    void FireProjectile(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        if (projectileParticleSystem == null) return;

        // Emit particles with an initial velocity in world space.
        var emitParams = new ParticleSystem.EmitParams();
        emitParams.position = transform.position + dir.normalized * 0.6f;
        emitParams.velocity = dir.normalized * projectileSpeed;
        // applyShapeToPosition ensures the particle appears at that position even if the system has a shape.
        emitParams.applyShapeToPosition = true;
        projectileParticleSystem.Emit(emitParams, particlesPerShot);
    }
}