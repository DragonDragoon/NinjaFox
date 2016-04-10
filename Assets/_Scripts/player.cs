using UnityEngine;
using System.Collections;

public class player : MonoBehaviour {
    public Animator anim;
    public Rigidbody rbody;
    public float speed;

    private float inputH;
    private float inputV;
    private bool run;
    private bool jump;
    private bool canJump;

	// initialization
	void Start () {
        anim = GetComponent<Animator>();
        rbody = GetComponent<Rigidbody>();
        run = false;
        jump = false;
	}

    void Update() {
        
    }
	
	void LateUpdate () {
	    if (Input.GetKeyDown("1")) {
            anim.Play("FOX_EVADE_L00", -1, 0f);
        }
        if (Input.GetKeyDown("2")) {
            anim.Play("FOX_EVADE_R00", -1, 0f);
        }

        if (Input.GetKey(KeyCode.LeftShift)) {
            run = true;
        } else {
            run = false;
        }

        canJump = true;
        if (Input.GetKey(KeyCode.Space) && canJump) {
            jump = true;
        } else {
            jump = false;
        }

        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxis("Vertical");

        anim.SetFloat("inputH", inputH);
        anim.SetFloat("inputV", inputV);
        anim.SetBool("run", run);
        anim.SetBool("jump", jump);

        move();
	}

    void move() {
        Vector3 move = new Vector3(inputH * Time.deltaTime, rbody.velocity.y, inputV * Time.deltaTime);
        move = new Vector3(Camera.main.transform.TransformDirection(move).x, rbody.velocity.y, Camera.main.transform.TransformDirection(move).z);

        if (run) {
            move *= 5.0f;
        }

        if (jump) {
            move = new Vector3(move.x, 1.0f, move.z);
        }

        transform.LookAt(new Vector3(transform.position.x + move.x, transform.position.y, transform.position.z + move.z));
        rbody.velocity = speed * 100.0f * move;
    }
}
