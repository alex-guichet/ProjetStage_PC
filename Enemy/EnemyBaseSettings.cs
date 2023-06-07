using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;


[CreateAssetMenu(fileName = "EnemyBaseSettings", menuName = "ScriptableObjects/EnemySettings")]
public class EnemyBaseSettings : ScriptableObject
{
    [Header( "Settings" )]
    [Tooltip( "Initial health points of the enemy" )]
    public float initialHealthPoints;
    [Tooltip("Refresh rate of the navmesh path")]
    public float navMeshRefreshRate = 2f;
    [Tooltip("Size of the detection radius for nearby targets")]
    public float radiusDetectionSize = 5f;
    [Tooltip("Layer of the nearby targets")]
    public LayerMask targetLayer;
    [Tooltip("Normal speed of the enemy")]
    public float normalSpeed = 4.5f;
    [Tooltip("Normal acceleration of the enemy")]
    public float normalAcceleration= 8f;
    [Tooltip("Focus speed of the enemy")]
    public float focusSpeed = 6f;
    [Tooltip("Focus acceleration of the enemy")]
    public float focusAcceleration = 16f;
    [Tooltip("Damage done by the enemy")]
    public float damage;
    [Tooltip("Delay between each damage")]
    public float delayDamage;
}
