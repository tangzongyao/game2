﻿using System.Collections;
using UnityEngine;
using static PlayerEntity;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class NormalState : BaseState
{
    public override State state => (int)State.Normal;

    public override bool haveCoroutine => false;

    public NormalState(PlayerEntity p):base(p){}

    public override IEnumerator Coroutine()
    {
        yield return null;
    }

    public override void OnEnd(){}
    public override void OnEnter(){}

    public override State Update()
    {
        {
            //爬墙
            if (pe.input.GamePlay.Climb.IsPressed() &!pe.IsTired&&!pe.Ducking)
            {
                //为了爬跳不会卡住，要往下落才能爬（我本来以为要个禁止爬墙计时器的，结果居然是这样- -）
                if (pe.speed.y <= 0 && pe.speed.x * (int)pe.facing >= 0)
                {
                    //if(pe.CheckCollider(pe.bodyBox, Vector2.right * (int)pe.facing))
                    if(pe.CastCheckCollider(Vector2.zero, Vector2.right * (int)pe.facing))
                    {
                        pe.Ducking = false;
                        return State.Climb;
                    }
                }
            }

            if(pe.CanDash)
            {
                return State.Dash;
            }

            if (pe.Ducking)
            {
                if(pe.onGround&&pe.input_move.y >= 0)
                {
                    if (pe.CanUnDuck)
                    {
                        pe.Ducking= false;
                        pe.scale = new Vector2(.8f, 1.2f);
                    }
                }
            }
            else if (pe.onGround && pe.input_move.y < 0 && pe.speed.y <= 0)
            {
                pe.scale = new Vector2(1.4f, 0.6f);
                pe.Ducking = true;
            }

        }

        //x轴速度计算
        if(pe.Ducking&&pe.onGround)
        {
            pe.speed.x = Mathf.MoveTowards(pe.speed.x, 0, SpdSet.DuckFriction*Time.deltaTime);
        }
        else
        {
            float mult = pe.onGround ? 1 : SpdSet.AirMult;
            float max = SpdSet.MaxRun;
            float moveX = pe.input_move.x;
            
            float acc = SpdSet.RunAccel;
            //可能会速度过快
            if (Mathf.Abs(pe.speed.x) > max && Mathf.Sign(pe.speed.x) == moveX)
                acc= SpdSet.RunReduce;

            pe.speed.x = Mathf.MoveTowards(pe.speed.x, max * moveX, acc * mult * Time.deltaTime);
        }


        //y轴速度计算
        float mf = SpdSet.MaxFall;
        float fmf = SpdSet.FastMaxFall;

        if (pe.input_move.y<0&& pe.speed.y <= mf)
        {
            pe.maxFall = Mathf.MoveTowards(pe.maxFall, fmf, SpdSet.FastMaxAccel * Time.deltaTime);

            //Scale变化：加速到fmf的一半的时候scale开始发生变化
            float half = (mf + fmf) / 2;
            if(pe.speed.y<=half)
            {
                float spriteLerp = Mathf.Min(1, (pe.speed.y - half) / (fmf - half));
                pe.scale.x = Mathf.Lerp(1f, 0.7f, spriteLerp);
                pe.scale.y = Mathf.Lerp(1f, 1.3f, spriteLerp);
            }
        }
        else
            pe.maxFall = Mathf.MoveTowards(pe.maxFall, mf, SpdSet.FastMaxAccel * Time.deltaTime);

        //重力 下落
        if (!pe.onGround)
        {
            float falls = pe.maxFall;

            //计算滑墙
            if (pe.input_move.x * (int)pe.facing > 0)
            {
                if (pe.speed.y <= 0 && pe.wallSlideTimer > 0 && pe.CheckCollider(pe.Position, pe.bodyBox, Vector2.right * (int)pe.facing))
                {
                    pe.Ducking = false;
                    pe.wallSlideDir = (int)pe.facing;
                }
                if (pe.wallSlideDir != 0)
                {
                    falls = Mathf.Lerp(SpdSet.MaxFall, SpdSet.WallSlideStartMax, pe.wallSlideTimer / TimeSet.WallSlideTime);
                    if (pe.wallSlideTimer / TimeSet.WallSlideTime > 0.65f)
                    {
                        pe.PlaySlideDust();
                    }
                }
            }
            pe.speed.y = Mathf.MoveTowards(pe.speed.y, falls, SpdSet.Gravity * Time.deltaTime);
        }
        else pe.speed.y = 0;

        if (pe.varJumpTimer > 0)
        {
            //有可能速度比跳跃快，所以是取Max
            if (pe.input.GamePlay.Jump.IsPressed())
                pe.speed.y = Mathf.Max(pe.speed.y, pe.varJumpSpeed);
            else
                pe.varJumpTimer = 0;
        }

        if (pe.input.GamePlay.Jump.WasPressedThisFrame())
        {
            if (pe.jumpGraceTimer > 0)
            {
                pe.Jump();
            }
            else if (true)
            {
                int wallJumpDir = pe.WallJumpCheck(1) ? -1 : pe.WallJumpCheck(-1) ? 1 : 0;

                if(wallJumpDir!=0)
                {
                    if (pe.dashAttackTimer > 0 && pe.dashDir.y > 0 && pe.dashDir.x == 0)
                        pe.SuperWallJump(wallJumpDir);
                    else
                        pe.WallJump(wallJumpDir);
                    pe.PlayWallJumpDust(-wallJumpDir);
                }

            }
        }

        return State.Normal;
    }
}
