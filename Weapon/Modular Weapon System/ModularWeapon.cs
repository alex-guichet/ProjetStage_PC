using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Den.Tools;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class ModularWeapon : MonoBehaviour
{ 
    [Tooltip( "Hit effects applied when an ammo hit an enemy" )]
    public List<HitEffectType> hitEffectList;
    [Tooltip( "Move effects applied on the Ammo" )]
    public List<MoveEffectType> moveEffectList;
    [Tooltip( "Instantiation effects applied on the weapon, eg : Where and how the ammo is instantiated" )]
    public List<InstantiationEffectType> instantiationEffectList;
    [Tooltip( "Execution effect applied on the current instantiation effects selected" )]
    public List<ExecutionBehaviorEffect> executionEffectList;
    [Tooltip( "Constraint effect applied on the execution effects, can stop or resume the execution" )]
    public List<ContraintEffectType> constraintEffectList;
    [Tooltip( "Reference to the projectile that will be shot" )]
    public GameObject bulletPrefab;
    [Tooltip( "Feedbacks when shooting" )]
    public MMF_Player ShootingFeedbacks;
    [Tooltip( "Does this weapon rotates ?" )]
    public bool isRotating = true;
    [Tooltip( "VFX to play when shooting" )]
    public VisualEffect[] shootVFX;
    [Tooltip( "Where to spawn the ammo" )]
    public Transform bulletSpawnPoint;
    [Tooltip( "Weapon Charging Bar" )]
    public RadialLoadingBar chargingBar;
    [Tooltip( "Landing Target Prefab used for the Target execution" )]
    public GameObject landingTargetPrefab;
    [Tooltip( "Is it possible to cancel Charges ?" )]
    public bool canCancelCharge;
    [Tooltip( "Layer mask of the enemy" )]
    public LayerMask enemyMask;
    [Tooltip( "Damage points made by the rover to the enemies" )]
    public FloatStat damage;
    [Tooltip( "Range of the ammunition" )]
    public FloatStat range;
    [Tooltip( "Move speed of the bullets" )]
    public FloatStat moveSpeed;
    [Tooltip( "Dispersion angle of the bullets when they are instantiated" )]
    public FloatStat dispersionAngle;
    [Tooltip( "Fire rate of the weapon" )]
    public FloatStat fireRate;
    [Tooltip( "Cooling delay of the weapon" )]
    public FloatStat coolingDelayTime;
    [Tooltip( "Time it takes for the weapon to overheat " )]
    public FloatStat overheatTime;
    [Tooltip( "Threshold percentage when the fireRate starts decreasing" )]
    public FloatStat decreaseThresholdPercentage;
    [Tooltip( "Number of ammunition in the weapon" )]
    public FloatStat clipSize;
    [Tooltip( "Time it takes the weapon to reload" )]
    public FloatStat reloadTime;
    [Tooltip( "Time it takes the charge the weapon before shooting" )]
    public FloatStat chargeTime;
    [Tooltip( "Start angle of the attack" )]
    public FloatStat attackAngle;
    [Tooltip( "Time it takes before being able to shoot with the weapon")]
    public FloatStat minimumTimeBeforeShot;
    [Tooltip( "Minimum kill count to get a range boost on the weapon")]
    public FloatStat minimumKillCount;
    [Tooltip( "Maximum kill count to get the best range boost on the weapon")]
    public FloatStat maximumKillCount;
    [Tooltip( "Minimum Time between each shots")]
    public FloatStat minimumTimeBetweenShot;
    [Tooltip( "Maximum Time between each shots")]
    public FloatStat maximumTimeBetweenShot;
    [Tooltip( "Duration of the forward dash")]
    public FloatStat dashDuration;
    [Tooltip( "Distance travelled by the rover during the dash")]
    public FloatStat dashDistance;
    [Tooltip( "Minimum attack count to trigger the spin attack")]
    public FloatStat minimumAttackCount;
    [Tooltip( "Speed of the target")]
    public FloatStat targetSpeed;
    [Tooltip( "Minimum attack count to trigger the spin attack")]
    public FloatStat targetMinimumDistance;
    [Tooltip( "Width of the laser")]
    public FloatStat laserWidth;
    [Tooltip( "Multiplicator applied on the rotation speed of the rover")]
    public FloatStat roverRotationMultiplicator;
    [Tooltip( "Duration of the shooting")]
    public FloatStat shootingTime;
    [Tooltip( "Speed of the rover multiplcated by this value while it's shooting")]
    public FloatStat roverShootingSpeedModifier;
    [Tooltip( "Radius of the bullet explosion")]
    public FloatStat explosionRadius;
    [Tooltip( "Radius of the mine detector")]
    public FloatStat plantAmmoDetectionRadius;
    [Tooltip( "Time it takes for the mine to explode")]
    public FloatStat mineActivationTime;
    [Tooltip( "Rotation delta between 2 bullets instantiation")]
    public FloatStat rotationDelta;
    
    public virtual bool isReloading{ get; set; }
    internal bool isHoldingShoot;
    internal bool isShooting;
    internal bool hasStoppedShooting;
    internal bool executeOnHoldBehaviors;

    public float gaugeFillAmount{ get; set; }
    public Color weaponIconCurrentBaseColor;
    
    public Dictionary<WeaponEffectType, List<WeaponEffect>> weaponEffectsDictionary = new();
    public WeaponExecutionGroup weaponExecutionGroup = new();


    public string currentLoadAsString
    { 
        get
        {

            if (weaponEffectsDictionary[WeaponEffectType.Instantiation]
                    .FindIndex(x => x is PlantInstantiation) != -1)
            {
                return (int)currentLoad + " / " + clipSize.GetValue();
            }

            if (weaponEffectsDictionary[WeaponEffectType.Constraint]
                    .FindIndex(x => x is ReloadConstraint) != -1)
            {
                return (int)currentLoad + " / " + clipSize.GetValue();
            }
            
            return "";
        }
    }
    [HideInInspector] public List<PlayerModularAmmo> currentGrabbableAmmo = new();
    [HideInInspector] public List<PlayerModularAmmo> currentSetupAmmo = new();
    [HideInInspector] public PlayerWeaponManager playerWeaponManager;
    [HideInInspector] public Vector3 currentDirection;
    [HideInInspector] public float reloadTimer;
    [HideInInspector] public float currentLoad;
    [HideInInspector] public TriggerType triggerType;
    [HideInInspector] public PlayerController parentPlayerController;
    [HideInInspector] public int currentlyUsedShootVFX;
    [HideInInspector] public bool explodeAmmo;
    [HideInInspector] public int enemyKillCount;
    [HideInInspector] public float targetRange;

    private Camera _playerCamera;
    private bool _isChargingBarNotNull;
    private bool _isPlayerCameraNotNull;

    public virtual void UpdateRotation( Quaternion in_new_rotation )
    {
        transform.rotation = in_new_rotation;
    }

    public virtual void UpdateWeaponIconColor()
    {
    }
    
    public void UpdateWeaponEffect()
    {
        currentLoad = (float)clipSize.GetValue();
        weaponEffectsDictionary.Clear();
        
        List<WeaponEffect> hit_effect_list = new();
        foreach (var h in hitEffectList)
        {
            hit_effect_list.Add(GetWeaponEffectWithString(h.ToString()));
        }
        weaponEffectsDictionary.TryAdd(WeaponEffectType.Hit, hit_effect_list);
        
        List<WeaponEffect> move_effect_list = new();
        foreach (var m in moveEffectList)
        {
            move_effect_list.Add(GetWeaponEffectWithString(m.ToString()));
        }
        weaponEffectsDictionary.TryAdd(WeaponEffectType.Move, move_effect_list);
        
        List<WeaponEffect> instantiation_effect_list = new();
        foreach (var i in instantiationEffectList)
        {
            instantiation_effect_list.Add(GetWeaponEffectWithString(i.ToString()));
        }
        weaponEffectsDictionary.TryAdd(WeaponEffectType.Instantiation, instantiation_effect_list);

        List<WeaponEffect> constraint_effect_list = new();
        foreach (var c in constraintEffectList)
        {
            constraint_effect_list.Add(GetWeaponEffectWithString(c.ToString()));
        }
        weaponEffectsDictionary.TryAdd(WeaponEffectType.Constraint, constraint_effect_list);
        
        List<WeaponEffect> execution_effect_list = new();
        ExecutionEffect execution_effect = new(null, null, null);
        foreach (var e in executionEffectList)
        {
            switch (e.executionEffectType)
            {
                case ExecutionEffectType.Single:
                    execution_effect = new DirectExecution(this,instantiation_effect_list,
                        GetWeaponEffectList(e.hitEffectType, e.moveEffectType));
                    break;
                case ExecutionEffectType.FireRate:
                    execution_effect = new FireRateExecution(this, instantiation_effect_list,
                        GetWeaponEffectList(e.hitEffectType, e.moveEffectType));
                    break;
                case ExecutionEffectType.AmmoCharge:
                    execution_effect = new AmmoChargeExecution(this, instantiation_effect_list,
                        GetWeaponEffectList(e.hitEffectType, e.moveEffectType));
                    break;
                case ExecutionEffectType.ChainRotation:
                    execution_effect = new ChainRotationExecution(this, instantiation_effect_list,
                        GetWeaponEffectList(e.hitEffectType, e.moveEffectType));
                    break;
                case ExecutionEffectType.TimeCharge:
                    execution_effect = new TimeChargeExecution(this, instantiation_effect_list,
                        GetWeaponEffectList(e.hitEffectType, e.moveEffectType));
                    break;
                case ExecutionEffectType.Target:
                    execution_effect = new TargetExecution(this, instantiation_effect_list,
                        GetWeaponEffectList(e.hitEffectType, e.moveEffectType));
                    break;
            }
            execution_effect_list.Add(execution_effect);
        }

        weaponExecutionGroup.ExecutionEffectList = execution_effect_list.Cast<ExecutionEffect>().ToList();
        weaponEffectsDictionary.TryAdd(WeaponEffectType.Execution, execution_effect_list);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ModularWeapon))]
    public class UpdateWeaponEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ModularWeapon modular_weapon = (ModularWeapon)target;
            modular_weapon.UpdateWeaponEffect();
            
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((ModularWeapon)target), typeof(ModularWeapon), false);
            GUI.enabled = true;
            EditorGUILayout.LabelField("Effects Settings", EditorStyles.boldLabel);
            SerializedProperty hit_effect = serializedObject.FindProperty("hitEffectList");
            EditorGUILayout.PropertyField(hit_effect);
            SerializedProperty move_effect = serializedObject.FindProperty("moveEffectList");
            EditorGUILayout.PropertyField(move_effect);
            SerializedProperty instantiation_effect = serializedObject.FindProperty("instantiationEffectList");
            EditorGUILayout.PropertyField(instantiation_effect);
            SerializedProperty execution_effect = serializedObject.FindProperty("executionEffectList");
            EditorGUILayout.PropertyField(execution_effect);
            SerializedProperty constraint_effect = serializedObject.FindProperty("constraintEffectList");
            EditorGUILayout.PropertyField(constraint_effect);
            
            EditorGUILayout.LabelField("Weapon fields", EditorStyles.boldLabel);
            Type modular_weapon_type = typeof(ModularWeapon);
            FieldInfo[] modular_weapon_fields_info = modular_weapon_type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            List<string> added_fields = new();
            foreach (var weapon_effect_list in modular_weapon.weaponEffectsDictionary)
            {
                foreach (var weapon_effect in weapon_effect_list.Value)
                {
                    Type weapon_effect_type = weapon_effect.GetType();
                    FieldInfo[] weapon_effect_fields_info = weapon_effect_type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                
                    foreach (var modular_weapon_field in Enumerable.Where(modular_weapon_fields_info, modular_weapon_field => weapon_effect_fields_info.Find(info => info.Name == modular_weapon_field.Name) != -1))
                    {
                        if (added_fields.FindIndex(x => x == modular_weapon_field.Name) != -1) 
                            continue;
                        
                        SerializedProperty stat_field = serializedObject.FindProperty(modular_weapon_field.Name);
                        EditorGUILayout.PropertyField(stat_field);
                        added_fields.Add(modular_weapon_field.Name);
                    }
                }
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Universal fields", EditorStyles.boldLabel);
            SerializedProperty bullet_prefab = serializedObject.FindProperty("bulletPrefab");
            EditorGUILayout.PropertyField(bullet_prefab);
            SerializedProperty shooting_feedback = serializedObject.FindProperty("ShootingFeedbacks");
            EditorGUILayout.PropertyField(shooting_feedback);
            SerializedProperty is_rotating = serializedObject.FindProperty("isRotating");
            EditorGUILayout.PropertyField(is_rotating);
            SerializedProperty enemy_mask = serializedObject.FindProperty("enemyMask");
            EditorGUILayout.PropertyField(enemy_mask);
            SerializedProperty bullet_spawn_point = serializedObject.FindProperty("bulletSpawnPoint");
            EditorGUILayout.PropertyField(bullet_spawn_point);
            SerializedProperty weapon_current_base_color = serializedObject.FindProperty("weaponIconCurrentBaseColor");
            EditorGUILayout.PropertyField(weapon_current_base_color);
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif  
    
    private List<WeaponEffect> GetWeaponEffectList(HitEffectType[] hit_effect_type_list = null, MoveEffectType[] move_effect_type_list = null)
    {
        List<WeaponEffect> weapon_effect_list = new();
        if (hit_effect_type_list != null)
        {
            foreach (var effect in hit_effect_type_list)
            {
                weapon_effect_list.Add(GetWeaponEffectWithString(effect.ToString()));
            }
        }
        
        if (move_effect_type_list != null)
        {
            foreach (var effect in move_effect_type_list)
            {
                weapon_effect_list.Add(GetWeaponEffectWithString(effect.ToString()));
            }
        }
        return weapon_effect_list;
    }

    private WeaponEffect GetWeaponEffectWithString(string weapon_effect_type)
    {
        WeaponEffect weapon_effect = new(null);
        switch (weapon_effect_type)
        {
            case nameof(HitEffectType.Normal):
                weapon_effect = new NormalHit(this);
                break;
            case nameof(HitEffectType.Explosion):
                weapon_effect = new ExplosionHit(this);
                break;
            case nameof(MoveEffectType.Straight):
                weapon_effect = new StraightMove(this);
                break;
            case nameof(MoveEffectType.Guided):
                weapon_effect = new GuidedMove(this);
                break;
            case nameof(MoveEffectType.Parabolic):
                weapon_effect = new ParabolicMove(this);
                break;
            case nameof(InstantiationEffectType.AutoAimShoot):
                weapon_effect = new AutoAimShotInstantiation(this);
                break;
            case nameof(InstantiationEffectType.Shoot):
                weapon_effect = new ShotInstantiation(this);
                break;
            case nameof(InstantiationEffectType.Plant):
                weapon_effect = new PlantInstantiation(this);
                break;
            case nameof(InstantiationEffectType.FixedToWeapon):
                weapon_effect = new FixedToWeaponInstantiation(this);
                break;;
            case nameof(InstantiationEffectType.ZigZag):
                weapon_effect = new ZigzagInstantiation(this);
                break;
            case nameof(ContraintEffectType.RestrictiveOverheat):
                weapon_effect = new RestrictiveOverheatConstraint(this);
                break;
            case nameof(ContraintEffectType.PermissiveOverheat):
                weapon_effect = new PermissiveOverheatConstraint(this);
                break;
            case nameof(ContraintEffectType.Reload):
                weapon_effect = new ReloadConstraint(this);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return weapon_effect;
    }

    private void CalculateStatsValue()
    {
        Type modular_weapon_type = typeof(ModularWeapon);
        FieldInfo[] modular_weapon_fields_info = modular_weapon_type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var weapon_effect_list in weaponEffectsDictionary)
        {
            foreach (var weapon_effect in weapon_effect_list.Value)
            {
                Type weapon_effect_type = weapon_effect.GetType();
                FieldInfo[] weapon_effect_fields_info = weapon_effect_type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var modular_weapon_field in Enumerable.Where(modular_weapon_fields_info, modular_weapon_field => weapon_effect_fields_info.Find(info => info.Name == modular_weapon_field.Name) != -1))
                {
                    foreach (var weapon_effect_field in weapon_effect_fields_info)
                    {
                        if (weapon_effect_field.Name != modular_weapon_field.Name)
                            continue;
                        
                        if (weapon_effect_field.FieldType != typeof(float))
                            continue;
                        
                        FloatStat float_stat = (FloatStat)modular_weapon_field.GetValue(this);
                        AggregateStatValue(float_stat);
                        weapon_effect_field.SetValue(weapon_effect, float_stat.calculatedValue);
                    }
                }
            }
        }
    }
    
    private void AggregateStatValue(FloatStat float_stat)
    {
        if (float_stat.isPercentage)
        {
            float upgrade_ratio = 0f;
            if (float_stat.statUpgrades != null)
            {
                foreach (var upgrade in float_stat.statUpgrades)
                {
                    upgrade_ratio += upgrade - 1f;
                }
            }
            upgrade_ratio += 1f;
            float_stat.calculatedValue = float_stat.value * upgrade_ratio;
        }
        else
        {
            float added_stats = 0f;
            foreach (var upgrade in float_stat.statUpgrades)
            {
                added_stats += upgrade;
            }
            float_stat.calculatedValue = float_stat.value + added_stats;
        }
    }

    private void InitializeVariables()
    {
        foreach (KeyValuePair<WeaponEffectType, List<WeaponEffect>> w in weaponEffectsDictionary)
        {
            foreach (WeaponEffect e in w.Value)
            {
                e.InitializeVariables();
            }
        }
    }
    
    private void InitializeEffectParameters()
    {
        if (instantiationEffectList.FindIndex( x => x == InstantiationEffectType.Plant) != -1)
        {
            if (!playerWeaponManager)
                return;
            
            playerWeaponManager.UpdateInstructionText(InstructionType.Place, triggerType);
        }
    }
    
    public virtual void Awake()
    {
        _isChargingBarNotNull = chargingBar != null;
        parentPlayerController = transform.GetComponentInParent<PlayerController>();
        playerWeaponManager = parentPlayerController.transform.GetComponent<PlayerWeaponManager>();
    }

    public virtual void Start()
    {
        UpdateWeaponEffect();
        CalculateStatsValue();
        InitializeVariables();
        InitializeEffectParameters();
        _playerCamera = parentPlayerController.transform.parent.GetComponentInChildren<Camera>();
    }
    
    private void Update()
    {
        weaponExecutionGroup.OnUpdate();
        
        if(constraintEffectList.Count > 0)
        {
            foreach( var constraint_effect in weaponEffectsDictionary[ WeaponEffectType.Constraint] )
            {
                constraint_effect.Execute();
            }
        }
        
        if (_isChargingBarNotNull)
        {
            chargingBar.transform.LookAt(_playerCamera.transform.position,Vector3.up);
        }
    }
}
