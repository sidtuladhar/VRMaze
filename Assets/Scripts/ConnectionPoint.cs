using UnityEngine;

public class ConnectionPoint : MonoBehaviour
{
    public Vector3 connectionOffset;  // Add this to store position offset

    public GameObject DeadEndPrefab;  // Add this to store dead end prefab

    void OnDrawGizmos()  // This will help visualize connection points in editor
    {
        Gizmos.color = Color.yellow;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        
        // Draw connection point
        Gizmos.DrawWireSphere(connectionOffset, 0.3f);
        // Draw direction line
        Gizmos.DrawLine(Vector3.zero, connectionOffset);
    }
}