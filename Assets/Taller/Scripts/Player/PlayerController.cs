using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    // ─── Events ───────────────────────────────────────────────────────────────
    public event Action OnJumpPerformed;
    public event Action OnLandPerformed;
    public static event Action OnHitPerformed;

    [Header("Movimiento")]
    [SerializeField, Range(1f, 20f)]
    private float moveSpeed = 8f;

    [SerializeField, Range(0f, 0.2f)]
    private float movementSmoothing = 0.05f;

    [SerializeField, Range(0f, 10f)]
    private float dashBurst = 5f;

    [Header("Salto")]
    [SerializeField, Range(5f, 30f)]
    private float jumpForce = 16f;

    [SerializeField, Range(0f, 0.5f)]
    private float coyoteTime = 0.15f;

    [SerializeField, Range(0f, 0.5f)]
    private float jumpBufferTime = 0.1f;

    [SerializeField, Range(1f, 5f)]
    private float fallExtraGravity = 2.5f;

    [SerializeField, Range(1f, 5f)]
    private float lowJumpExtraGravity = 2f;

    [Header("Escaleras")]
    [SerializeField]
    private LayerMask ladderLayer;

    [SerializeField, Range(1f, 10f)]
    private float climbSpeed = 5f;

    [Header("One Way Platforms")]
    [SerializeField]
    private LayerMask oneWayPlatformLayer;

    [SerializeField]
    private float dropDownDuration = 0.5f;

    [Header("Combate & Daño")]
    [SerializeField]
    private LayerMask damageLayer;

    [SerializeField, Range(0f, 2f)]
    private float damageCooldown = 1f;

    [SerializeField]
    private Vector2 knockbackForce = new Vector2(10f, 8f);

    [SerializeField]
    private float hitStunDuration = 0.3f;

    [Header("Detección de Suelo")]
    [SerializeField]
    private LayerMask whatIsGround;

    [SerializeField]
    private Transform groundCheck;

    [SerializeField]
    private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);

    [Header("Debug")]
    [SerializeField]
    private bool showDebugGizmos = true;

    // ─── Components ───────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private CapsuleCollider2D _col;

    [SerializeField]
    private Animator _anim;

    // ─── State ────────────────────────────────────────────────────────────────
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _isClimbing;
    private bool _isDucking;
    private bool _isHit;
    private bool _isBouncing;
    private bool _isDropping;
    private bool _dropDownRequested;
    private bool _facingRight = true;
    private float _velocityXSmoothing;
    private float _originalGravity;

    private bool _isJumping;
    private float _maxAllowedJumpVY;

    // ─── Buffers ───────────────────────────────────────────────────────
    private ContactFilter2D _groundFilter;
    private ContactFilter2D _oneWayFilter;
    private readonly Collider2D[] _groundHits = new Collider2D[5];
    private readonly Collider2D[] _oneWayHits = new Collider2D[10];
    private readonly Collider2D[] _ignoredPlatforms = new Collider2D[10];
    private int _ignoredPlatformsCount;

    // ─── Timers ────────────────────────────────────────────────────────────────
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _lastHitTime;
    private float _dropDownTimer;
    private float _hitStunTimer;

    // ─── Hashes Animator ───────────────────────────────────────────────────────
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimVerticalVel = Animator.StringToHash("VerticalVelocity");
    private static readonly int AnimIsClimbing = Animator.StringToHash("IsClimbing");
    private static readonly int AnimClimbSpeed = Animator.StringToHash("ClimbSpeed");
    private static readonly int AnimIsDucking = Animator.StringToHash("IsDucking");
    private static readonly int AnimHitTrigger = Animator.StringToHash("Hit");
    private static readonly int AnimIsHit = Animator.StringToHash("IsHit");

    public Vector2 CurrentVelocity => _rb != null ? _rb.linearVelocity : Vector2.zero;
    public bool IsGrounded => _isGrounded;

    // ═══════════════════════════════════════════════════════════════════════════
    #region Setup
    // ═══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();
        _originalGravity = _rb.gravityScale;

        if (_anim == null)
            _anim = GetComponentInChildren<Animator>();

        if (_col.sharedMaterial == null || _col.sharedMaterial.friction != 0f)
        {
            _col.sharedMaterial = new PhysicsMaterial2D("NoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        _groundFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = whatIsGround,
            useTriggers = false
        };

        _oneWayFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = oneWayPlatformLayer,
            useTriggers = false
        };
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Main Loop
    // ═══════════════════════════════════════════════════════════════════════════

    private void FixedUpdate()
    {
        if (_isJumping)
        {
            if (_rb.linearVelocity.y <= 0f)
                _isJumping = false;
            else if (_rb.linearVelocity.y > _maxAllowedJumpVY)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _maxAllowedJumpVY);
        }

        if (_dropDownRequested)
        {
            _dropDownRequested = false;
            StartDropDown();
        }

        UpdateTimers();
        CheckGround();
        CheckLadder();

        if (_isHit)
        {
            ApplyVariableJumpGravity();
            UpdateAnimations();
            return;
        }

        HandleJumpInitiation();
        ApplyVariableJumpGravity();
        HandleMovement();
        HandleClimbingPhysics();
        UpdateAnimations();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Input
    // ═══════════════════════════════════════════════════════════════════════════

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (!value.isPressed || _isHit)
            return;

        if (_moveInput.y < -0.5f && _isGrounded && !_isDropping)
        {
            _dropDownRequested = true;
            return;
        }

        _jumpBufferCounter = jumpBufferTime;
    }

    private void ExitLadderByJump()
    {
        _isClimbing = false;
        _rb.gravityScale = _originalGravity;
        PerformJump();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region One Way Platforms
    // ═══════════════════════════════════════════════════════════════════════════

    private void StartDropDown()
    {
        _isDropping = true;
        _dropDownTimer = dropDownDuration;

        int hitCount = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            _oneWayFilter,
            _oneWayHits
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D platform = _oneWayHits[i];
            if (platform == null || _ignoredPlatformsCount >= _ignoredPlatforms.Length)
                continue;
            Physics2D.IgnoreCollision(_col, platform, true);
            _ignoredPlatforms[_ignoredPlatformsCount++] = platform;
        }

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -3f);
    }

    private void RestoreIgnoredPlatforms()
    {
        for (int i = 0; i < _ignoredPlatformsCount; i++)
        {
            if (_ignoredPlatforms[i] != null)
                Physics2D.IgnoreCollision(_col, _ignoredPlatforms[i], false);
            _ignoredPlatforms[i] = null;
        }
        _ignoredPlatformsCount = 0;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Combat
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnCollisionEnter2D(Collision2D collision) => TryTakeDamage(collision.gameObject);

    private void OnTriggerEnter2D(Collider2D collision) => TryTakeDamage(collision.gameObject);

    private void TryTakeDamage(GameObject target)
    {
        if ((damageLayer.value & (1 << target.layer)) == 0)
            return;
        if (Time.time < _lastHitTime + damageCooldown)
            return;
        TakeDamage(target.transform.position);
    }

    private void TakeDamage(Vector3 source)
    {
        _lastHitTime = Time.time;
        if (!_isHit)
        {
            _isHit = true;
            _hitStunTimer = hitStunDuration;
        }

        float dir = Mathf.Sign(transform.position.x - source.x);
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(new Vector2(dir * knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);

        _anim?.SetTrigger(AnimHitTrigger);
        OnHitPerformed?.Invoke();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Trampoline
    // ═══════════════════════════════════════════════════════════════════════════

    public void Bounce(float force)
    {
        _isBouncing = true;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        OnJumpPerformed?.Invoke();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Physics & Logic
    // ═══════════════════════════════════════════════════════════════════════════

    private void UpdateTimers()
    {
        _coyoteTimeCounter = _isGrounded ? coyoteTime : _coyoteTimeCounter - Time.fixedDeltaTime;

        if (_jumpBufferCounter > 0f)
            _jumpBufferCounter -= Time.fixedDeltaTime;

        if (_isDropping)
        {
            _dropDownTimer -= Time.fixedDeltaTime;
            if (_dropDownTimer <= 0f)
            {
                _isDropping = false;
                RestoreIgnoredPlatforms();
            }
        }

        if (_isHit)
        {
            _hitStunTimer -= Time.fixedDeltaTime;
            if (_hitStunTimer <= 0f)
                _isHit = false;
        }
    }

    private void HandleJumpInitiation()
    {
        if (_jumpBufferCounter <= 0f)
            return;

        if (_isClimbing)
        {
            ExitLadderByJump();
            _jumpBufferCounter = 0f;
        }
        else if (_coyoteTimeCounter > 0f && !_isDucking)
        {
            PerformJump();
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;
        }
    }

    private void PerformJump()
    {
        _rb.gravityScale = 1f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        _maxAllowedJumpVY = _rb.linearVelocity.y;
        _isJumping = true;

        OnJumpPerformed?.Invoke();
    }

    private void ApplyVariableJumpGravity()
    {
        if (_isClimbing)
            return;

        float vy = _rb.linearVelocity.y;
        float g = Physics2D.gravity.magnitude;

        if (vy < 0f)
        {
            _rb.AddForce(Vector2.down * g * (fallExtraGravity - 1f), ForceMode2D.Force);
        }
        else if (vy > 0f && !IsJumpHeld() && !_isBouncing)
        {
            _rb.AddForce(Vector2.down * g * (lowJumpExtraGravity - 1f), ForceMode2D.Force);
        }
    }

    private void HandleMovement()
    {
        if (_isGrounded && _moveInput.y < -0.5f && !_isClimbing && !_isDropping)
        {
            _isDucking = true;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }
        _isDucking = false;

        if (_isClimbing)
        {
            _rb.linearVelocity = new Vector2(
                _moveInput.x * (moveSpeed * 0.3f),
                _rb.linearVelocity.y
            );
            return;
        }

        float targetVX = _moveInput.x * moveSpeed;
        float currentVX = _rb.linearVelocity.x;
        bool changingDir =
            (_moveInput.x > 0f && currentVX < -0.1f) || (_moveInput.x < 0f && currentVX > 0.1f);
        bool fromZero = _moveInput.x != 0f && Mathf.Abs(currentVX) < 0.1f;

        if (changingDir || fromZero)
        {
            _velocityXSmoothing = 0f;
            _rb.linearVelocity = new Vector2(
                targetVX + _moveInput.x * dashBurst,
                _rb.linearVelocity.y
            );
        }
        else if (Mathf.Abs(_moveInput.x) < 0.01f)
        {
            _velocityXSmoothing = 0f;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }
        else
        {
            float smoothedX = Mathf.SmoothDamp(
                currentVX,
                targetVX,
                ref _velocityXSmoothing,
                movementSmoothing
            );
            _rb.linearVelocity = new Vector2(smoothedX, _rb.linearVelocity.y);
        }

        if (_moveInput.x > 0f && !_facingRight)
            Flip();
        else if (_moveInput.x < 0f && _facingRight)
            Flip();
    }

    private void CheckLadder()
    {
        bool onLadder = _col.IsTouchingLayers(ladderLayer);

        if (onLadder)
        {
            if (_isClimbing && _isGrounded && _moveInput.y < -0.1f)
            {
                _isClimbing = false;
                _rb.gravityScale = _originalGravity;
                return;
            }
            if (!_isClimbing)
            {
                bool validEntry = _isGrounded
                    ? (_moveInput.y > 0.1f)
                    : (Mathf.Abs(_moveInput.y) > 0.1f);
                if (validEntry)
                    _isClimbing = true;
            }
        }
        else if (_isClimbing)
        {
            _isClimbing = false;
            _rb.gravityScale = _originalGravity;
        }
    }

    private void HandleClimbingPhysics()
    {
        if (!_isClimbing)
            return;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _moveInput.y * climbSpeed);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Ground Check
    // ═══════════════════════════════════════════════════════════════════════════

    private void CheckGround()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = false;

        if (_rb.linearVelocity.y <= 0f)
            _isBouncing = false;
        if (_rb.linearVelocity.y > 0.01f)
            return;

        if (groundCheck == null)
            return;

        _groundFilter.layerMask = _isDropping
            ? (whatIsGround & ~oneWayPlatformLayer)
            : whatIsGround;

        int hitCount = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            _groundFilter,
            _groundHits
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCol = _groundHits[i];
            if (hitCol == null || hitCol.gameObject == gameObject)
                continue;
            if (Physics2D.GetIgnoreCollision(_col, hitCol))
                continue;
            _isGrounded = true;
            break;
        }

        if (!wasGrounded && _isGrounded)
            OnLandPerformed?.Invoke();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Animation & Utils
    // ═══════════════════════════════════════════════════════════════════════════

    private void UpdateAnimations()
    {
        if (_anim == null)
            return;

        _anim.SetFloat(AnimSpeed, Mathf.Abs(_moveInput.x) < 0.01f ? 0f : Mathf.Abs(_moveInput.x));
        _anim.SetBool(AnimIsClimbing, _isClimbing);

        if (_isClimbing)
        {
            _anim.SetFloat(AnimVerticalVel, 0f);
            _anim.SetFloat(AnimClimbSpeed, Mathf.Abs(_moveInput.y));
        }
        else
        {
            float vY = _rb.linearVelocity.y;
            if (_isGrounded || Mathf.Abs(vY) < 0.01f)
                vY = 0f;
            _anim.SetFloat(AnimVerticalVel, vY);
        }

        _anim.SetBool(AnimIsGrounded, _isGrounded);
        _anim.SetBool(AnimIsDucking, _isDucking);
        _anim.SetBool(AnimIsHit, _isHit);
    }

    private bool IsJumpHeld()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
            return true;
        return false;
    }

    private void Flip()
    {
        _facingRight = !_facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || groundCheck == null)
            return;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(
            groundCheck.position,
            new Vector3(groundCheckSize.x, groundCheckSize.y, 0f)
        );
    }

    #endregion
}
