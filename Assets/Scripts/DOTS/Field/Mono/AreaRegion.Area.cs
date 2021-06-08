using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;


using GuildID = System.Int64;


//--------------------------------------------------------------------
// Class: CArea
// Desc : 연맹영역 1개
//--------------------------------------------------------------------
public struct CArea
{
    // KEY
    public Int64            m_Key;
    // DATA
    public GuildID          m_GuildID;      // 연맹 ID
    public RectInt          m_Rect;         // 영역 GRID  
    public EnumAreaStatus   m_Status;       // 연맹영역 상태 : 연맹건물의 상태 : 배치중(클라이언트 ONLY), 건설중, 완료 (다른 이미지로 표시)
    public EnumAreaOwn      m_Own;          // 연맹영역 소유관계 : 내 연맹과의 관계 (라인 색이 다르게 표시)
    public Int64            m_Priority;     // 우선순위 : CrossFire에만 존재 (연맨건물 완료 시간 : CELL에 여려 연맹영역이 겹쳐진경우, 먼저 연맹건물을 건설해 점유한경우 해당 연맹의 소유로 판단하기 위함)

    //--------------------------------------------------------------------
    // 생성자
    //--------------------------------------------------------------------
    public CArea(GuildID guildID, int2 pos, int volume, int size, EnumAreaStatus status, EnumAreaOwn Own, Int64 priority)
    {
        m_Key       = Make32BitKeyValue(pos.x, pos.y);
        // DATA
        m_GuildID   = guildID;
        m_Rect      = GetRect(pos, volume, size, status != EnumAreaStatus.Placing);
        m_Status    = status;
        m_Own       = Own;
        m_Priority  = priority;
    }

    //--------------------------------------------------------------------
    // 연맹영역 Area 1개의 상태가 완성된 상태인지
    //--------------------------------------------------------------------
    public bool IsCompleted()
    {
        return m_Status == EnumAreaStatus.Completed; 
    }

    //--------------------------------------------------------------------
    // 동일한 정보인지 검사한다.
    //--------------------------------------------------------------------
    public bool IsCompare(ref CArea area)
    {
        if (m_GuildID   != area.m_GuildID)      return false;
        if (!m_Rect.Equals(area.m_Rect))        return false;
        if (m_Status    != area.m_Status)       return false;
        if (m_Own       != area.m_Own)          return false;
        if (m_Priority  != area.m_Priority)     return false;

        return true;
    }


    //--------------------------------------------------------------------
    // Rect 교차 상태 검사
    //--------------------------------------------------------------------
    public static bool IsIntersectRect(RectInt a, RectInt b)
    {
        if (a.xMin < b.xMax && a.xMax > b.xMin &&
            a.yMax > b.yMin && a.yMin < b.yMax)
            return true;

        return false;
    }

    //--------------------------------------------------------------------
    // Rect tolerance 안으로 인접 상태 검사
    //--------------------------------------------------------------------
    public static bool IsAdjacentRect(RectInt a, RectInt b)
    {
        Int32 tolerance = 1;
        return !((a.xMin - b.xMax) > tolerance
               || (b.xMin - a.xMax) > tolerance
               || (a.yMin - b.yMax) > tolerance
               || (b.yMin - a.yMax) > tolerance);
    }

    //--------------------------------------------------------------------
    // 위치를 기반으로 32 BIT KEY 생성 
    // X 가 상위 16BIT 에 존재하므로 X가 가장작은 값으로 KEY값이 정렬되어지는 효과가 있다.
    //--------------------------------------------------------------------
    static public Int64 Make32BitKeyValue(Int32 x, Int32 y) 
    {
        // 가장장은 X 중에 가장작은 Y 위치를 선택하기 위한 KEY 순서
        Int64 Key = x; Key <<= 16; Key += y;    
        return Key;
    }

    //=======================================================================================
    // COMMAN METHOD MAKE RECT WITH POS, SIZE(REGION SIZE), VOLUME
    //=======================================================================================
    // pos      : 연맹건물의 CELL 위치
    // volume   : 연맹건물의 CELL 점유 크기
    // size     : 연맹영역 크기 WIDTH == HEIGHT
    // clippable: false == Unity 에서 연맹영역을 확보하는 건물을 배치시킬경우 연맹영역의 표시는 CLIPPING 하지 않는다.
    public static RectInt GetRect(int2 pos, int volume, int size, bool clippable)
    {
        Int32 radius= size / 2;
        int2 start  = pos - radius;
        int2 end    = pos + volume - 1 + radius;

        if (clippable)
        {
            //ClampToBounds
            start   = math.clamp(start, GLOBAL_CONST.MAP_MIN, GLOBAL_CONST.MAP_MAX);
            end     = math.clamp(end,   GLOBAL_CONST.MAP_MIN, GLOBAL_CONST.MAP_MAX);
        }
        return new RectInt(start.x, start.y, (end.x - start.x) + 1, (end.y - start.y) + 1);
    }
}

//--------------------------------------------------------------------
// Class: CAreaGroup
// Desc : 겹쳐지거나 인접되어진 영역들의 GROUP
//--------------------------------------------------------------------
public class CAreaGroup : Dictionary<Int64, CArea>
{
    // for Logic
    public RectInt  m_Bound  = new RectInt(-1, -1, 0, 0);
    public Dictionary<Int64, GuildAreaElement> m_RepoGuildArea  = new Dictionary<Int64, GuildAreaElement>();

    //--------------------------------------------------------------------
    // 생성자
    //--------------------------------------------------------------------
    public CAreaGroup(CArea Area)
    {
        AppendArea(Area);
    }

    //--------------------------------------------------------------------
    // AreaGroup의 Bounding Box 갱신
    //--------------------------------------------------------------------
    private bool UnionRect(RectInt srcRect)
    {
        // srcRectis is Empty
        if (srcRect.width <= 0 && srcRect.height <= 0)
            return true;

        // m_Bound is Empty
        if (m_Bound.width <= 0 && m_Bound.height <= 0)
        {
            m_Bound = srcRect;
            return true;
        }

        m_Bound.xMin = Math.Min(m_Bound.xMin, srcRect.xMin);
        m_Bound.yMin = Math.Min(m_Bound.yMin, srcRect.yMin);
        m_Bound.yMax = Math.Max(m_Bound.yMax, srcRect.yMax);
        m_Bound.xMax = Math.Max(m_Bound.xMax, srcRect.xMax);

        return true;
    }

    //--------------------------------------------------------------------
    // Area 를 추가한다.
    //--------------------------------------------------------------------
    public bool AppendArea(CArea Area)
    {
        UnionRect(Area.m_Rect);
        Add(Area.m_Key, Area);

        return true;
    }
    //--------------------------------------------------------------------
    // Key 의 Area 를 제거한다.
    //--------------------------------------------------------------------
    public bool RemoveArea(Int64 Key)
    {
        if(Remove(Key))
        {
            // 실제 큰 의미는 없다. AREA를 해당 GROUP에서 제거하면 
            // 해당 GROUP은 파괴되고 나머지 AREA는 다시 연산되어져 새로운 GROUP으로 만들어져야한다.
            m_Bound = new RectInt(-1, -1, 0, 0);
            foreach (var a in this)
            { 
                UnionRect(a.Value.m_Rect);
            }

            return true;
        }

        return false;
    }

    //--------------------------------------------------------------------
    // AreaGroup에 속해있는 Area 들의 GuildID를 모은다.
    //--------------------------------------------------------------------
    public void CollectGuildIDs(Int64 groupID)
    {
        m_RepoGuildArea.Clear();

        foreach (var a in this)
        {
            if (!m_RepoGuildArea.ContainsKey(a.Value.m_GuildID))
            {
                m_RepoGuildArea.Add(a.Value.m_GuildID, new GuildAreaElement(groupID, a.Value.m_GuildID, a.Value.m_Status, a.Value.m_Own));
            }
        }
    }

    //--------------------------------------------------------------------
    // GuildID가 존재하는지 검사한다.
    //--------------------------------------------------------------------
    public bool IsContainGuild(GuildID guildID)
    {
        return m_RepoGuildArea.ContainsKey(guildID);
    }

    //--------------------------------------------------------------------
    // AreaGroup 에 해당 Area 가 합쳐질수 있는지 (교차나 인접)
    //--------------------------------------------------------------------
    public bool IsMergeable(CArea Area)
    {
        // 완료된 Area 들만 합쳐질수 있다.
        if (Area.IsCompleted() == false)
            return false;

        // Bound 도 인접되지 않는 Area는 합쳐질수도 없다.
        if (!CArea.IsAdjacentRect(m_Bound, Area.m_Rect))
            return false;

        // Group 의 Area 와 합쳐질수 있는 Area 인지 검사한다.
        foreach (var a in this)
        {
            if (a.Value.IsCompleted() == false) continue;

            if (a.Value.m_GuildID == Area.m_GuildID)
            {
                // 같은길드는 인접되어지는 경우에도 연결되어졌다고 본다.
                if (CArea.IsAdjacentRect(a.Value.m_Rect, Area.m_Rect))
                    return true;
            }
            else
            {
                // 다른길드는 겹쳐졌을 경우에만 연결되어졌다고 본다.
                if (CArea.IsIntersectRect(a.Value.m_Rect, Area.m_Rect))
                    return true;
            }
        }

        return false;
    }
        
    
};
