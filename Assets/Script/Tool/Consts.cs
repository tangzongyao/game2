using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Consts{
    public static class Path
    {
        public static readonly string prefabPath = "Prefab/";
    }

    public static class Times
    {
        public const float AirMult = .65f;
        /// <summary>跳跃</summary>
        public const float VarJumpTime = .2f;
        public const float WallJumpForceTime = .16f;
        /// <summary>土狼时间，离地0.1s还可以跳</summary>
        public const float JumpGraceTime = .1f;
        /// <summary>爬墙开始时的速度减少</summary>
        public const float ClimbGrabYMult = .2f;
        public const float ClimbNoMoveTime=.1f;
    }

}
