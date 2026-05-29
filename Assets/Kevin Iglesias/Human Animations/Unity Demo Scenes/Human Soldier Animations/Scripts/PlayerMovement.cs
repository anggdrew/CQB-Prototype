using KevinIglesias;
using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;
    public Transform player;
    public Transform camTarget;
    public CinemachineCamera lookCam;
    public CinemachineCamera aimCam;

    public float walkingSpeed = 5f;
    public float runningSpeed = 15f;
    public float turningSpeed = 10f;
    public float gravity = -9.81f;
    Vector3 velocity;
    private bool isAiming;

    public float dashSpeed = 50f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;
    public TrailRenderer dashTrail;

    public Transform groundCheck;
    public float groundRadius = 0.5f;
    public LayerMask groundMask;
    public bool isGrounded;

    public float turnSmoothTime = 0.1f;
    public float turnSmoothSpeed = 1.0f;
    public float lookSensitivity = 0.5f;
    public Vector3 camOffset = new Vector3(5, 7, -5);

    private Vector3 lookCamDefaultPos;
    private Quaternion lookCamDefaultRot;

    private float yaw;
    private float pitch;
    public float minPitch;
    public float maxPitch;

    public float jumpHeight = 1f;
    public float jumpModifier = 1f;

    private PlayerInputActions input;
    private HumanSoldierController soldier;
    private bool firePressed;
    private bool jumpPressed;

    [Header("Bullet")]
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float shot_cooldown = 10f;
    public ParticleSystem muzzleflash;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        soldier = GetComponent<HumanSoldierController>();
        dashTrail = GetComponent<TrailRenderer>();
        dashTrail.emitting = false;
        lookCamDefaultPos = lookCam.transform.localPosition;
        lookCamDefaultRot = lookCam.transform.localRotation;
    }
    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Player.Enable();
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        Vector2 lookInput = input.Player.Look.ReadValue<Vector2>();
        Vector3 moveDirn;

        float targetAngle_H = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + lookCam.transform.eulerAngles.y;
        float angle_H = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle_H, ref turnSmoothSpeed, turnSmoothTime);

        

        camTarget.rotation = Quaternion.Euler(0,yaw,0);


        //lookCam.transform.localPosition = lookCamDefaultPos;
        //lookCam.transform.localRotation = lookCamDefaultRot;

        if (input.Player.Aim.IsPressed()) //Aiming
        {
            if (!isAiming)
                isAiming = true;

            if (input.Player.Aim.WasPressedThisFrame())
            {
                Vector3 forward = camTarget.forward;
                forward.y = 0;
                forward.Normalize();
                transform.rotation = Quaternion.LookRotation(forward);
            }

            aimCam.Priority = 1;
            lookCam.Priority = 0;

            yaw += lookInput.x * lookSensitivity * Time.deltaTime;
            pitch -= lookInput.y * lookSensitivity * Time.deltaTime;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            moveDirn = transform.forward * moveInput.y + transform.right * moveInput.x;

            
        }
        else
        {
            if (isAiming)
                isAiming = false;

            lookCam.Priority = 1;
            aimCam.Priority = 0;

            Vector3 camForward = camTarget.forward;
            Vector3 camRight = camTarget.right;

            camForward.y = 0;
            camRight.y = 0;

            camForward.Normalize();
            camRight.Normalize();

            moveDirn = transform.forward;
        }

        if (direction.magnitude >= 0.05f) //Movement
        {
            if (!isAiming)
            {
                transform.rotation = Quaternion.Euler(0f,angle_H, 0f);
            }

            if (input.Player.Run.IsPressed())
            {
                soldier.movement = SoldierMovement.Run;
                controller.Move(moveDirn.normalized * runningSpeed * Time.deltaTime);
            }
            else
            {
                soldier.movement = SoldierMovement.Walk;
                controller.Move(moveDirn.normalized * walkingSpeed * Time.deltaTime);
            }
        }
        else
        {
            soldier.movement = SoldierMovement.NoMovement;
        }
  
        if (input.Player.Dash.WasPressedThisFrame() && !isDashing && canDash) //Dashing
        {
            StartCoroutine(Dash());
        }

        if (input.Player.Jump.WasPressedThisFrame() && isGrounded) //Jumping
        {
            soldier.action = SoldierAction.Jump;
            velocity.y = Mathf.Sqrt((jumpHeight * 2 * -gravity) * jumpModifier);
        }

        else if (input.Player.Fire.WasPressedThisFrame()) //Shooting
        {
            // Trigger shooting once
            if (soldier.action != SoldierAction.Shoot01)
                soldier.action = SoldierAction.Shoot01;

            BulletSpawner();
            BulletSpawner(0, 5, 0);
            BulletSpawner(0, -5, 0);
        }
        else
            soldier.action = SoldierAction.Nothing;

            velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    void BulletSpawner(float i = 0f, float j = 0f, float k = 0f)
    {
        bulletSpawnPoint.Rotate(i, j, k, Space.Self);

        var bullet_0 = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet_0.GetComponent<Rigidbody>().linearVelocity = bulletSpawnPoint.forward * bulletSpeed;
        muzzleflash.Play();

        bulletSpawnPoint.Rotate(-i, -j, -k, Space.Self);
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        soldier.movement = SoldierMovement.Sprint;
        dashTrail.emitting = true;

        for (int i = 0; i < 15; i++)
        {
            controller.Move(transform.forward * dashSpeed);
            yield return null;
        }
        soldier.movement = SoldierMovement.NoMovement;
        controller.Move(Vector3.zero);
        dashTrail.emitting = false;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
