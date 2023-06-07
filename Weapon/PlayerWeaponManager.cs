using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static TriggerType;
using static InstructionType;

public enum TriggerType
{
    RIGHT_SHOULDER,
    LEFT_SHOULDER,
    RIGHT_TRIGGER,
    LEFT_TRIGGER
}

public enum InstructionType
{
    Place,
    Grab,
    Explode
}

public class PlayerWeaponManager : MonoBehaviour
{
    [Header( "Reference" )]
    [Tooltip( "List of Weapon and their anchorPoint (Reference where to instantiate the weapon)" )]
    public List<WeaponTrigger> weaponList;
    private Dictionary<TriggerType, ModularWeapon> _modularWeaponDictionary = new();

    [HideInInspector]
    public PlayerController playerController;

    internal Vector2 currentAimInput;
    private float _horizontalAimInput;
    private float _verticalAimInput;
    private bool _isAiming;

    private Vector3 _currentDirection;

    public bool updateUI{ get; set; }


    #region Input Callbacks

    public void OnHorizontalAim( InputAction.CallbackContext context )
    {
        var value_f = context.ReadValue<float>();

        if( !playerController.isUsingKeyboard )
        {
            if( Mathf.Abs( value_f ) > 1f )
            {
                return;
            }
        }
        else
        {
            value_f = Mathf.Max( value_f, 0f );
            value_f = value_f.Map(0f, Screen.width, -1f, 1f );
        }

        _horizontalAimInput = value_f;
    }

    public void OnVerticalAim( InputAction.CallbackContext context )
    {
        var value_f = context.ReadValue<float>();

        if( !playerController.isUsingKeyboard )
        {
            if( Mathf.Abs( value_f ) > 1f )
            {
                return;
            }
        }
        else
        {
            value_f = Mathf.Max( value_f, 0f );
            value_f = value_f.Map(0f, Screen.height, -1f, 1f );
        }

        _verticalAimInput = value_f;
    }

    public void OnLeftTrigger(InputAction.CallbackContext context)
    {
        if( playerController.isSearching )
        {
            return;
        }

        playerController.RemoveCurrentBuildBlueprint();
        playerController.isInBuildingMode = false;

        SwitchWeaponActivation(context, LEFT_TRIGGER);
    }

    public void OnRightTrigger(InputAction.CallbackContext context)
    {
        if( playerController.isSearching )
        {
            return;
        }
        
        playerController.RemoveCurrentBuildBlueprint();
        playerController.isInBuildingMode = false;
        
        SwitchWeaponActivation(context, RIGHT_TRIGGER);
    }
    
    public void OnLeftShoulder(InputAction.CallbackContext context)
    {        
        if( playerController.isSearching )
        {
            return;
        }
        
        playerController.RemoveCurrentBuildBlueprint();
        playerController.isInBuildingMode = false;
        
        SwitchWeaponActivation(context, LEFT_SHOULDER);
    }

    public void OnRightShoulder(InputAction.CallbackContext context)
    {
        if( playerController.isSearching )
        {
            return;
        }
        
        playerController.RemoveCurrentBuildBlueprint();
        playerController.isInBuildingMode = false;
        
        SwitchWeaponActivation(context, RIGHT_SHOULDER);
    }

    private void SwitchWeaponActivation(InputAction.CallbackContext context, TriggerType type, bool reverse = false)
    {
        if (context.phase is not (InputActionPhase.Performed or InputActionPhase.Canceled))
            return;
        
        var is_pressed = context.canceled == reverse;
        if (_modularWeaponDictionary.TryGetValue(type, out ModularWeapon weapon))
        {
            if (is_pressed)
            {
                weapon.weaponExecutionGroup.OnPress();
            }
            else
            {
                weapon.weaponExecutionGroup.OnRelease();
            }
        }
    }

    public void SwitchAllWeaponOff(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            return;
        }

        foreach (TriggerType type in Enum.GetValues(typeof(TriggerType)))
        {
            SwitchWeaponActivation(context, type, true);
        }
    }
    
#endregion

    private void Awake()
    {
        foreach (var item in weaponList.Where(item => item.modularWeapon != null))
        {
            ModularWeapon modularWeapon = Instantiate( item.modularWeapon, item.anchorPoint );
            modularWeapon.triggerType = item.triggerType;
            _modularWeaponDictionary.TryAdd(item.triggerType, modularWeapon);
            
            foreach( MeshRenderer mesh_renderer in modularWeapon.GetComponentsInChildren<MeshRenderer>() )
            {
                mesh_renderer.material = item.teamMaterial;
            }
    	}
    }

    private void Update()
    {
        UpdateModelRotation();

        if( updateUI )
        {
            UpdateWeaponUI();
        }
        UpdateWeaponIconColor();

        _currentDirection = _isAiming ? new Vector3( _horizontalAimInput, 0.0f, _verticalAimInput ).normalized : playerController.roverBody.forward;

        foreach (KeyValuePair<TriggerType, ModularWeapon> weapon in _modularWeaponDictionary.Where(weapon => weapon.Value.isRotating))
        {
            weapon.Value.currentDirection = _currentDirection;
            Quaternion new_rotation = Quaternion.LookRotation( _currentDirection );
            weapon.Value.UpdateRotation( new_rotation );
        }
    }

    private void UpdateWeaponUI()
    {
        foreach (KeyValuePair<TriggerType, WeaponInterface> w in HUDManager.Instance.weaponInterfaceDictionary)
        {
            if (!_modularWeaponDictionary.ContainsKey(w.Key))
                return;
            
            if (!_modularWeaponDictionary[w.Key].isReloading && _modularWeaponDictionary[w.Key].isHoldingShoot)
            {
                w.Value.weaponIcon.color = w.Value.weaponIconFireColor;
            }
            
            if((float)_modularWeaponDictionary[w.Key].overheatTime.GetValue() > 0f)
            {
                w.Value.weaponGauge.color = w.Value.overHeatGradient.Evaluate( _modularWeaponDictionary[w.Key].gaugeFillAmount );
                w.Value.weaponClip.text = "" + ( int )( _modularWeaponDictionary[w.Key].gaugeFillAmount * 100 ) + "%";
            }
            else if((float)_modularWeaponDictionary[w.Key].reloadTime.GetValue() > 0f)
            {
                w.Value.weaponClip.text = _modularWeaponDictionary[w.Key].isReloading ? "reloading" : _modularWeaponDictionary[w.Key].currentLoadAsString;
            }

            if ((float)_modularWeaponDictionary[w.Key].plantAmmoDetectionRadius.GetValue() > 0f)
            {
                w.Value.weaponClip.text = _modularWeaponDictionary[w.Key].currentLoadAsString;
            }
            
            w.Value.weaponGauge.fillAmount = _modularWeaponDictionary[w.Key].gaugeFillAmount;
        }
    }
    
    private void UpdateWeaponIconColor()
    {
        foreach (KeyValuePair<TriggerType, WeaponInterface> w in HUDManager.Instance.weaponInterfaceDictionary)
        {
            if (!_modularWeaponDictionary.ContainsKey(w.Key))
                return;
            
            w.Value.weaponIcon.color = Color.Lerp( w.Value.weaponIcon.color, _modularWeaponDictionary[w.Key].weaponIconCurrentBaseColor, w.Value.fireColorLerpStrength * Time.deltaTime );
        }
    }

    private void UpdateModelRotation()
    {
        currentAimInput = new Vector2( _horizontalAimInput, _verticalAimInput );
        _isAiming = currentAimInput.magnitude > 0f;

        if( _isAiming )
        {
            playerController.RotateToDirection( new Vector3( currentAimInput.x, 0.0f, currentAimInput.y ) );
        }
        else
        {
            playerController.RotateToDirection();
        }

        playerController.UpdateRoverRotation();
    }
    
    
    public void UpdateInstructionText(InstructionType instruction, TriggerType trigger_type)
    {
        switch (instruction)
        {
            case Place :
                HUDManager.Instance.weaponInterfaceDictionary[trigger_type].instructionText.text =
                    "Place mine";
                break;
            case Grab :
                HUDManager.Instance.weaponInterfaceDictionary[trigger_type].instructionText.text =
                    "Grab mine";
                break;
            case Explode :
                HUDManager.Instance.weaponInterfaceDictionary[trigger_type].instructionText.text =
                    "Explode all mines";
                break;
        }
    }

    public void UpdateWeaponClipText(TriggerType trigger_type)
    {
        HUDManager.Instance.weaponInterfaceDictionary[trigger_type].weaponClip.text =
            _modularWeaponDictionary[trigger_type].currentLoadAsString;
    }
}

[System.Serializable]
public class WeaponTrigger
{
    public TriggerType triggerType;
    public ModularWeapon modularWeapon;
    public Transform anchorPoint;
    public Material teamMaterial;
    public PlayerWeaponIK weaponDoor;
}
