using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour
{
    Rigidbody2D rb;

    public float Speed => rb.velocity.magnitude;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 direction, float speed, float maxSpeed)
    {
        rb.AddForce(speed * direction);
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxSpeed);
    }

    public void SetVelocity(Vector2 velocity)
    {
        rb.velocity = velocity;
    }
}
