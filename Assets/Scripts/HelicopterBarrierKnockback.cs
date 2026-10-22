using System.Collections;
using UnityEngine;

public sealed class HelicopterBarrierKnockback : MonoBehaviour
{
    [SerializeField, Min(0f)] private float backwardSpeed = 7.5f;
    [SerializeField, Min(0f)] private float upwardSpeed = 1.8f;
    [SerializeField, Min(0.05f)] private float recoveryDelay = 0.42f;

    private bool recoveryRunning;

    public void Configure(
        float backward,
        float upward,
        float delay)
    {
        backwardSpeed = Mathf.Max(0f, backward);
        upwardSpeed = Mathf.Max(0f, upward);
        recoveryDelay = Mathf.Max(0.05f, delay);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (recoveryRunning)
        {
            return;
        }

        PlayerRunner runner =
            collision.gameObject.GetComponent<PlayerRunner>();

        if (runner == null)
        {
            runner =
                collision.gameObject.GetComponentInParent<PlayerRunner>();
        }

        if (runner == null)
        {
            return;
        }

        Rigidbody body = runner.GetComponent<Rigidbody>();

        if (body == null)
        {
            return;
        }

        StartCoroutine(RecoverPlayer(runner, body));
    }

    private IEnumerator RecoverPlayer(
        PlayerRunner runner,
        Rigidbody body)
    {
        recoveryRunning = true;
        runner.SetMovementEnabled(false);

        Vector3 velocity = body.linearVelocity;
        velocity.x = 0f;
        velocity.y = upwardSpeed;
        velocity.z = -backwardSpeed;
        body.linearVelocity = velocity;

        CameraShakeController.ShakeGlobal(0.16f, 0.06f);

        yield return new WaitForSeconds(recoveryDelay);

        if (runner != null)
        {
            runner.SetMovementEnabled(true);
        }

        recoveryRunning = false;
    }
}
