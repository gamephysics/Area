using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

using GuildID = System.Int64;

[UpdateInGroup(typeof(FieldInitializeSystemGroup))]
[UpdateAfter(typeof(AreaUpdateSystem))]
//--------------------------------------------------------------------
// Class: AreaRegionSystem 
// Desc : AreaGruop의 Area 들로 연맹영억의 LINE POINT 생성 (Outline,HoleLines)
//--------------------------------------------------------------------
public partial class AreaPointLineSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        //==================================================================================================
        // Command Buffer
        //==================================================================================================
        m_EntityCommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        //==================================================================================================
        // LINE CALCULATION
        //==================================================================================================
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();

        var XPointsHash     = new NativeHashSet<int>(10, Allocator.TempJob);                    // UNIQUE Y POINT LIST  (NOT SORTED)
        var YPointsHash     = new NativeHashSet<int>(10, Allocator.TempJob);                    // UNIQUE X POINT LIST  (NOT SORTED)
        var GridSpace       = new NativeList<Int64>(Allocator.TempJob);                         // NORMALIZED GRID SPACE
        var SegmentLine     = new NativeHashMap<Int64, Segment>(10, Allocator.TempJob);         // UNIQUE LINE SEGMENT 
        var SegmentStart    = new NativeMultiHashMap<Int64, Segment>(10, Allocator.TempJob);    // LINE SEGMENT WITH START POSITION (NOT SORTED)
        var GridPointList   = new NativeList<int2>(10, Allocator.TempJob);

        var AreaRegionUpdateJobHandle = Entities
            .WithName("AreaRegionUpdate")
            .ForEach(
                (Entity entity,
                in DynamicBuffer<AreaElement> Areas,
                in DynamicBuffer<GuildAreaElement> GuildAreas) =>
            {
                // 제거
                commandBuffer.DestroyEntity(entity);

                if (Areas.Length <= 0) return;

                //==================================================================================================
                // I. 차원을 줄여 연산 부하를 줄여봅시다.
                //==================================================================================================
                //==========================================================================
                // ALLOCATE
                //==========================================================================
                XPointsHash.Capacity = math.max(XPointsHash.Capacity, (Areas.Length + 1) * 2);
                YPointsHash.Capacity = math.max(YPointsHash.Capacity, (Areas.Length + 1) * 2);
                XPointsHash.Clear();
                YPointsHash.Clear();

                //===================================================
                // 1. 사각형들의 꼭지점들을 중복제거된 X 좌표, Y 좌표로 모아봅시다.
                //===================================================
                for (int i = 0; i < Areas.Length; ++i)
                {
                    AreaElement area = Areas[i];

                    if (!XPointsHash.Contains(area.m_Rect.xMin)) XPointsHash.Add(area.m_Rect.xMin);
                    if (!XPointsHash.Contains(area.m_Rect.xMax)) XPointsHash.Add(area.m_Rect.xMax);
                    if (!YPointsHash.Contains(area.m_Rect.yMin)) YPointsHash.Add(area.m_Rect.yMin);
                    if (!YPointsHash.Contains(area.m_Rect.yMax)) YPointsHash.Add(area.m_Rect.yMax);
                }
                if (XPointsHash.Count() <= 0) return;
                if (YPointsHash.Count() <= 0) return;

                //===================================================
                // 2. 중복제거된 X 좌표와 Y 좌표의 순서를 정리하면 다음과 같습니다.
                //===================================================
                // SORTED POINT LIST
                var XPoints = XPointsHash.ToNativeArray(Allocator.Temp); XPoints.Sort();
                var YPoints = YPointsHash.ToNativeArray(Allocator.Temp); YPoints.Sort();       // 좌표의 순서를 정렬

                //===================================================
                // 3. 정리된 꼭기점으로 GRID를 만들어 봅시다.
                //===================================================
                // GRID 개수 
                int GridXSpace = (XPoints.Length - 1);
                int GridYSpace = (YPoints.Length - 1);

                int gridspacesize = (GridXSpace * GridYSpace) * 2;  // (GUILDID, PRIORITY) PRIORITY 값이 적은 점유가 우선순위를 갖습니다.(CF ONLY)
                int gridmaxlines  = (GridXSpace * GridYSpace) * 4;  // 모든 GRID 4면의 LINE 총 개수

                //==========================================================================
                // ALLOCATE
                //==========================================================================
                GridSpace.Clear();      // 초기화 
                GridSpace.Resize(gridspacesize, NativeArrayOptions.ClearMemory);  // RESIZE 될때만 초기화됨
                SegmentLine.Clear();     SegmentLine.Capacity     = math.max(SegmentLine.Capacity, gridmaxlines);           // UNIQUE LINE 조각들
                SegmentStart.Clear();    SegmentStart.Capacity    = math.max(SegmentStart.Capacity, SegmentLine.Count());   // 시작지점을 기준으로하는 LINE 조각들

                //==========================================================================
                // 4. GRID에 점유 정보를 설정합니다. 
                //==========================================================================
                for (int g = 0; g < Areas.Length; ++g)
                {
                    AreaElement area = Areas[g];

                    // AREA의 4 꼭지점 좌표의 INDEX를 얻어온다.
                    Int32 xIndex1 = XPoints.IndexOf<int, int>(area.m_Rect.xMin); if (xIndex1 < 0) continue;  // ERROR
                    Int32 xIndex2 = XPoints.IndexOf<int, int>(area.m_Rect.xMax); if (xIndex2 < 0) continue;  // ERROR
                    Int32 yIndex1 = YPoints.IndexOf<int, int>(area.m_Rect.yMin); if (yIndex1 < 0) continue;  // ERROR
                    Int32 yIndex2 = YPoints.IndexOf<int, int>(area.m_Rect.yMax); if (yIndex2 < 0) continue;  // ERROR

                    //==========================================================================
                    // 5. 줄어든 차원의 GRID에 영역의 소유를 설정합니다.
                    //==========================================================================
                    for (Int32 y = yIndex1; y < yIndex2; ++y)
                    {
                        for (Int32 x = xIndex1; x < xIndex2; ++x)
                        {
                            Int32 Index  = (y * GridXSpace + x) * 2; // 정보가 2개씩 증가합니다. [GUILDID, PRIORITY]
                            Int32 GIndex = Index + 0;
                            Int32 PIndex = Index + 1;
                            // 비어있는곳은 정보를 설정합니다.
                            if (GridSpace[GIndex] == 0)
                            {
                                GridSpace[GIndex] = area.m_GuildID;
                                GridSpace[PIndex] = area.m_Priority;
                            }
                            // 채워진곳은 우선순위를 결정해 점유길드를 설정합니다.
                            else if ((GridSpace[PIndex] == area.m_Priority && GridSpace[GIndex] > area.m_GuildID) ||
                                     (GridSpace[PIndex] > area.m_Priority))
                            {
                                GridSpace[GIndex] = area.m_GuildID;
                                GridSpace[PIndex] = area.m_Priority;
                            }
                        }
                    }
                }

                //==================================================================================================
                // II. 점유 영역의 외곽을 추출해 냅니다.
                //==================================================================================================
                //==================================================================================================
                // 0. GUILD 별로 LINE POINT를 추출해 냅니다.
                //==================================================================================================
                for (int g = 0; g < GuildAreas.Length; ++g)
                {
                    GuildID guildID = GuildAreas[g].m_GuildID;

                    //===================================================
                    // 1. 모든 GIRD 공간을 반복합니다.
                    //===================================================
                    for (Int32 y = 0; y < GridYSpace; ++y)
                    {
                        for (Int32 x = 0; x < GridXSpace; ++x)
                        {
                            Int32 Index  = (y * GridXSpace + x) * 2;
                            Int32 GIndex = Index + 0;

                            // 0 은 비어있는곳이므로 PASS, 값이 있으면 선택된 GUILD 만 정보를 모읍니다
                            if (GridSpace[GIndex] != guildID) continue;

                            // GRID 의 LINE 조각을 모읍니다. 
                            for (Int32 l = 0; l < 4; ++l)
                            {
                                Segment r = new Segment();
                                switch (l)
                                {
                                    case 0: r.Set(XPoints[x],     YPoints[y],       XPoints[x],     YPoints[y + 1]); break;
                                    case 1: r.Set(XPoints[x],     YPoints[y + 1],   XPoints[x + 1], YPoints[y + 1]); break;
                                    case 2: r.Set(XPoints[x + 1], YPoints[y + 1],   XPoints[x + 1], YPoints[y]);     break;
                                    case 3: r.Set(XPoints[x + 1], YPoints[y],       XPoints[x],     YPoints[y]);     break;
                                    default: break;
                                }
                                // 방향에 상관없이 LINE 조각을 비교하기 위해서 Hash 를 사용합니다.
                                if (SegmentLine.ContainsKey(r.GetSegKey())) { SegmentLine.Remove(r.GetSegKey()); }      // 같으면 모두 제거
                                else                                        { SegmentLine.Add(r.GetSegKey(), r); }      // 그렇지 않으면 추가
                            }
                        }
                    }

                    //===================================================
                    // 2.모아진 LINE 조각들을 시작지점을 기준으로 정리합니다.
                    //===================================================
                    // MultiValueSortedDictionary 가 없어서 SegmentMultiMap, SegmentKeys 로 나눕니다.
                    SegmentStart.Capacity = math.max(SegmentStart.Capacity, SegmentLine.Count());
                    SegmentStart.Clear();  // CLEAR

                    // HASH 에 담겨진 LINE조각들을 시작지점을 기준으로 MULTIMAP으로 옮겨옵니다.
                    if (SegmentLine.Count() > 0)
                    {
                        var setKeyValues = SegmentLine.GetKeyValueArrays(Allocator.Temp);
                        for (int i = 0; i < setKeyValues.Values.Length; ++i)
                        {
                            var va = setKeyValues.Values[i];
                            SegmentStart.Add(va.StartKey(), va);
                        }
                        // CLEAR
                        setKeyValues.Dispose();
                        SegmentLine.Clear();
                    }

                    // LINE 조각의 시작 X,Y 값을 기준으로 SORT 하기위해서 KEY 만 가져와 SORT 합니다.
                    var SegmentKeys = SegmentStart.GetKeyArray(Allocator.Temp);
                    SegmentKeys.Sort();

                    //===================================================
                    // 3. 이제 LINE 조각들을 뽑아서 CLOSED LINE을 구성하는 POINT 들로 구성해봅시다.
                    //===================================================
                    int KeyIndex    = 0;
                    Int64 LastKey   = 0;
                    Segment.Side LastSide = Segment.Side.SIDE_INVALID;
                    bool HoleLine   = false; // Outline 생성 (1개) 시작은 OUTLINE 부터

                    //===================================================
                    // 4. 모든 LINE 조각들을 뽑을때까지 반복합니다.
                    //===================================================
                    while (SegmentStart.Count() > 0)
                    {
                        GridPointList.Clear();

                        //Debug.Log("1. START POINT SEARCH ");
                        //===================================================
                        // 5. 최초 시작 LINE 조각을 찾습니다.
                        //===================================================
                        bool FoundStart = false;

                        while (true)
                        {
                            // 미리 SORT된 KEY를 기준으로 가장 작은 X 중 가장작은 Y 값을 갖고있는 LINE 조각을 찾습니다.
                            if (SegmentStart.TryGetFirstValue(SegmentKeys[KeyIndex], out var value, out var it) == true)
                            {
                                GridPointList.Add(value.Point1());
                                GridPointList.Add(value.Point2());

                                LastKey     = value.EndKey();
                                LastSide    = value.GetSide();

                                SegmentStart.Remove(it);

                                // 정상 BREAK 성공
                                FoundStart = true;

                                //Debug.Log("# START POINT FOUND ");

                                break;
                            }
                            else
                            {
                                // 해당 KEY가 다 소진어졌으면 다음 KEY로 이동해 반복합니다.
                                KeyIndex++;

                                // 강제 BREAK 에러
                                if (SegmentKeys.Length <= KeyIndex)
                                {
                                    //Debug.Log("# START POINT NOT FOUND");
                                    break;
                                }
                                else
                                {
                                    //Debug.Log("2. START POINT NEXT KEY");
                                }
                            }
                        }

                        // 시작지점을 찾지 못하였으면 종단해야합니다.
                        if (FoundStart == false)
                            break;

                        //===================================================
                        // 6. 선택된 LINE 조각에서 부터 연속적으로 LINE조각들을 이어갑니다.
                        //===================================================
                        while (true)
                        {
                            bool FoundNext = false;

                            if (SegmentStart.Count() > 0)
                            {
                                //===================================================
                                // 7. 직전 LINE 조각의 END POINT를 시작지점인 LINE 조각을 찾습니다.
                                //===================================================
                                if (SegmentStart.CountValuesForKey(LastKey) >= 1)
                                {
                                    Segment value = default;

                                    // 진행방향을 기준으로 좌측, 직진, 우측 3가지 방향의 LINE 조각이 있을 수 있습니다.
                                    for (int i = 0; (i < 3 && FoundNext == false); ++i)
                                    {
                                        //===================================================
                                        // 직전 진행방을 기준으로 밖으로 크게 CLOSED LINE을 구성하기 위해서 
                                        // OUT 라인을 구성할때는 시계방향 이므로 좌측, 직진, 우측 순서로 선택합니다.
                                        // HOLE 라인을 구성할때는 방시계방향이므로 우측, 직진, 촤즉 순서로 선택합니다.
                                        //===================================================
                                        Segment.Side SearchSide = Segment.GetSearchSide(LastSide, i, HoleLine);

                                        //===================================================
                                        // 직전 LINE 조각의 END POINT를 시작지점으로 갖는 라인조각들중 SearchSide의 방향을 갖는 라인조각을 찾습니다.
                                        //===================================================
                                        if (SegmentStart.TryGetFirstValue(LastKey, out var iter, out var it))
                                        {
                                            do
                                            {
                                                if (iter.GetSide() == SearchSide)
                                                {
                                                    value = iter;
                                                    FoundNext = true;
                                                    SegmentStart.Remove(it);

                                                    break;
                                                }
                                            }
                                            while (SegmentStart.TryGetNextValue(out iter, ref it));
                                        }
                                    }

                                    //===================================================
                                    // 8. 이어지는 라인조각을 찾았으면 연속된 POINT를 추가합니다.
                                    //===================================================
                                    if (FoundNext)
                                    {
                                        if (LastSide != value.GetSide())
                                        {
                                            // 방향이 바뀌면
                                            // 새로얻은 라인조각의 END POINT를 추가합니다.
                                            GridPointList.Add(value.Point2());
                                        }
                                        else
                                        {
                                            // 같은 방향으로 이어지면 직전의 POINT를 제거하고,
                                            // 새로얻은 라인조각의 END POINT를 추가합니다.
                                            GridPointList[GridPointList.Length - 1] = (value.Point2());
                                        }

                                        LastKey = value.EndKey();
                                        LastSide = value.GetSide();
                                    }
                                }
                            }

                            //===================================================
                            // 9. 더이상 이어지는 LINE 조각이 없으면 CLOSED LINE이 완성된것입니다.
                            //===================================================
                            if (FoundNext == false)
                            {
                                if (GridPointList.Length > 0)
                                {
                                    int2 LineLastPoint  = GridPointList[GridPointList.Length - 1];
                                    int2 LineFirstPoint = GridPointList[0];

                                    // 시작지점과 끝점이 같으면 연결되어진 CLOSED LINE 이고 중복된 끝점을 제거합니다.
                                    if (LineLastPoint.Equals(LineFirstPoint))
                                    {
                                        // 끝점을 제거 합니다. (CLOSED LINE 구성에 같은 값을 필요없습니다)
                                        GridPointList.RemoveAt(GridPointList.Length - 1);
                                    }
                                }
                                break;
                            }
                        }

                        //===================================================
                        // 10. 완성된 CLOSED LINE을 LINE RENDER 로 MESH 형태로 구성하기 위해 DOTS ENTITY를 구성합니다.
                        //===================================================
                        AddAreaRegion(commandBuffer, GuildAreas[0].m_GroupID, GuildAreas[g].m_Status, GuildAreas[g].m_Own, GridPointList);

                        // 다음부터는 HOLE CLOSED LINE 을 생성하기 시작합니다.
                        HoleLine = true;
                    }

                    SegmentKeys.Dispose();
                }
                //==========================================================================
                // DESTROY
                //==========================================================================
                XPoints.Dispose();
                YPoints.Dispose();
            })
            .WithDisposeOnCompletion(XPointsHash)
            .WithDisposeOnCompletion(YPointsHash)
            .WithDisposeOnCompletion(GridSpace)
            .WithDisposeOnCompletion(SegmentLine)
            .WithDisposeOnCompletion(SegmentStart)
            .WithDisposeOnCompletion(GridPointList)
            .Schedule(this.Dependency);

        //==================================================================================================
        // ADD JOB BARRIER FOR USING
        //==================================================================================================
        m_EntityCommandBufferSystem.AddJobHandleForProducer(AreaRegionUpdateJobHandle);

        //==================================================================================================
        // Dependency = LAST JOB
        //==================================================================================================
        this.Dependency = AreaRegionUpdateJobHandle;
    }

    private static void AddAreaRegion(EntityCommandBuffer commandBuffer, Int64 groupID, EnumAreaStatus status, EnumAreaOwn own,  NativeList<int2> GridPointList)
    {
        if (GridPointList.Length <= 0)
            return;

        //==================================================================================================
        // CREATE NEW AREA ENTITY
        //==================================================================================================
        {
            // CREATE
            Entity entity = commandBuffer.CreateEntity();
            if (entity != Entity.Null)
            {
                // ADD DATA 기본정보
                commandBuffer.AddComponent(entity, new FieldMeshLineData
                {
                    m_ID        = groupID,
                    m_Type      = EnumdMeshLineType.AreaRegion,
                    m_Material  = (Int16)status,
                    m_Color     = (Int16)own,
                    m_Speed     = 1f
                });

                //===================================================
                // RENDERMESH  RAW DATA
                //===================================================
                var pointBuffer = commandBuffer.AddBuffer<FieldMeshPointElement>(entity);
                for (int i = GridPointList.Length - 1; i >= 0; --i)
                {
                    pointBuffer.Add(GridPointList[i]);
                }

                // BUFFER 추가 계산을 위한
                commandBuffer.AddBuffer<FieldMeshVertexElement>(entity);
                commandBuffer.AddBuffer<FieldMeshTriangleElement>(entity);
                commandBuffer.AddBuffer<FieldMeshUVElement>(entity);
                commandBuffer.AddBuffer<FieldMeshColorElement>(entity);

                Debug.Log("CREATED AREA LINE ");
            }
        }
    }
}