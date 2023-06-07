using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerModularAmmo : MonoBehaviour
{
    public AnimationCurve speedOverTime;
    [Tooltip( "vfx instantiated upon impact with enemy" )]
    public GameObject damageImpactVFX;
    [Tooltip( "vfx instantiated upon impact with obstacle" )]
    public GameObject noDamageImpactVFX;
    [HideInInspector]
    public ModularWeapon modularWeapon;

    [Tooltip( "Feedbacks played when bullet touches" )]
    public MMF_Player ImpactFeedbacks;

    protected float TimeToLive;
    [HideInInspector]
    public float liveTimer;
    protected bool isAlive = true;
    
    [HideInInspector]
    public Vector3 startPosition;
    [HideInInspector]
    public Vector3 endPosition;
    [HideInInspector]
    public Vector3 direction;
    [HideInInspector]
    public float animationTimer;
    [HideInInspector]
    public float speedMultiplicator;

    public virtual void Kill( bool dealt_damage )
    {
        if( !isAlive )
        {
            return;
        }

        isAlive = false;
        Instantiate( dealt_damage ? damageImpactVFX : noDamageImpactVFX, transform.position, Quaternion.identity );
        modularWeapon.currentGrabbableAmmo.Remove( this );;
        modularWeapon.currentSetupAmmo.Remove( this );
        Destroy( gameObject );
    }

    public void Hit(Transform target)
    {
        foreach( HitEffect hit_effect in modularWeapon.weaponEffectsDictionary[ WeaponEffectType.Hit ] )
        {
            hit_effect.Execute(transform.position, target);
        }
    }

    protected virtual void Awake()
    {
        startPosition = transform.position;
        speedMultiplicator = 0f;
    }

    protected virtual void Start()
    {
        if (modularWeapon.weaponEffectsDictionary[WeaponEffectType.Execution].FindIndex(x => x is TargetExecution) != -1)
        {
            endPosition = startPosition + transform.forward * modularWeapon.targetRange;
            speedMultiplicator = modularWeapon.targetRange / (float)modularWeapon.range.GetValue();
        }
        else
        {
            endPosition = startPosition + transform.forward * (float)modularWeapon.range.GetValue();;
        }

        if (modularWeapon.weaponEffectsDictionary[WeaponEffectType.Move].FindIndex(x => x is ParabolicMove) != -1)
        {
            if (Physics.Raycast(endPosition, Vector3.down, out RaycastHit hit_info, 10.0f,
                    1 << Constants.groundLayer))
            {
                endPosition = hit_info.point;
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        List<Vector3> move_position_list = new();

        foreach( MoveEffect move_effect in modularWeapon.weaponEffectsDictionary[ WeaponEffectType.Move ] )
        {
            move_position_list.Add( move_effect.Move(this ) );
        }

        if( move_position_list.Count == 0 )
        {
            return;
        }

        Vector3 average_position = new Vector3( move_position_list.Average( x => x.x ), move_position_list.Average( x => x.y ), move_position_list.Average( x => x.z ) );
        transform.position += average_position;
    }

    protected virtual void Update()
    {
        liveTimer += Time.deltaTime;

        if( liveTimer >= TimeToLive )
        {
            Kill( false );
        }
    }

    protected virtual void OnTriggerEnter( Collider other )
    {
        if( other.gameObject.layer == Constants.enemyLayer || ( !other.CompareTag( "Player" ) && !other.CompareTag( "PlayerInteractable" ) && !other.CompareTag( "Collectible" ) ) )
        {
            ImpactFeedbacks?.PlayFeedbacks();
            Kill( true );
            
            var enemy = other.GetComponent<EnemyBase>();
            if (!enemy)
                return;
            
            Hit(other.transform);
        }
    }
}
