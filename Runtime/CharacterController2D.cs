using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour
{
    Rigidbody2D rb;

    public float Speed => rb.linearVelocity.magnitude;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 direction, float speed, float maxSpeed)
    {
        Vector2 step = Vector2.MoveTowards(rb.position, rb.position + direction * speed, maxSpeed * Time.fixedDeltaTime);
        rb.MovePosition(step);
        //rb.linearVelocity = Vector2.ClampMagnitude(direction * speed, maxSpeed);
    }

    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
}
