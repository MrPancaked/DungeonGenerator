using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    
    [SerializeField] private NavMeshAgent navMeshAgent;
    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            Ray mouseRay = Camera.main.ScreenPointToRay( Input.mousePosition );
            if (Physics.Raycast( mouseRay, out RaycastHit hitInfo )) {
                Vector3 clickWorldPosition = hitInfo.point;
                if (hitInfo.collider.CompareTag("Floor"))
                {
                    GoToDestination(clickWorldPosition);
                }
                else
                {
                    print(clickWorldPosition + " is not a valid destination");
                }
            }
        }
    }

    public void GoToDestination(Vector3 destination)
    {
        navMeshAgent.SetDestination(destination);
    }
}
