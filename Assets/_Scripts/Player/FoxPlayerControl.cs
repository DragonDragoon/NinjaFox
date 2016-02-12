using UnityEngine;
using System.Collections;

public class FoxPlayerControl : MonoBehaviour {

	public float _walkSpeed = 0.15f;
	public float _runSpeed = 1.0f;
	public float _sprintSpeed = 2.0f;

	public float _turnSmoothing = 3.0f;
	public float _aimTurnSmoothing = 15.0f;
	public float _speedDampTime = 0.1f;

	public float _jumpHeight = 5.0f;
    public float _secondJumpHeight = 3.0f;
	public float _jumpCooldown = 1.0f;

	private float timeToNextJump = 0;
	
	private float speed;

	private Vector3 lastDirection;

	private Animator anim;
	private int speedFloat;
	private int firstJumpBool;
    private int secondJumpBool;
    private int canFirstJumpBool;
    private int canSecondJumpBool;
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

	private bool isMoving;

	private Vector3 colliderBound;

    private bool hasFirstJumped = false;
    private bool hasSecondJumped = false;

	void Awake() {
		anim = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody>();
		cameraTransform = Camera.main.transform;

		speedFloat = Animator.StringToHash("Speed");
		firstJumpBool = Animator.StringToHash("FirstJump");
        secondJumpBool = Animator.StringToHash("SecondJump");
        canFirstJumpBool = Animator.StringToHash("canFirstJump");
        canSecondJumpBool = Animator.StringToHash("canSecondJump");
		hFloat = Animator.StringToHash("H");
		vFloat = Animator.StringToHash("V");
		aimBool = Animator.StringToHash("Aim");
		
        groundedBool = Animator.StringToHash("Grounded");
		colliderBound = GetComponent<Collider>().bounds.extents;
	}

	bool IsGrounded() {
		return Physics.Raycast(transform.position, Vector3.down, colliderBound.y);
	}

	void Update() {
		aim = Input.GetButton("Aim");
		h = Input.GetAxis("Horizontal");
		v = Input.GetAxis("Vertical");
		run = Input.GetButton("Run");
		sprint = Input.GetButton("Sprint");
        jump = Input.GetButtonDown("Jump");
		isMoving = Mathf.Abs(h) > 0.1 || Mathf.Abs(v) > 0.1;
	}

	void FixedUpdate() {
		anim.SetBool(aimBool, IsAiming());
		anim.SetFloat(hFloat, h);
		anim.SetFloat(vFloat, v);
		
		anim.SetBool(groundedBool, IsGrounded());

        anim.applyRootMotion = IsGrounded();

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
    }

	void JumpManagement() {
        Vector3 currentVelocity = rigidBody.velocity;
        if (IsGrounded()) { //already jumped and landed
			anim.SetBool(firstJumpBool, false);
            anim.SetBool(secondJumpBool, false);
            hasFirstJumped = false;
            hasSecondJumped = false;
            //if (timeToNextJump > 0) {
            //    timeToNextJump -= Time.deltaTime;
            //}
		}
		if (jump) {
			//if(speed > 0 && timeToNextJump <= 0 && !aim) {
            if (IsGrounded() && !hasFirstJumped) { //first jump
                anim.SetBool(firstJumpBool, true);
                anim.SetBool(groundedBool, false);
                rigidBody.velocity = new Vector3(currentVelocity.x, _jumpHeight, currentVelocity.z);
				//timeToNextJump = _jumpCooldown;
                hasFirstJumped = true;
			} else if (!hasSecondJumped) { //second jump
                anim.SetBool(secondJumpBool, true);
                rigidBody.velocity = new Vector3(currentVelocity.x, _jumpHeight, currentVelocity.z);
                //timeToNextJump = _jumpCooldown;
                hasSecondJumped = true;
            }
		}
	}

	void MovementManagement(float horizontal, float vertical, bool running, bool sprinting) {
		Rotating(horizontal, vertical);

		if (isMoving) {
			if (sprinting) {
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
        rigidBody.AddForce(transform.TransformDirection(Vector3.forward) * speed);
	}

	Vector3 Rotating(float horizontal, float vertical) {
		Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
		forward = forward.normalized;
        forward.y = 0;

		Vector3 right = new Vector3(forward.z, 0, -forward.x);

		Vector3 targetDirection;

		float finalTurnSmoothing;

		if (IsAiming()) {
			targetDirection = forward;
			finalTurnSmoothing = _aimTurnSmoothing;
		} else {
			targetDirection = forward * vertical + right * horizontal;
			finalTurnSmoothing = _turnSmoothing;
		}

		if ((isMoving && targetDirection != Vector3.zero) || IsAiming()) {
			Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

            Quaternion newRotation = Quaternion.Slerp(rigidBody.rotation, targetRotation, finalTurnSmoothing * Time.deltaTime);
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

	public bool isSprinting() {
		return sprint && !aim && isMoving;
	}
}
