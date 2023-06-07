using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


public class PlayerController : MonoBehaviour
{
    [Header( "Settings" )]
    [Tooltip( "Settings for the current rover" )]
    public RoverSettings_ScriptableObject playerSettings;
    [Tooltip( "Does Radar follow player" )]
    public bool isRadarFollowingPlayer;
    [Tooltip( "Offset on body rotation to compensate skeletal orientation" )]
    public Vector3 bodyRotationOffset = new Vector3( 0.0f, 90.0f, 0.0f );
    [Tooltip( "Offset on wheels rotation to compensate skeletal orientation" )]
    public Vector3 wheelsRotationOffset = new Vector3( 0.0f, 90.0f, 0.0f );
    [Tooltip( "Wheel parent rotation smoothness" )]
    public float wheelRotationSmoothSharpness = 15.0f;

    [Header( "Reference" )]
    [Tooltip( "Reference to the rigidbody of the player" )]
    public Rigidbody playerRigidbody;
    [Tooltip( "Reference to the loot monitor" )]
    public PlayerLootMonitor lootMonitor;
    [Tooltip( "Reference to the prefab of the radar" )]
    public PlayerRadar playerRadar;
    [Tooltip("Speed multiplier when radar is active")]
    public float radarSpeedMultiplier = 1f;
    [Tooltip( "Reference to the bottom part of the rover" )]
    public Transform roverWheels;
    [Tooltip( "Reference to the top part of the rover" )]
    public Transform roverBody;
    [Tooltip( "Reference to the head part of the rover" )]
    public Transform roverHead;
    [Tooltip( "Reference to the player's name canvas" )]
    public Canvas nameCanvas;
    public float groundDistance = 0.3f;
    [Tooltip("slope check hint")]
    public float[] distanceAheadToTestHeight;
    [Tooltip("Speed at which collectibles are attracted")]
    public float magnetSpeed = 10f;

    [HideInInspector]
    public int entityId;

    [HideInInspector]
    public bool isUsingKeyboard;
    [HideInInspector]
    public Vector2 currentMovementInput;
    [HideInInspector]
    public float bodyRotationSpeed;
    [FormerlySerializedAs("speedModifier")] [HideInInspector]
    public float shootingSpeedModifier = 1f;
    [HideInInspector]
    public bool isDashing;
    public Vector3 currentVelocity => _currentSpeed * _currentDirection;
    
    [HideInInspector]
    public UnityEvent onDestroy = new();

    private float _currentSpeed;
    private Vector3 _currentDirection;
    private Vector3 _forward;
    private Quaternion _targetBodyRotation;
    private float _currentBoostEnergy;

    public PlayerHealth healthManager;
    public PlayerWeaponManager weaponManager;

    public PlayerRadar playerRadarInstance{ get; private set; }

    internal bool isInBuildingMode;
    public List<Building> buildings;

    internal float closestTrinketDistance { get; private set; }
    internal int   trinketCount          => _trinketsInRange.Count; 
    
    private readonly HashSet<UniqueCollectible> _trinketsInRange = new ();

    private bool isMoving{ get; set; }

    private bool _isBoosted;
    private bool _isGrounded;
    private float _boostedTimeLeft;
    private float _horizontalMovementInput;
    private float _verticalMovementInput;
    private float _currentFallingVelocity;
    private Vector3 _lastDirection;

    public  float      airborneDecelerationMultiplier = 0.45f;
    public  float      constructionRadius = 7f;
    private int        _currentBuildingIndex;
    private Building   _currentBuilding;
    private Vector3    _constructionPosition;
    private Vector2    _localConstructionPosition;
    private RaycastHit _raycastHit;
    private bool       _blockActions;
    private bool       _isHoldingRadar;
    private bool _canMove;

    internal delegate void ContextualInputEffect( PlayerController this_player );
    internal event ContextualInputEffect OnContextualInput;

    public          TargetPoint targetPoint{ get; private set; }
    
    public bool        isSearching => playerRadarInstance.isActive;

    public void RotateToDirection()
    {
        if( currentMovementInput.sqrMagnitude > 0.0f )
        {
            _targetBodyRotation = Quaternion.Euler( bodyRotationOffset ) * Quaternion.LookRotation( new Vector3( currentMovementInput.x, 0.0f, currentMovementInput.y ) );
        }
    }

    public void RotateToDirection( Vector3 direction )
    {
        _targetBodyRotation = Quaternion.Euler( bodyRotationOffset ) * Quaternion.LookRotation( direction );
    }

    public void UpdateRoverRotation()
    {

        roverBody.rotation = Quaternion.Lerp( roverBody.rotation, _targetBodyRotation, bodyRotationSpeed * Time.deltaTime );
        roverHead.rotation = _targetBodyRotation;
    }

#region Input events
    public void OnHorizontalMovements( InputAction.CallbackContext context )
    { 
        _horizontalMovementInput = context.ReadValue<float>();
    }

    public void OnVerticalMovements( InputAction.CallbackContext context )
    {
        _verticalMovementInput = context.ReadValue<float>();
    }

    public void OnMovementAbility(InputAction.CallbackContext context)
    {
        if( _blockActions )
        {
            return;
        }
        
        if( _isBoosted || !( _currentBoostEnergy > playerSettings.boostedTime * playerSettings.boostEnergyConsumptionRate ) )
        {
            return;
        }

        _isBoosted = true;
        _boostedTimeLeft = playerSettings.boostedTime;
    }

    public void OnSwitchRadarToWeapons(InputAction.CallbackContext context)
    {
        if (!isSearching)
            return;
        
        OnActivateRadar(context);
    }

    public void OnActivateRadar(InputAction.CallbackContext context)
    {
        if( playerRadarInstance.isRadarAToggle )
        {
            if (playerRadarInstance.doesActivationBlockPlayer)
            {
                _blockActions   = !context.canceled;
            }

            _isHoldingRadar = !context.canceled;
            if (!context.canceled)
            {
                StartCoroutine(ActivateRadarAfter());
            }
        }
        else
        {
            playerRadarInstance.Activate( transform.position );
        }
    }

    public void OnPodMoveOrder( InputAction.CallbackContext context )
    {
        if( _blockActions )
        {
            return;
        }
        
        PodController.Instance.CallToDestination( transform.position );
    }

    public void OnContextual(InputAction.CallbackContext context)
    {
        if( _blockActions )
        {
            return;
        }
        
        if( isInBuildingMode )
        {
            Build();
            return;
        }
        
        OnContextualInput?.Invoke(this);
    }

    public void OnBuildingMode( InputAction.CallbackContext context )
    {
        if( _blockActions || isSearching )
        {
            return;
        }
        
        SwitchBuilding( context.ReadValue<float>() );
    }
#endregion

#region Build
    private void SwitchBuilding( float axis_value )
    {
        if( Mathf.Abs( axis_value ) < 0.5f || buildings.Count == 0 )
        {
            return;
        }

        if( !isInBuildingMode )
        {
            isInBuildingMode = true;
        }
        else
        {
            RemoveCurrentBuildBlueprint();
            _currentBuilding = null;

            int count = buildings.Count;

            int next_building_index = _currentBuildingIndex + count;
            next_building_index += 1 * ( int )Mathf.Sign( axis_value );
            next_building_index %= count;
            _currentBuildingIndex = next_building_index;
        }

        _currentBuilding = Instantiate( buildings[ _currentBuildingIndex ], _constructionPosition, Quaternion.identity );
    }

    private void ShowConstructionPreview()
    {
        if( Vector3.Distance( weaponManager.currentAimInput, Vector3.zero ) > 0.1f )
        {
            _localConstructionPosition = weaponManager.currentAimInput.normalized * constructionRadius;
        }

        _constructionPosition = transform.position + new Vector3( _localConstructionPosition.x, 0f, _localConstructionPosition.y );

        _currentBuilding.SwitchMaterialColor( CanBuild() ? new Color( 0, 1, 0, 0.25f ) : new Color( 1, 0, 0, 0.25f ) );

        _currentBuilding.UpdateCoordinates( _constructionPosition );
    }

    private void Build()
    {
        if( !CanBuild() )
        {
            return;
        }

        // Removing resources
        InventoryManager inventory_manager = InventoryManager.Instance;

        for( var i = 0; i < ( int )CollectibleType.Count; i++ )
        {
            var missing_resource_on_player = 0;

            if( inventory_manager.playerCollectedResourcesCount[ i ] < _currentBuilding._neededResourcesCount[ i ] )
            {
                missing_resource_on_player = _currentBuilding._neededResourcesCount[ i ] - inventory_manager.playerCollectedResourcesCount[ i ];
                inventory_manager.playerCollectedResourcesCount[ i ] = 0;
            }

            inventory_manager.podCollectedResourcesCount[ i ] -= missing_resource_on_player;
        }

        Instantiate( _currentBuilding.Build, _currentBuilding.transform.position, Quaternion.identity );
    }

    private bool CanBuild()
    {
        if( _currentBuilding.isToFar || _currentBuilding.notOnFlat )
        {
            return false;
        }

        // Check if the object is within the good radius from the pod
        if( PodController.Instance && !PodController.Instance.CheckPointInPodRadius( _constructionPosition ) )
        {
            return false;
        }

        // Checking if there are enough resources
        InventoryManager inventory_manager = InventoryManager.Instance;

        if( !inventory_manager )
        {
            return false;
        }

        for( var i = 0; i < ( int )CollectibleType.Count; i++ )
        {
            if( inventory_manager.playerCollectedResourcesCount[ i ] + inventory_manager.podCollectedResourcesCount[ i ] < _currentBuilding._neededResourcesCount[ i ] )
            {
                return false;
            }
        }

        return true;
    }

    public void RemoveCurrentBuildBlueprint()
    {
        if( !_currentBuilding )
        {
            return;
        }

        Destroy( _currentBuilding.gameObject );
        _currentBuilding = null;
    }
#endregion

#region Radar
    private IEnumerator ActivateRadarAfter()
    {
        var time = 0f;

        while (time < playerRadarInstance.toggleHoldingTime)
        {
            if (!_isHoldingRadar || playerRadarInstance.onCooldown)
            {
                playerRadarInstance.InvokeActivationUpdate( 0f );
                yield break;
            }

            playerRadarInstance.InvokeActivationUpdate( time );
            time += Time.deltaTime;
            
            yield return null;
        }

        playerRadarInstance.InvokeActivationUpdate( playerRadarInstance.toggleHoldingTime );
        playerRadarInstance.Activate( transform.position );
        
        isInBuildingMode = false;
        _blockActions    = false;
    }

    public void StartDetectingTrinket(UniqueCollectible unique_collectible)
    {
        _trinketsInRange.Add(unique_collectible);
    }

    public void EndDetectingTrinket(UniqueCollectible unique_collectible)
    {
        _trinketsInRange.Remove(unique_collectible);
    }

    private void UpdateClosestTrinket()
    {
        float min_distance = int.MaxValue;
        foreach (UniqueCollectible unique_collectible in _trinketsInRange)
        {
            float distance = Vector3.Distance(unique_collectible.transform.position, transform.position);

            if (!( distance < min_distance )) continue;

            min_distance           = distance;
            closestTrinketDistance = distance / playerRadarInstance.rangeMinimumFlicker;
        }
    }
#endregion

    private void Awake()
    {
        weaponManager.playerController = this;

        playerRadarInstance                  = GetComponentInChildren<PlayerRadar>();
        playerRadarInstance.playerController = this;
        
        if( !isRadarFollowingPlayer )
        {
            playerRadarInstance.transform.parent = null;
        }

        _currentBoostEnergy = playerSettings.maxBoostEnergy;

        _localConstructionPosition = Vector3.right * constructionRadius;
        entityId = EntityManager.Instance.AddEntity( this );

        healthManager ??= GetComponent<PlayerHealth>();
        targetPoint ??= GetComponentInChildren<TargetPoint>();

        bodyRotationSpeed = playerSettings.bodyRotationSpeed;
    }

    private void Update()
    {
        if( PodController.Instance )
        {
            HUDManager.Instance.podInterface.UpdateDistanceText( Vector3.Distance( transform.position, PodController.Instance.transform.position ) );
        }

        if( isInBuildingMode )
        {
            ShowConstructionPreview();
        }

        if (isSearching)
        {
            UpdateClosestTrinket();
        }
    }

    private void FixedUpdate()
    {
        _isGrounded = false;
        currentMovementInput = _blockActions ? Vector2.zero : new(_horizontalMovementInput, _verticalMovementInput);

        #region Boost

        if( _boostedTimeLeft > 0 && _currentBoostEnergy > 0 )
        {
            _boostedTimeLeft -= Time.fixedDeltaTime;
            _currentBoostEnergy -= playerSettings.boostEnergyConsumptionRate * Time.fixedDeltaTime;
        }
        else
        {
            _boostedTimeLeft = 0;
            _isBoosted = false;
        }

        if( !_isBoosted )
        {
            _currentBoostEnergy += Time.fixedDeltaTime;
            _currentBoostEnergy = Mathf.Clamp( _currentBoostEnergy, 0, playerSettings.maxBoostEnergy );
        }

#endregion

        HUDManager.Instance.movementInterface.UpdateBoostGauge( _currentBoostEnergy, playerSettings.maxBoostEnergy );

        bool hit_ground = false;
        bool hit_hill = false;

        if( Physics.Raycast( transform.position + Vector3.up * groundDistance, -roverBody.up, out RaycastHit hitGround, 5f, Constants.groundLayerMask ) )
        {
            _isGrounded = hitGround.distance < groundDistance * 2.0f;

            hit_ground = true;
        }
        
        isMoving = currentMovementInput.magnitude > 0f;

        if( !_canMove )
        {
            isMoving = false;
            currentMovementInput = Vector3.zero;
        }
        
        if( isMoving || isDashing)
        {
            _lastDirection = _currentDirection;
            float speed_percent = 0f;
            
            if (!isDashing)
            {
                _currentSpeed += playerSettings.acceleration * Time.fixedDeltaTime;
                speed_percent = Mathf.Max(Mathf.Abs(currentMovementInput.x), Mathf.Abs(currentMovementInput.y));
                _forward = _currentDirection = new Vector3( currentMovementInput.x, 0.0f, currentMovementInput.y );
                _currentDirection.Normalize(); 
            }
            else
            {
                speed_percent = 1f;
            }
            
            Vector3 hit_normal = Vector3.up;
            float angle_ratio = 0f;

            Vector3 side = Vector3.Cross(_currentDirection, roverBody.up);

            hit_hill = true;
            // Raycast forward (according to direction) to detect slope (takes priority on previous raycast to go up slopes)
            if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, _currentDirection, out RaycastHit hitHill, 1.5f, Constants.groundLayerMask))
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, -_currentDirection, out hitHill, 1.5f, Constants.groundLayerMask))
                {
                    if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, side, out hitHill, 1.5f, Constants.groundLayerMask))
                    {
                        if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, -side, out hitHill, 1.5f, Constants.groundLayerMask))
                        {
                            hit_hill = false;
                        }
                    }
                }
            } 
            
            if (hit_hill)
            {
                float ground_angle = Vector3.Angle( hitHill.normal, Vector3.up );
                angle_ratio = ground_angle / playerSettings.maximumAngle;
                
                if( angle_ratio <= 1f )
                {
                    hit_normal = hitHill.normal;
                    _currentDirection = Vector3.ProjectOnPlane( _currentDirection, hit_normal ).normalized;
                }
                else
                {
                    angle_ratio = 0f;
                }
            }

            float maximum_speed_percentage = playerSettings.speedLossRatio.Evaluate( angle_ratio );

            if (!isDashing)
            {
                _currentSpeed = Mathf.Clamp( _currentSpeed, 0.0f, playerSettings.maximumSpeed * (isSearching ? radarSpeedMultiplier : 1f) * ( _isBoosted ? playerSettings.boostedSpeedBonusMultiplier : 1 ) * maximum_speed_percentage );
            }
            playerRigidbody.velocity = _currentDirection * (_currentSpeed * speed_percent * shootingSpeedModifier);
            
            if( Physics.Raycast( transform.position + Vector3.up, Vector3.down, out RaycastHit ground_hit, 2.0f, Constants.groundLayerMask ) )
            {
                roverWheels.rotation = MathHelper.Smooth( roverWheels.rotation, Quaternion.LookRotation( _currentDirection, ground_hit.normal ) * Quaternion.Euler( wheelsRotationOffset ), wheelRotationSmoothSharpness );
            }
        }
        else
        {
            _currentSpeed -= playerSettings.deceleration * (_isGrounded ? 1 : airborneDecelerationMultiplier) * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Max( _currentSpeed, 0.0f );
            playerRigidbody.velocity = _currentDirection * (_currentSpeed * ( _isBoosted ? playerSettings.boostedSpeedBonusMultiplier : 1 ) * shootingSpeedModifier);
        }

        if( !_isGrounded )
        {
            _currentFallingVelocity += playerSettings.gravityForce * Time.fixedDeltaTime;

            if( hit_hill )
            {
                _currentFallingVelocity = playerSettings.gravityForce * Time.fixedDeltaTime;
            }
            
            if( _canMove )
            {
                _canMove = hit_ground;
            }
        }
        else
        {
            _currentFallingVelocity = playerSettings.gravityForce * Time.fixedDeltaTime;
            _canMove = true;
        }
        
        _currentFallingVelocity = Mathf.Clamp( _currentFallingVelocity, 0.0f, playerSettings.terminalVelocity );
        playerRigidbody.velocity += Vector3.down * _currentFallingVelocity;
    }

    private void OnTriggerEnter( Collider other )
    {
        if( other.CompareTag( "Collectible" ) )
        {
            other.GetComponent<Collectible>().TryPickUp(magnetSpeed, this);
        }
    }

    private void OnTriggerExit( Collider other )
    {
        if( other.CompareTag( "Collectible" ) )
        {
            other.GetComponent<Collectible>().ForgetPickUp(this);
        }
    }

    private void OnValidate()
    {
        if (!playerRadarInstance)
        {
            return;
        }

        Transform radar_transform = playerRadarInstance.transform;
        if( isRadarFollowingPlayer )
        {
            radar_transform.parent   = transform;
            radar_transform.position = transform.position;
        }
        else
        {
            playerRadarInstance.isRadarAToggle = false;
            radar_transform.parent             = null;
        }
    }

    public void ForwardDash(Vector3 direction, float time, float speed)
    {
        StartCoroutine(ForwardDashCoroutine());
        
        IEnumerator ForwardDashCoroutine()
        {
            float last_speed = _currentSpeed;
            isDashing = true;
            _forward = _currentDirection = new Vector3(direction.x, 0.0f, direction.z);
            _currentSpeed = speed;
            while( time > 0f)
            {
                time -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            _currentSpeed = last_speed;
            isDashing = false;
        }
    }
    

    private void OnDestroy()
    {
        EntityManager.Instance.RemoveEntity( this );
    }
}
