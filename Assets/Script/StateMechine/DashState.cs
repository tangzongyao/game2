﻿using System;
using System.Collections;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;
using static PlayerEntity;

public class DashState : BaseState
{
    public DashState(PlayerEntity p):base(p) 
    { }
    public override State state => State.Dash;

    public override bool haveCoroutine => true;

    
    public override void OnEnd()
    {
        //pe.rd.gravityScale = PhySet.Gravity;
    }

    private State returnState;
    public override void OnEnter()
    {
        returnState = state;
        pe.dashDir = pe.input_move;
        pe.dashes--;
        pe.dashStartedOnGround = pe.onGround;
        pe.dashCooldownTimer = TimeSet.DashCooldown;
        if (pe.dashDir == Vector2.zero)
            pe.dashDir = new Vector2((float)pe.facing, 0);
        pe.dashAttackTimer = TimeSet.DashAttackTime;
    }

    public override State Update()
    {
        if (pe.input.GamePlay.Climb.IsPressed() && pe.CastCheckCollider(Vector2.zero, Vector2.right * (int)pe.facing))
        {
            return State.Climb;
        }

        if (pe.dashDir.y == 0)
        {
            if (pe.input.GamePlay.Jump.WasPressedThisFrame()&& pe.jumpGraceTimer > 0)
            {
                pe.SuperJump();
                return State.Normal;
            }
        }
        else if(pe.dashDir.y==1&& pe.dashDir.x==0)
        {
            if(pe.input.GamePlay.Jump.WasPressedThisFrame())
            {
                if(pe.WallJumpCheck((int)pe.facing*-1))
                {
                    pe.SuperWallJump((int)pe.facing);
                    return State.Normal;
                }
                else if (pe.WallJumpCheck((int)pe.facing))
                {
                    pe.SuperWallJump((int)pe.facing * -1);
                    return State.Normal;
                }
            }
        }
        else
        {
            if (pe.input.GamePlay.Jump.WasPressedThisFrame())
            {
                if (pe.WallJumpCheck((int)pe.facing * -1))
                {
                    pe.WallJump((int)pe.facing);
                    return State.Normal;
                }
                else if (pe.WallJumpCheck((int)pe.facing))
                {
                    pe.WallJump((int)pe.facing * -1);
                    return State.Normal;
                }
            }
        }

        return returnState;
    }
    public override IEnumerator Coroutine()
    {
        Vector2 spdDir = pe.dashDir.normalized;
        Vector2 newSpeed = spdDir * pe.DashSpeed;

        if(spdDir.x==Mathf.Sign(pe.speed.x)&& Mathf.Abs(newSpeed.x)< Mathf.Abs(pe.speed.x))
            newSpeed.x=pe.speed.x;
        pe.speed=newSpeed;

        pe.rd.gravityScale = 0;
        yield return new WaitTime(TimeSet.DashTime);

        if(spdDir.y>=0)
        {
            pe.speed = spdDir * pe.EndDashSpeed;
        }
        if(pe.speed.y>0)
        {
            pe.speed.y *= SpdSet.EndDashUpMult;
        }

        returnState = State.Normal;

    }
}
