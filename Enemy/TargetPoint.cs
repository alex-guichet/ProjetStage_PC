using Lofelt.NiceVibrations;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

public enum TargetPointType
{
    PLAYER,
    POD,
    BUILDING,
    TURRET
}

[RequireComponent(typeof(SphereCollider))]
public class TargetPoint : MonoBehaviour
{
    [Tooltip( "Points around the circle in degrees" )]
    public CirclePoint[] circlePoints;
    [Tooltip( "Radius of the circle" )]
    public float circleRadius;
    [Tooltip( "Offset radius of the collider" )]
    public float offsetCollider;
    [Tooltip( "Type of the Target point" )]
    public TargetPointType targetPointType;
    [HideInInspector]
    public UnityEvent onDestroy = new();
    [HideInInspector]
    public int entityId;

    private SphereCollider _sphereCollider;

    private void OnDestroy()
    {
        EntityManager.Instance.RemoveEntity(this);
        onDestroy.Invoke();
    }

    private void Start()
    {
        entityId = EntityManager.Instance.AddEntity(this);
    }
    
    
#if UNITY_EDITOR
    [ContextMenu("Generates points around the circle")]
    public void AddPoints()
    {
        _sphereCollider = GetComponent<SphereCollider>();
        _sphereCollider.radius = (circleRadius + offsetCollider) / 18;
        if (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        
        Vector3 center_position = transform.position;
        
        GameObject targetParent = new GameObject("TargetParent");
        targetParent.transform.parent = transform;
        
        for(int i = 0; i < circlePoints.Length; i++)
        {
            float x = center_position.x + (circleRadius * Mathf.Cos(circlePoints[i].degree * Mathf.Deg2Rad));
            float z = center_position.z + (circleRadius * Mathf.Sin(circlePoints[i].degree * Mathf.Deg2Rad));
            
            GameObject target_point = new GameObject("Target_"+i);
            target_point.transform.position = new Vector3(x, 0, z);;
            target_point.transform.parent = targetParent.transform;

            circlePoints[i].circleTransform = target_point.transform;
        }
    }
    
    void OnDrawGizmos()
    {
        foreach (CirclePoint c in circlePoints)
        {
            Gizmos.DrawIcon(c.circleTransform.transform.position, "Knob.tiff", true);
        }
        Handles.DrawWireDisc(transform.position, Vector3.up, circleRadius);
    }
#endif
    
}

[System.Serializable]
public struct CirclePoint
{
    [Range(0,360)] public int degree;
    public Transform circleTransform;
}
