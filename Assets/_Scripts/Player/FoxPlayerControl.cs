using UnityEngine;
using System.Collections;

public class FoxPlayerControl : MonoBehaviour {

    public float _baseSpeed = 5f;

	public float _walkSpeed = 0.15f;
	public float _runSpeed = 1.0f;
	public float _sprintSpeed = 2.0f;
    public float _glideSpeed = 1.0f;

    public float _glideFallSpeed = -5.0f;

	public float _turnSmoothing = 3.0f;
    public float _airTurnSmoothing = 2.0f;
	public float _aimTurnSmoothing = 15.0f;
	public float _speedDampTime = 0.1f;

    public float _airControl = 0.1f;

	public float _jumpHeight = 5.0f;
    public float _secondJumpHeight = 3.0f;
	public float _jumpCooldown = 0.3f;

    public float _attackCooldown = 0.5f;

    public float _starThrowForce = 10.0f;

    public bool hasRoot = false;

    public Rigidbody projectile_star;

	private float timeToNextJump = 0;
	
	private float speed;

	private Vector3 lastDirection;

	private Animator anim;
	private int speedFloat;
	private int firstJumpBool;
    private int secondJumpBool;
    private int canFirstJumpBool;
    private int canSecondJumpBool;
    private int glideBool;
    private int attackInt;
	private int hFloat;
	private int vFloat;
	private int aimBool;
	private int flyBool;
	private int groundedBool;
	private Transform cameraTransform;
    private Rigidbody rigidBody;

	private float h;
	private float v;

	private bool aim;
	private bool run;
	private bool sprint;
    private bool jump;
    private bool glide;
    private bool attack;

	private bool isMoving;

	private Vector3 colliderBound;

    private bool hasFirstJumped = false;
    private bool hasSecondJumped = false;

    private int currentAttack;
    private float lastAttackTime = 0;
    private float lastStarThrowTime = 0;

	void Awake() {
		anim = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody>();
		cameraTransform = Camera.main.transform;

		speedFloat = Animator.StringToHash("Speed");
		firstJumpBool = Animator.StringToHash("FirstJump");
        secondJumpBool = Animator.StringToHash("SecondJump");
        canFirstJumpBool = Animator.StringToHash("canFirstJump");
        canSecondJumpBool = Animator.StringToHash("canSecondJump");
        glideBool = Animator.StringToHash("Glide");
		hFloat = Animator.StringToHash("H");
		vFloat = Animator.StringToHash("V");
		aimBool = Animator.StringToHash("Aim");
        attackInt = Animator.StringToHash("Attack");
		
        groundedBool = Animator.StringToHash("Grounded");
		colliderBound = GetComponent<CapsuleCollider>().bounds.extents;
	}

	bool IsGrounded() {
		return Physics.Raycast(transform.position, Vector3.down, colliderBound.y);
	}

	void Update() {
		aim = Input.GetButton("Aim");
		h = Input.GetAxis("Horizontal");
		v = Input.GetAxis("Vertical");
        run = false;// Input.GetButton("Run");
		sprint = Input.GetButton("Sprint");
        jump = Input.GetButtonDown("Jump");
        glide = Input.GetButton("Jump");
        attack = Input.GetButtonDown("Attack");
		isMoving = Mathf.Abs(h) > 0.1 || Mathf.Abs(v) > 0.1;
	}

	void FixedUpdate() {
		anim.SetBool(aimBool, IsAiming());
		anim.SetFloat(hFloat, h);
		anim.SetFloat(vFloat, v);
		
		anim.SetBool(groundedBool, IsGrounded());

        if (hasRoot) {
            anim.applyRootMotion = IsGrounded();
        }

        if (IsGliding()) {
            rigidBody.useGravity = false;
        } else {
            rigidBody.useGravity = true;
        }

        CombatManagement();
        MovementManagement(h, v, run, sprint);
        JumpManagement();
	}

    void LateUpdate() {
        if (hasFirstJumped) {
            anim.SetBool(canFirstJumpBool, false);
        } else {
            anim.SetBool(canFirstJumpBool, true);
        }
        if (hasSecondJumped) {
            anim.SetBool(canSecondJumpBool, false);
        } else {
            anim.SetBool(canSecondJumpBool, true);
        }
        if (!IsGrounded()) {
            anim.SetBool(firstJumpBool, true);
            hasFirstJumped = true;
            anim.SetBool(canFirstJumpBool, false);
        } else {
            anim.SetBool(firstJumpBool, false);
            hasFirstJumped = false;
            anim.SetBool(canFirstJumpBool, true);
        }
    }

	void JumpManagement() {
        Vector3 forward = transform.TransformDirection(Vector3.forward) * speed;
        Vector3 forward_constant = transform.TransformDirection(Vector3.forward) * ((speed == 0) ? _runSpeed : speed);
        Vector3 currentVelocity = rigidBody.velocity;
        if (IsGrounded()) { //already jumped and landed
			anim.SetBool(firstJumpBool, false);
            anim.SetBool(secondJumpBool, false);
            anim.SetBool(glideBool, false);
            hasFirstJumped = false;
            hasSecondJumped = false;
		}
        if (jump) {
            if (IsGrounded() && !hasFirstJumped && timeToNextJump <= 0) { //first jump
                anim.SetBool(firstJumpBool, true);
                anim.SetBool(groundedBool, false);
                rigidBody.velocity = new Vector3(currentVelocity.x, _jumpHeight, currentVelocity.z);
                timeToNextJump = _jumpCooldown;
                hasFirstJumped = true;
            } else if (!hasSecondJumped && timeToNextJump <= 0) { //second jump
                anim.SetBool(secondJumpBool, true);
                rigidBody.velocity = new Vector3(forward.x * _baseSpeed, _jumpHeight, forward.z * _baseSpeed);
                timeToNextJump = _jumpCooldown;
                hasSecondJumped = true;
            }
        }
        if (IsGliding()) {
            anim.SetBool(glideBool, true);
            rigidBody.velocity = new Vector3(forward_constant.x * _baseSpeed, _glideFallSpeed, forward_constant.z * _baseSpeed);
        } else {
            anim.SetBool(glideBool, false);
        }
        if (timeToNextJump > 0) {
            timeToNextJump -= Time.deltaTime;
        }
	}

	void MovementManagement(float horizontal, float vertical, bool running, bool sprinting) {
		Rotating(horizontal, vertical);

		if (isMoving) {
			if (Time.time - lastAttackTime < _attackCooldown) {
                speed = 0;
            } else if (sprinting) {
				speed = _sprintSpeed;
			} else if (running) {
				speed = _runSpeed;
			} else {
				speed = _walkSpeed;
			}

			anim.SetFloat(speedFloat, speed, _speedDampTime, Time.deltaTime);
		} else {
			speed = 0f;
			anim.SetFloat(speedFloat, 0f);
		}

        if (hasRoot) {
            rigidBody.AddForce(transform.TransformDirection(Vector3.forward) * speed);
        } else {
            Vector3 forward = transform.TransformDirection(Vector3.forward) * speed;
            Vector3 currentVelocity = rigidBody.velocity;
            if (IsGrounded()) {
                rigidBody.velocity = new Vector3(forward.x * _baseSpeed, currentVelocity.y, forward.z * _baseSpeed);
            } else {
                rigidBody.velocity = new Vector3(((1 / currentVelocity.magnitude) * currentVelocity.x + forward.x * _airControl) * _baseSpeed, currentVelocity.y, ((1 / currentVelocity.magnitude) * currentVelocity.z + forward.z * _airControl) * _baseSpeed);
            }
        }
	}

    void CombatManagement() {
        float attackTime = Time.time - lastAttackTime;
        float throwTime = Time.time - lastStarThrowTime;

        if (attackTime >= _attackCooldown && throwTime >= 0.05f) {
            anim.SetInteger(attackInt, 0);
            currentAttack = 0;
        }

        if (attack && IsGrounded() && !IsAiming()) {
            if (currentAttack == 0 && attackTime >= _attackCooldown) {
                anim.SetInteger(attackInt, 1);
                currentAttack = 1;

                lastAttackTime = Time.time;
            } else if (currentAttack == 1 && Time.time - lastAttackTime >= (1 / 5) * _attackCooldown) {
                anim.SetInteger(attackInt, 2);
                currentAttack = 2;

                lastAttackTime = Time.time;
            } else if (currentAttack == 2 && Time.time - lastAttackTime >= (1 / 5) * _attackCooldown) {
                anim.SetInteger(attackInt, 3);
                currentAttack = 3;

                lastAttackTime = Time.time;
            }
        } else if (attack && (IsAiming() || IsGliding()) && Time.time - lastStarThrowTime >= (1 / 5) * _attackCooldown) {
            Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);

            anim.SetInteger(attackInt, 4);
            currentAttack = 4;

            Rigidbody star = Instantiate(projectile_star, transform.position + new Vector3(0.0f, 0.8f, 0.0f) + forward * 0.5f, Quaternion.Euler(forward)) as Rigidbody;
            star.velocity = forward * _starThrowForce;

            lastStarThrowTime = Time.time;
        }
    }

	Vector3 Rotating(float horizontal, float vertical) {
		Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
		forward = forward.normalized;
        forward.y = 0;

		Vector3 right = new Vector3(forward.z, 0, -forward.x);

		Vector3 targetDirection;

		float finalTurnSmoothing;

        targetDirection = forward * vertical + right * horizontal;

		if (IsAiming()) {
            if (targetDirection == Vector3.zero || Time.time - lastStarThrowTime < 0.1f) {
                targetDirection = forward;
            }
			finalTurnSmoothing = _aimTurnSmoothing;
        } else if (IsGrounded()) {
            finalTurnSmoothing = _turnSmoothing;
        } else {
            finalTurnSmoothing = _airTurnSmoothing;
        }

		if ((isMoving && targetDirection != Vector3.zero) || IsAiming()) {
			Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

            Quaternion newRotation = Quaternion.Lerp(rigidBody.rotation, targetRotation, finalTurnSmoothing * Time.deltaTime);
            rigidBody.MoveRotation(newRotation);
			lastDirection = targetDirection;
		}
		//idle
		if (!(Mathf.Abs(h) > 0.9 || Mathf.Abs(v) > 0.9)) {
			Repositioning();
		}

		return targetDirection;
	}	

	private void Repositioning() {
		Vector3 repositioning = lastDirection;
		if (repositioning != Vector3.zero) {
			repositioning.y = 0;
			Quaternion targetRotation = Quaternion.LookRotation(repositioning, Vector3.up);
            Quaternion newRotation = Quaternion.Slerp(rigidBody.rotation, targetRotation, _turnSmoothing * Time.deltaTime);
            rigidBody.MoveRotation(newRotation);
		}
	}

	public bool IsAiming() {
		return aim;
	}

	public bool IsSprinting() {
		return sprint && !aim && isMoving;
	}

    public bool IsGliding() {
        return glide && hasFirstJumped && hasSecondJumped && timeToNextJump <= 0 && !IsGrounded();
    }
}
