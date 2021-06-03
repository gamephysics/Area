using System;
using Unity.Mathematics;

//--------------------------------------------------------------------
// class : GLOBAL_CONST
// Desc  : CONST 값들을 여기에 모아두었음
//--------------------------------------------------------------------
public class GLOBAL_CONST
{
    public const            Int32   MAP_WIDTH   = 1024;
    public const            Int32   MAP_HEIGHT  = 1024;
    public static readonly  int2    MAP_MIN     = int2.zero;
    public static readonly  int2    MAP_MAX     = new int2(MAP_MIN.x + MAP_WIDTH - 1, MAP_MIN.y + MAP_HEIGHT - 1);
}
