using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerModularBullet : PlayerModularAmmo
{
    protected override void Start()
    {
        base.Start();
        TimeToLive = (float)modularWeapon.range.GetValue() / (float)modularWeapon.moveSpeed.GetValue();
    }
}
