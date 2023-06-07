using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;



public class WeaponExecutionGroup
{
    public List<ExecutionEffect> ExecutionEffectList = new();

    public void OnPress()
    {
        foreach (var e in ExecutionEffectList)
        {
            e.OnPress();
        }
    }
    
    public void OnRelease()
    {
        foreach (var e in ExecutionEffectList)
        {
            e.OnRelease();
        }
    }

    public void OnUpdate()
    {
        foreach (var e in ExecutionEffectList)
        {
            e.OnUpdate();
        }
    }
}
