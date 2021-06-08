#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;

public class ShaderGUI_NM_FleetTrail : ShaderGUI
{
    MaterialProperty blendMode = null;
    MaterialProperty srcBlend = null;
    MaterialProperty dstBlend = null;

    MaterialProperty lineColor = null;
    MaterialProperty maskTex = null;

    MaterialProperty count_U = null;
    MaterialProperty count_V = null;
    MaterialProperty speed_X = null;
    MaterialProperty speed_Y = null;
    MaterialProperty cull = null;
    MaterialProperty isZWrite = null;
    MaterialProperty texScale = null; 

    MaterialEditor m_MaterialEditor;

    bool m_FirstTimeApply = true;

    public void FindProperties(MaterialProperty[] props)
    {
        blendMode = FindProperty("_BlendMode", props);
        srcBlend = FindProperty("_SrcBlend", props);
        dstBlend = FindProperty("_DstBlend", props);
        lineColor = FindProperty("_LineColor", props);
        maskTex = FindProperty("_MaskTex", props);
        count_U = FindProperty("_Count_U", props);
        count_V = FindProperty("_Count_V", props);
        speed_X = FindProperty("_Speed_X", props);
        speed_Y = FindProperty("_Speed_Y", props);
        cull = FindProperty("_Cull", props);
        isZWrite = FindProperty("_IsZWrite", props);
        texScale = FindProperty("_TexScale", props);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        FindProperties(props);
        
        m_MaterialEditor = materialEditor;
        Material material = materialEditor.target as Material;

        ShaderPropertiesGUI(material);

        if (m_FirstTimeApply)
        {
            m_FirstTimeApply = false;
        }
    } 

    public void ShaderPropertiesGUI(Material material)
    {
        EditorGUIUtility.fieldWidth = 64.0f;

        m_MaterialEditor.ShaderProperty(blendMode, "Blend Mode", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        if (blendMode.floatValue == 0)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        if (blendMode.floatValue == 1)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        if (blendMode.floatValue == 2)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
        }

        if (blendMode.floatValue == 3)
        {
            m_MaterialEditor.ShaderProperty(srcBlend, "Src.Blend", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 2);
            m_MaterialEditor.ShaderProperty(dstBlend, "Dst.Blend", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 2);
        }
        m_MaterialEditor.ShaderProperty(lineColor, "Line Color", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(maskTex, "MaskTex (RGB)", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(count_U, "Count U", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(count_V, "Count V", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(speed_X, "Speed X", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(speed_Y, "Speed Y", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(cull, "Culling", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(isZWrite, "ZWrite", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        m_MaterialEditor.ShaderProperty(texScale, "Tex Scale", MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
        EditorGUILayout.Space();

        GUILayout.Label("ADVANCED", EditorStyles.boldLabel);
        GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(2.0f) });
        m_MaterialEditor.RenderQueueField();
        m_MaterialEditor.EnableInstancingField();
        m_MaterialEditor.DoubleSidedGIField();

        EditorGUIUtility.fieldWidth = 64.0f;
        EditorGUILayout.Space();

        EditorUtility.SetDirty(material);
    }
}
#endif