using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static EnemyState;
using Random = UnityEngine.Random;

public enum WeakPoint
{
    FRONT,
    BACK,
    NONE
}

public class EnemyMajor : EnemyBase
{
    [Header("Debug")]
    public GameObject frontWeakPointVisual;
    public GameObject backWeakPointVisual;

    [Header("Attack Settings")]
    [Tooltip("Radius of the player detection sphere")]
    public float playerDetectionRadius;
    [Tooltip("Rotation speed of the enemy in Attack Mode")]
    public float attackRotationSpeed;
    [Tooltip("Offset Raycast attack detector")]
    public Vector3 offsetRaycast;
    [Tooltip("Charge time of the attack")]
    public float chargeTime = 2f;
    [Tooltip("Cooldown time of the attack")]
    public float cooldownTime = 4f;
    [Tooltip("Prefab of the shockwave")] 
    public GameObject shockWavePrefab;
    [Tooltip("Transform of the start position of the shockwave")]
    public Transform shockWaveStartTransform;
    [Tooltip("Interval spawn distance of the minions")]
    public float spawnInterval;
    [Tooltip("Enemy formation to spawn")] 
    public EnemyFormation_ScriptableObject enemyFormation;

    [Header("Damage Settings")]
    [Tooltip("Percentage damage on non weak points")]
    [Range(0,100)] public float nonWeakPointDamageRatio;
    [Tooltip("Percentage damage on front weak point")]
    [Range(0,100)] public float frontWeakPointDamageRatio;
    [Tooltip("Percentage damage on back weak point")]
    [Range(0,100)] public float backWeakPointDamageRatio;
    [Tooltip("Amount of damage to get the aggro")]
    public float aggroDamage;
    [Tooltip("Amount of damage to stun")]
    public float stunDamage;
    
    [Header("Weak points Settings")]
    [Tooltip("Transform of the front weak point")]
    public Transform frontWeakPointTransform;
    [Tooltip("Radius of the front weak point")]
    public float radiusFrontWeakPoint;
    [Tooltip("Transform of the back weak point")]
    public Transform backWeakPointTransform;
    [Tooltip("Radius of the back weak point")]
    public float radiusBackWeakPoint;
    [Tooltip("Stun time")]
    public float stunTime;
    
    [HideInInspector] public PlayerController currentPlayerTargeted;
    
    private float _currentAggro;
    private float _currentStun;
    private float _radiusWeakPoint = 0f;
    
    private Transform _positionWeakPoint = null;
    private WeakPoint _currentWeakPoint;
    private Coroutine _isAttackingCoroutine;
    private Coroutine _isStunCoroutine;
    private Slider _currentHealthBar;

    private void OnStateEnter()
    {
        switch (currentState)
        {
            case MOVING:
                agent.enabled = true;
                InvokeRepeating("SetDestination", 0, enemySettings.navMeshRefreshRate);
                break;
            case ATTACK:
                agent.enabled = false;
                break;
            case SHOCKWAVE:
                SwitchWeakPoint(WeakPoint.BACK);
                backWeakPointVisual.SetActive(true);
                _isAttackingCoroutine = StartCoroutine(IsAttacking());
                break;
            case STUN:
                if (_isAttackingCoroutine != null)
                {
                    StopCoroutine(_isAttackingCoroutine);
                }
                SwitchWeakPoint(WeakPoint.FRONT);
                frontWeakPointVisual.SetActive(true);
                _isStunCoroutine = StartCoroutine(IsStun());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void OnStateFixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.MOVING:
                DetectPlayer();
                break;
            case EnemyState.ATTACK:
                DetectPlayer();
                TargetPlayer();
                break;
            case EnemyState.SHOCKWAVE:
                DetectBullet();
                break;
            case EnemyState.STUN:
                DetectBullet();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void OnStateExit()
    {
        switch (currentState)
        {
            case MOVING:
                CancelInvoke("SetDestination");
                break;
            case ATTACK:
                enemyRb.velocity = Vector3.zero;
                break;
            case SHOCKWAVE:
                backWeakPointVisual.SetActive(false);
                break;
            case STUN:
                frontWeakPointVisual.SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TransitionToState(EnemyState state)
    {
        OnStateExit();
        currentState = state;
        OnStateEnter();
    }
    
    public override void ReceiveDamage(float damage_amount, PlayerController playerController, WeakPoint weak_point = WeakPoint.NONE)
    {
        HitFeedbacks?.PlayFeedbacks();
        float damage = 0f;
        switch (weak_point)
        {
            case WeakPoint.FRONT :
                damage = damage_amount * (frontWeakPointDamageRatio/100f);
                healthPoints -= damage;
                break;
            case WeakPoint.BACK :
                damage = damage_amount * (backWeakPointDamageRatio/100f);
                healthPoints -= damage;
                _currentStun += damage;
                if (_currentStun >= stunDamage)
                {
                    _currentStun = 0f;
                    TransitionToState(STUN);
                }
                break;
            case WeakPoint.NONE :
                damage = damage_amount * (nonWeakPointDamageRatio/100f);
                healthPoints -= damage;
                
                if (currentState != ATTACK)
                    return;
                _currentAggro += damage;
        
                
                if (_currentAggro >= aggroDamage)
                {
                    _currentAggro = 0f;
                    currentPlayerTargeted = playerController;
                }
                break;
        }

        healthBarCanvas.SetActive(true);
        _currentHealthBar.value = healthPoints / enemySettings.initialHealthPoints;
    }
     
    protected override void SetDestination()
    {
        if (!currentPlayerTargeted)
        {
            if (!EntityManager.HasInstance())
                return;
            currentPlayerTargeted = EntityManager.Instance.DetectIsolatedPlayer();
        }
        NavMesh.CalculatePath (transform.position, currentPlayerTargeted.transform.position, agent.areaMask, path);
        agent.SetPath (path);
    }

    IEnumerator IsAttacking()
    {
        GameObject shockWave = Instantiate(shockWavePrefab, shockWaveStartTransform.position, transform.rotation);
        EnemyShockwave enemy_shockwave = shockWave.GetComponent<EnemyShockwave>();
        enemy_shockwave.enemyMajor = this;
        enemy_shockwave.chargingTime = chargeTime;
        
        yield return new WaitForSeconds(chargeTime);
        yield return new WaitForSeconds(cooldownTime);
        TransitionToState(ATTACK);
    }
    
    IEnumerator IsStun()
    {
        yield return new WaitForSeconds(stunTime);
        currentPlayerTargeted = EntityManager.Instance.DetectNearestPlayer(transform.position);
        TransitionToState(ATTACK);
    }

    public void InitializeBoss()
    {
        currentPlayerTargeted = EntityManager.Instance.DetectIsolatedPlayer();
        TransitionToState(MOVING);
    }
    
    protected override void OnTriggerEnter(Collider other)
    {
    }
    
    protected override void OnTriggerExit(Collider other)
    {
    }

    private void BootstrapManager_OnScenesLoaded()
    {
        SelectRoverInterface.Instance.onConfirmPlayerCreated.AddListener(InitializeBoss);
    }

    private void DetectPlayer()
    {
        int player_detected;
        Collider[] player_detection = Physics.OverlapSphere(transform.position, playerDetectionRadius,enemySettings.targetLayer );
        player_detected = Array.FindIndex(player_detection, player => player.transform.GetComponent<PlayerController>().entityId == currentPlayerTargeted.entityId );

        if (player_detected == -1)
        {
            if (currentState != MOVING)
            {
                TransitionToState(MOVING);
            }
            return;
        }
        
        if (currentState != ATTACK)
        {
            TransitionToState(ATTACK);
        }
    }

    private void TargetPlayer()
    {
        if (!currentPlayerTargeted)
            return;
        
        float rotation_step = attackRotationSpeed * Time.deltaTime;
        Vector3 lookTowardsPlayer = currentPlayerTargeted.transform.position - transform.position;
        Quaternion look_rotation = Quaternion.LookRotation(lookTowardsPlayer);
        Quaternion rotateTowards = Quaternion.RotateTowards(transform.rotation, look_rotation, rotation_step );
        enemyRb.MoveRotation(rotateTowards);
        
        bool hitPlayer = Physics.Raycast(transform.position + offsetRaycast, transform.forward, out RaycastHit hit,
            playerDetectionRadius, enemySettings.targetLayer);
        if (hitPlayer)
        {
            TransitionToState(SHOCKWAVE);
        }
    }

    private void SwitchWeakPoint(WeakPoint weak_point)
    {
        _currentWeakPoint = weak_point;
        switch (weak_point)
        {
            case WeakPoint.FRONT :
                _radiusWeakPoint = radiusFrontWeakPoint;
                _positionWeakPoint = frontWeakPointTransform;
                break;
            case WeakPoint.BACK :
                _radiusWeakPoint = radiusBackWeakPoint;
                _positionWeakPoint = backWeakPointTransform;
                break;
        }
    }
    
    private void DetectBullet()
    {
        Collider[] bullet_detection = Physics.OverlapSphere(_positionWeakPoint.position, _radiusWeakPoint, 1<<Constants.bulletLayer );
        if (bullet_detection.Length == 0)
            return;

        foreach (Collider c in bullet_detection)
        {
            PlayerModularAmmo bullet = c.GetComponent<PlayerModularAmmo>();
            if (bullet)
            {
                ReceiveDamage((float)bullet.modularWeapon.damage.GetValue(), null, _currentWeakPoint);
                Destroy(bullet.gameObject);
            }
        }
    }

    protected override void Start()
    {
        currentState = MOVING;
        BootstrapManager.Instance.ScenesLoaded += BootstrapManager_OnScenesLoaded;
    }
    
    private void Update()
    {
        OnStateFixedUpdate();
    }
    
    void OnDrawGizmos()
    {
        Vector3 transform_position = transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform_position, playerDetectionRadius);
        
        bool isHit = Physics.Raycast(transform_position + offsetRaycast, transform.forward, out RaycastHit hit,
            playerDetectionRadius, enemySettings.targetLayer);
        if (isHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform_position + offsetRaycast, transform.forward * hit.distance);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform_position + offsetRaycast, transform.forward * playerDetectionRadius);
        }

        if (currentState == STUN)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(frontWeakPointTransform.position, radiusFrontWeakPoint);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(frontWeakPointTransform.position, radiusFrontWeakPoint);
        }
        
        if (currentState == SHOCKWAVE)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(backWeakPointTransform.position, radiusBackWeakPoint);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(backWeakPointTransform.position, radiusBackWeakPoint);
        }
    }
}
