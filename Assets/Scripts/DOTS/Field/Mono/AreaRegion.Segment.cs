using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

using GuildID = System.Int64;

//--------------------------------------------------------------------
// Struct: Segment
// Desc  : LINE 조각
//--------------------------------------------------------------------
public struct Segment
{
    // LINE 조각의 방향
    public enum Side
    {
        SIDE_INVALID = 0,

        // REGMENT 용 (시계방향)
        SIDE_V_PLUS,    // 1 (NORTH)
        SIDE_H_PLUS,    // 2 (EAST)   
        SIDE_V_MINUS,   // 3 (SOUTH)
        SIDE_H_MINUS,   // 4 (WEST)

        SIDE_TOTAL,
    };

    public int2     m_S;        // LINE 조각의 시작지점
    public int2     m_E;        // LINE 조각의 종료지점
    public Side     m_side;     // m_S -> m_E 의 방향
    public Int64    m_Key;
    //--------------------------------------------------------------------
    // LINE 조각의 시작지점 종료지점을 저장하고 방향을 기록한다.
    //--------------------------------------------------------------------
    public void Set(Int32 x1, Int32 y1, Int32 x2, Int32 y2)
    {
        m_S.x = x1;
        m_S.y = y1;
        m_E.x = x2;
        m_E.y = y2;
        if      (m_S.x != m_E.x){ m_side = (m_S.x < m_E.x) ? Side.SIDE_H_PLUS : Side.SIDE_H_MINUS; }
        else if (m_S.y != m_E.y){ m_side = (m_S.y < m_E.y) ? Side.SIDE_V_PLUS : Side.SIDE_V_MINUS; }
        else                    { m_side = Side.SIDE_INVALID; }

        m_Key = _MakeSegKey(x1, y1, x2, y2);

        // 같은 방향에 상관없이 동일한 LINE SEGMENT 들을 비교하기위해서 Start, End 좌표의 순서를 정렬해 KEY로 만든다.
        Int64 _MakeSegKey(Int32 x1, Int32 y1, Int32 x2, Int32 y2)
        {
            if (x1 > x2) { Swap<Int32>(ref x1, ref x2); }
            if (y1 > y2) { Swap<Int32>(ref y1, ref y2); }
            Int64 value = CArea.Make32BitKeyValue(x2, y2); value <<= 32; value += CArea.Make32BitKeyValue(x1, y1);
            return value;
        }
    }

    public int2  Point1()   { return m_S;     }
    public int2  Point2()   { return m_E;     }
    public Side  GetSide()  { return m_side;  }
    public Int64 GetSegKey(){ return m_Key;   }
    public Int64 StartKey() {   return CArea.Make32BitKeyValue(m_S.x, m_S.y);    }
    public Int64 EndKey()   {   return CArea.Make32BitKeyValue(m_E.x, m_E.y);    }

    static public Side GetSearchSide(Side last, int index, bool holeline)
    {
        // index 0, 1, 2
        Side Value = last;
        if (holeline == false)
            Value = last - 1 + index;
        else
            Value = last + 1 - index;

        if (Value <= 0)
            Value += (int)(Side.SIDE_TOTAL - 1);
        if (Value >= Side.SIDE_TOTAL)
            Value -= (int)(Side.SIDE_TOTAL - 1);

        return Value;
    }

    //--------------------------------------------------------------------
    // Code	: Swap()
    //--------------------------------------------------------------------
    static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }
}

