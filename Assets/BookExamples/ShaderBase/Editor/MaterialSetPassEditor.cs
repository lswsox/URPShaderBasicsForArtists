using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MaterialSetPass))] [CanEditMultipleObjects]
public class MaterialSetPassEditor : Editor
{
    private Material m_mat;

    private void OnEnable()
    {
        MaterialSetPass mss = target as MaterialSetPass;
        MeshRenderer mr = mss.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            m_mat = mr.sharedMaterial;
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("셰이더에 라이트모드 태그를 가진 Pass가 있어야함. 렌더러의 Render Features에서 Render Objects의 LightMode Tags가 설정된 이후에 작동함", MessageType.Info);
        base.OnInspectorGUI();
        if (m_mat != null)
        {
            MaterialSetPass msp = target as MaterialSetPass;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable " + msp.m_lightMode))
            {
                m_mat.SetShaderPassEnabled(msp.m_lightMode, true);
            }
            if (GUILayout.Button("Disable " + msp.m_lightMode))
            {
                m_mat.SetShaderPassEnabled(msp.m_lightMode, false);
            }
            GUILayout.EndHorizontal();
        }
    }
}
