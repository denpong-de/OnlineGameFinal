using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehav : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    private Vector2 moveVec;

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        Move();
    }

    //Get values from New Input System.
    public void OnMove(InputValue value) 
    {
        moveVec = value.Get<Vector2>();
    }

    private void Move()
    {
        this.transform.position += new Vector3(moveVec.x * moveSpeed, 0f, 0f) * Time.deltaTime;
    }
}
