using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] GameObject _target;
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _spped = 3;

    private void Update()
    {
        //_agent.SetDestination(_target.transform.position);

        X();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("asd");
    }

    private void X()
    {
        /*
        var PlayerPosition = _target.transform.position;

        var vectorToPlayer = PlayerPosition - transform.position;
        var dir = math.normalize(new Vector2(vectorToPlayer.x, vectorToPlayer.z));

        Vector2 moveStep = dir * _spped * Time.deltaTime;
        Vector3 currentVelocity = _rb.linearVelocity;

        _rb.linearVelocity = new Vector3(moveStep.x, currentVelocity.y, moveStep.y);
        */
        Vector3 direction = (_target.transform.position - transform.position).normalized;

        // Assign new velocity
        _rb.linearVelocity = direction * _spped;

        transform.LookAt(_target.transform.position);
    }
}
