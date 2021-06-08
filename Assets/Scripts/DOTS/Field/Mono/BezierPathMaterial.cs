using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System;
using System.Collections.Generic;
using UnityEngine;

//--------------------------------------------------------------------
// Class: BezierPathMaterial
// Desc : Prefab Mono Component 
//--------------------------------------------------------------------
public class BezierPathMaterial : MonoBehaviour
{
    static class Uniforms
    {
        internal static readonly int _Speed_X   = Shader.PropertyToID("_Speed_X");
        internal static readonly int _LineColor = Shader.PropertyToID("_LineColor");
    }

    public Color[]      lineColors;     // Color Types
    public Material[]   lineTypes;      // Material List

    public void SetMaterialColor(Int16 material, Int16 color, float speed)
    { 
        var meshRender = GetComponent<MeshRenderer>();
        if (meshRender != null)
        {
            // 머티리얼
            if (lineTypes != null)
                meshRender.material = lineTypes[material % lineTypes.Length];
            // 색상
            if (lineColors != null)
                meshRender.material.SetColor(Uniforms._LineColor, lineColors[color % lineColors.Length]);

            // 속도 
            meshRender.material.SetFloat(Uniforms._Speed_X, speed);
        }
    }
}
