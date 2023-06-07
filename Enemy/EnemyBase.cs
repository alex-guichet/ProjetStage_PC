using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using Shapes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public enum EnemyState
{
    FAR,
    CLOSE,
    FOCUS,
    ATTACK,
    SHOCKWAVE,
    STUN,
    MOVING
}

public abstract class EnemyBase : PoolableObject
{
    [Tooltip( "Feedbacks on death" )]
    public MMF_Player DeathFeedbacks;
    [Tooltip( "Feedbacks on hit" )]
    public MMF_Player HitFeedbacks;
    [Tooltip( "Scriptable object of the base enemy settings" )]
    public EnemyBaseSettings enemySettings;
    [Tooltip( "Current enemy State used for debugging" )]
    public EnemyState currentState;
    [Tooltip( "Death visual effect VFX (prefab name)" )]
    public string deathVisualEffect;
    [Tooltip( "Damage visual effect VFX" )]
    public string hitVisualEffect;
    [Tooltip( "Time before effusion disappear" )]
    public float effusionDisappearingTime = 35.0f;

    [Tooltip( "Life bar canvas" )]
    public GameObject healthBarCanvas;
    [Tooltip( "Life bar slider" )]
    public Slider healthBarSlider;

    [Tooltip( "Amount of knockback applied on enemy when hit" )]
    public float HitKnockback = 10f;

    [HideInInspector]
    public NavMeshAgent agent;

    protected NavMeshPath path;
    protected Transform defaultTarget;
    protected Transform currentTarget;
    protected Transform lastTarget;
    protected int currentTargetPointId;
    protected Rigidbody enemyRb;

    private float _healthPoints;
    private bool _canDamage = true;
    private bool _isLooping;
    private bool _isAggro;
    private CirclePoint[] _circlePoints;
    private Coroutine _iteratePoints;
    private float _pathRefreshTimer;
    private float _damageTimer;

    public float healthPoints
    {
        get => _healthPoints;
        set
        {
            _healthPoints = value;

            if( _healthPoints <= 0.0f )
            {
                Kill();
            }
        }
    }

    private static NavMeshHit PoolNavmeshHit;

    public override PoolableObject GenerateObject()
    {
        if( !PoolNavmeshHit.hit )
        {
            NavMesh.FindClosestEdge( Vector3.zero, out PoolNavmeshHit, NavMesh.AllAreas );
        }

        return Instantiate( gameObject, PoolNavmeshHit.position, quaternion.identity ).GetComponent<PoolableObject>();
    }

    private void SetEnemySpeed( float speed, float acceleration )
    {
        agent.speed = speed;
        agent.acceleration = acceleration;
    }

    protected virtual Transform DetectNearbyTargets()
    {
        Transform nearest_target;
        Collider[] hitColliders = Physics.OverlapSphere( transform.position, enemySettings.radiusDetectionSize, enemySettings.targetLayer );

        if( hitColliders.Length == 0 )
        {
            Dictionary<int, TargetPoint> target_points = EntityManager.Instance.targetPoints;
            TargetPoint target_point = target_points.ElementAt( Random.Range( 0, target_points.Count ) ).Value;
            nearest_target = target_point.transform;
            return nearest_target;
        }

        float shortest_distance = Vector3.Distance( hitColliders[ 0 ].transform.position, transform.position );
        nearest_target = hitColliders[ 0 ].transform;

        for( int i = 1; i < hitColliders.Length; i++ )
        {
            float distance = Vector3.Distance( hitColliders[ i ].transform.position, transform.position );

            if( distance < shortest_distance )
            {
                shortest_distance = distance;
                nearest_target = hitColliders[ i ].transform;
            }
        }

        return nearest_target;
    }

    protected virtual void SetDestination()
    {
        if( !defaultTarget )
        {
            defaultTarget = DetectNearbyTargets();
            TargetPoint target_point = defaultTarget.GetComponent<TargetPoint>();
            currentTargetPointId = target_point.entityId;
            target_point.onDestroy.AddListener( CoroutineIterateStop );
            _circlePoints = target_point.circlePoints;
            currentState = EnemyState.FAR;
            SetEnemySpeed( enemySettings.normalSpeed, enemySettings.normalAcceleration );
        }

        if( !currentTarget )
        {
            currentTarget = defaultTarget;
            _isAggro = false;
            currentState = EnemyState.FAR;
            SetEnemySpeed( enemySettings.normalSpeed, enemySettings.normalAcceleration );
        }

        if( !NavMesh.CalculatePath( transform.position, currentTarget.position, agent.areaMask, path ) )
        {
            if( !NavMesh.SamplePosition( currentTarget.position, out NavMeshHit hit, 100.0f, NavMesh.AllAreas ) )
            {
                return;
            }

            var target_position = currentTarget.position;

            target_position = new Vector3( target_position.x, hit.position.y, target_position.z );
            currentTarget.position = target_position;
            NavMesh.CalculatePath( transform.position, target_position, agent.areaMask, path );
        }

        if( agent.isOnNavMesh )
        {
            agent.SetPath( path );
        }
    }

    public virtual void ReceiveDamage( float damage_amount, PlayerController player_controller, WeakPoint weak_point = WeakPoint.NONE )
    {
        healthPoints -= damage_amount;
        healthBarCanvas.SetActive( true );
        healthBarSlider.value = healthPoints / enemySettings.initialHealthPoints;

        HitFeedbacks?.PlayFeedbacks();
        ObjectPoolingManager.Instance.Get( hitVisualEffect, transform.position );

        if( !player_controller )
        {
            return;
        }

        agent.velocity = Vector3.zero;

        if( currentState == EnemyState.FOCUS )
        {
            return;
        }

        SetEnemySpeed( enemySettings.focusSpeed, enemySettings.focusAcceleration );
        currentState = EnemyState.FOCUS;
        currentTarget = player_controller.transform;
        _isAggro = true;
        SetDestination();
    }

    private void Kill()
    {
        DeathFeedbacks?.PlayFeedbacks();

        ObjectPoolingManager.Instance.Get( deathVisualEffect, transform.position );
        ObjectPoolingManager.Instance.Put( this );
    }

    private void ContactDamage( PlayerHealth player_health )
    {
        player_health.ReceiveDamage( enemySettings.damage );
        StartCoroutine( DamageDelay() );
    }

    private IEnumerator DamageDelay()
    {
        yield return new WaitForSeconds( enemySettings.delayDamage );
        _canDamage = true;
    }

    public virtual void InitializeEnemy( Vector3 position, TargetPoint target_point )
    {
        InitializeEnemy( position, target_point, target_point.circlePoints[ Random.Range( 0, target_point.circlePoints.Length ) ].circleTransform );
    }

    public virtual void InitializeEnemy( Vector3 position, TargetPoint target_point, Transform first_target )
    {
        agent.Warp( position );
        target_point.onDestroy.AddListener( CoroutineIterateStop );
        defaultTarget = target_point.transform;
        currentTargetPointId = target_point.entityId;
        _circlePoints = target_point.circlePoints;
        currentTarget = first_target;
        lastTarget = first_target;
        //InvokeRepeating( nameof( SetDestination ), 0, enemySettings.navMeshRefreshRate );
    }

    IEnumerator IterateTroughPoints()
    {
        _isLooping = true;
        NavMesh.CalculatePath( transform.position, currentTarget.position, agent.areaMask, path );
        List<int> used_indexes_list = new();

        while( path.status == NavMeshPathStatus.PathInvalid )
        {
            if( used_indexes_list.Count == _circlePoints.Length )
            {
                currentTarget = defaultTarget;

                if( !currentTarget )
                {
                    break;
                }

                NavMesh.CalculatePath( transform.position, currentTarget.position, agent.areaMask, path );
                break;
            }

            int index = Random.Range( 0, _circlePoints.Length );

            while( used_indexes_list.FindIndex( x => x == index ) != -1 )
            {
                index = Random.Range( 0, _circlePoints.Length );
            }

            currentTarget = _circlePoints[ index ].circleTransform;

            if( !currentTarget )
            {
                break;
            }

            NavMesh.CalculatePath( transform.position, currentTarget.position, agent.areaMask, path );
            used_indexes_list.Add( index );
            yield return new WaitForSeconds( 0.1f );
        }

        agent.SetPath( path );
        _isLooping = false;
    }

    public void CoroutineIterateStop()
    {
        if( !currentTarget )
        {
            if( _iteratePoints != null )
            {
                StopCoroutine( _iteratePoints );
                _isLooping = false;
            }
        }
    }

    public override void ResetForStorage()
    {
        healthPoints = enemySettings.initialHealthPoints;
        HitFeedbacks?.ResetFeedbacks();

        if( agent.isOnNavMesh )
        {
            agent.ResetPath();
        }

        base.ResetForStorage();
    }

    public override void WakeFromStorage()
    {
        base.WakeFromStorage();
        currentState = EnemyState.FAR;
        _isAggro = false;
        healthBarSlider.value = 1.0f;
        healthBarCanvas.SetActive( false );
        _pathRefreshTimer = -1f;
    }

    private void UpdateLifebarRotation()
    {
        if( healthBarCanvas.activeSelf )
        {
            healthBarCanvas.transform.rotation = Quaternion.Euler( 50f, -transform.rotation.y, 0f );
        }
    }

    protected virtual void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
        healthPoints = enemySettings.initialHealthPoints;
    }

    protected virtual void Start()
    {
        currentState = EnemyState.FAR;

        agent.angularSpeed = 1000;
        
        //Dictionary<int, TargetPoint> target_points = EntityManager.Instance.originalTargetPoints;
        //TargetPoint target_point = target_points.ElementAt(Random.Range(0, target_points.Count)).Value;
        //InitializeEnemy(target_point);
    }

    private void Update()
    {
        UpdateLifebarRotation();

        _pathRefreshTimer -= Time.deltaTime;

        if( _pathRefreshTimer <= 0.0f )
        {
            _pathRefreshTimer = enemySettings.navMeshRefreshRate * Random.Range(0.95f, 1.05f);
            SetDestination();
        }
    }

    protected virtual void OnTriggerEnter( Collider other )
    {
        if( currentState == EnemyState.FAR )
        {
            TargetPoint target_point = other.GetComponent<TargetPoint>();

            if( !target_point )
                return;

            if (target_point.entityId != currentTargetPointId)
                return;

            lastTarget = currentTarget;
            currentTarget = defaultTarget;
            currentState = EnemyState.CLOSE;
        }
    }

    protected virtual void OnTriggerStay( Collider other )
    {
        var modular_laser = other.GetComponent<PlayerModularLaser>();
        if (modular_laser)
        {
            _damageTimer += Time.deltaTime;
            if (_damageTimer >= modular_laser.damageTime)
            {
                _damageTimer = 0f;
                modular_laser.Hit(transform);
            }
        }
        
        if( !_canDamage )
        {
            return;
        }
        
        if( other.CompareTag( "Player" ) )
        {
            var player_health = other.GetComponent<PlayerHealth>();

            if( player_health && _canDamage )
            {
                _canDamage = false;
                ContactDamage( player_health );
            }
        }

        if( other.gameObject.layer == Constants.podLayer )
        {
            PodController pod_controller = other.GetComponent<PodController>();

            if( pod_controller && _canDamage )
            {
                _canDamage = false;
                pod_controller.TakeDamage( enemySettings.damage );
                StartCoroutine( DamageDelay() );
            }
        }
    }

    protected virtual void OnTriggerExit( Collider other )
    {

        var modular_laser = other.GetComponent<PlayerModularLaser>();
        if (modular_laser)
        {
            _damageTimer = 0f;
        }
        
        if (_isAggro)
        {
            return;
        }
        
        if( currentState == EnemyState.CLOSE )
        {
            TargetPoint target_point = other.GetComponent<TargetPoint>();

            if( !target_point )
            {
                return;
            }

            if( target_point.entityId == currentTargetPointId )
            {
                currentTarget = lastTarget;
                currentState = EnemyState.FAR;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var position = transform.position;

        Gizmos.DrawWireSphere( position, enemySettings.radiusDetectionSize );
        Gizmos.color = Color.white;

        if( currentTarget )
        {
            Gizmos.DrawLine( position + Vector3.up, currentTarget.transform.position + Vector3.up );
        }
    }
}
