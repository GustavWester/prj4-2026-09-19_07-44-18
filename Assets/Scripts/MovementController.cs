using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simpel movement controller til top-down 2D bullet hell.
/// Håndterer kun bevægelse: base speed, optional sprint og optional dash.
/// Understøtter både WASD og piletaster.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    [Header("Base Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Sprint (optional)")]
    [SerializeField] private bool sprintEnabled = false;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Dash (optional)")]
    [SerializeField] private bool dashEnabled = false;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    private Rigidbody2D rb;

    SpriteRenderer spriteRenderer;

    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
    }

    private void Update()
    {
        ReadInput();
        animator.SetBool("Move", moveInput.sqrMagnitude > 0f);
        HandleDashInput();
        TickTimers();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = lastMoveDirection * dashSpeed;
        }
        else
        {
            float currentSpeed = speed;

            if (sprintEnabled && Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            {
                currentSpeed *= sprintMultiplier;
            }

            rb.linearVelocity = moveInput * currentSpeed;
        }
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        // Læser både WASD og piletaster manuelt, da vi ikke bruger
        // en genereret Input Actions-asset her.
        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        

        if (x != 0f) spriteRenderer.flipX = x < 0f; //går man til venstre -1 til højre +1, hvis man går til venstre er flip true, til højre falsk

        moveInput = new Vector2(x, y).normalized;

        if (moveInput.sqrMagnitude > 0f)
        {
            lastMoveDirection = moveInput;
        }
    }

    private void HandleDashInput()
    {
        if (!dashEnabled || isDashing || Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }
    }

    private void TickTimers()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }
}
