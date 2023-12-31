using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerEntity;

public enum IntroTypes { Transition, Respawn, WalkInRight, WalkInLeft, Jump, WakeUp, Fall, None }
public enum Face {Left=-1,Right=1}

public partial class PlayerEntity: MonoBehaviour
{
    #region 配置
    public PlayerInput input;
    [HideInInspector] public Rigidbody2D rd;
    private ActionStateMechine stateMachine;
    private IntroTypes introType;

    [Header("设置")]
    [SerializeField] private Tail tail;
    [Title("动画控制"), SerializeField] private Animator anim;
    [SerializeField] private Color flashColor;
    [SerializeField] private Color[] dashColors;
    private SpriteRenderer sprite;
    [HideInInspector]public BoxCollider2D bodyBox;
    public BoxCollider2D normalBox;
    public BoxCollider2D duckBox;
    public int scaleMult;
    #endregion

    #region vars
    /// <summary>速度，会在Update最后赋值</summary>
    public Vector2 speed;
    [HideInInspector] public Vector2 dashDir;
    [HideInInspector] public int dashes;
    public int maxDashes;
    [HideInInspector] public float maxFall;
    /// <summary>保留速度用于计算长按</summary>
    [HideInInspector] public float varJumpSpeed;
    /// <summary>跳跃一定时间内不能转向</summary>
    [HideInInspector] public int forceMoveX;
    [HideInInspector] public Face facing;
    [HideInInspector] public int wallSlideDir;
    [HideInInspector] public bool dashStartedOnGround;
    [HideInInspector] public Vector3 scale;
    [HideInInspector] public int lastDashIndex;
    [HideInInspector] public float lastDashFacing;
    private float respawnTimer;

    //状态计算所需
    private bool _onGround;
    public float Stamina;
    #endregion

    #region 计时器
    [HideInInspector] public float varJumpTimer;
    /// <summary>土狼时间</summary>
    [HideInInspector] public float jumpGraceTimer;
    [HideInInspector] public float forceMoveXTimer;

    [HideInInspector] public float dashAttackTimer;
    [HideInInspector] public float dashCooldownTimer;
    [HideInInspector] public float wallSlideTimer;

    private float launchTimer;
    [HideInInspector]public float dashEffectTimer;

    [HideInInspector] public float deadTimer;
    #endregion

    [Header("输入")]
    [HideInInspector] public Vector2 input_move;

    #region 初始化函数
    private void Awake()
    {
        input = new PlayerInput();
        stateMachine = new ActionStateMechine();
        stateMachine.states.Add(new NormalState(this));
        stateMachine.states.Add(new ClimbState(this));
        stateMachine.states.Add(new DashState(this));
        stateMachine.states.Add(new DeadState(this));
        rd = GetComponent<Rigidbody2D>();
        sprite=anim.GetComponent<SpriteRenderer>();

        scale = Vector3.zero;
        respawnTimer = TimeSet.respawnTime;
        facing = Face.Right;
    }
    private void OnEnable()
    {
        input.Enable();
        maxFall = SpdSet.MaxFall;
        Ducking = false;
        Stamina = ClimbSet.ClimbMaxStamina;
    }
    private void OnDisable()
    {
        input.Disable();
    }
    
    void Start()//被加入地图后
    {
        switch (introType)
        {
            case IntroTypes.Transition:
                stateMachine.state = (int)State.Normal;
                break;
            default:break;
        }
    }
    #endregion

    void Update()
    {
        speed = rd.velocity;

        //输入
        input_move = input.GamePlay.Move.ReadValue<Vector2>();
        input_move.x = input_move.x > 0 ? 1 : input_move.x < 0 ? -1 : 0;
        input_move.y = input_move.y > 0 ? 1 : input_move.y < 0 ? -1 : 0;

        //变量计算
        var wasOnGround=onGround;
        onGround = (!(stateMachine.state==(int)State.Dash&&speed.y>0))&&CastCheckCollider(Vector2.zero,Vector2.down);
        //onGround = CastCheckCollider(Vector2.zero, Vector2.down);

        #region 各种计时器
        varJumpTimer.TimePassBy();
        dashAttackTimer.TimePassBy();
        dashCooldownTimer.TimePassBy();

        if (onGround)
        {
            jumpGraceTimer = TimeSet.JumpGraceTime;
            dashes = maxDashes;
            if (!wasOnGround) speed.y = 0;
        }
        else
        { jumpGraceTimer.TimePassBy(); }

        //由游戏控制input.x
        if (forceMoveXTimer > 0)
        {
            forceMoveXTimer -= Time.deltaTime;
            input_move.x = forceMoveX;
        }

        //wallSlideTimer
        if (wallSlideDir != 0)
        {
            wallSlideTimer = Math.Max(wallSlideTimer - Time.deltaTime, 0);
            wallSlideDir = 0;
        }

        #endregion

        #region  var 
        // 朝向
        if (input_move.x!=0&&stateMachine.state!=(int)State.Climb)
        {
            facing = (Face)input_move.x;
        }
        //Climb相关
        if(onGround&& stateMachine.state != (int)State.Climb)
        {
            Stamina = ClimbSet.ClimbMaxStamina;
            wallSlideTimer = TimeSet.WallSlideTime;
        }
        //上墙等待赋值X速度
        if (hopWaitX != 0)
        {
            if (speed.x * hopWaitX < 0)
                hopWaitX = 0;
            if (!CheckCollider(Position, bodyBox, Vector2.right * (int)facing))
            {
                speed += hopWaitX * hopWaitXSpeed * Vector2.right;
                hopWaitX = 0;
            }
        }

        #endregion

        if (respawnTimer.TimePassBy() <= 0)
        stateMachine.Update();

        OnCollisionDatas();

        UpdateAnimAndTail();

        UpdateSprite();
            
        rd.velocity = speed;
        scale.x *= (int)facing;
        sprite.transform.localScale = scale*scaleMult;
        scale.x *= (int)facing;

        if (stateMachine.state == (int)State.Dead)
        {
            if (deadTimer.TimePassBy() <= 0)
                Dead();
        }
    }

}
