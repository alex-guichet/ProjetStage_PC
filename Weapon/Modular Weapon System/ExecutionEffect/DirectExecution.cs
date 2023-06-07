using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DirectExecution : ExecutionEffect
{
    public DirectExecution(ModularWeapon modular_weapon, List<WeaponEffect> instantiationEffectList, List<WeaponEffect> addedEffectList) : base(modular_weapon, instantiationEffectList, addedEffectList)
    {
    }
    
    
    public override void InitializeVariables()
    {
        base.InitializeVariables();
    }
    
    public override void OnPress()
    {
        foreach(var instantiation_effect in InstantiationEffectList)
        {
            instantiation_effect.Execute();
        }
    }
    
    public override void OnRelease()
    {
        
    }

    public override void OnUpdate()
    {
        
    }
}
