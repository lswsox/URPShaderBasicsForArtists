using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MaterialSetState))] [CanEditMultipleObjects]
public class MaterialSetStateEditor : Editor
{
    private Material m_mat;

    private void OnEnable()
    {
        MaterialSetState mss = target as MaterialSetState;
        MeshRenderer mr = mss.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            m_mat = mr.sharedMaterial;
        }
    }

    public override void OnInspectorGUI()
    {
        if (m_mat != null)
        {
            EditorGUILayout.HelpBox("셰이더의 커스텀 에디터를 비활성 후 사용", MessageType.Info);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable Cast Shadow"))
            {
                m_mat.SetShaderPassEnabled("ShadowCaster", true);
            }
            if (GUILayout.Button("Disable Cast Shadow"))
            {
                m_mat.SetShaderPassEnabled("ShadowCaster", false);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable Alpha Clip"))
            {
                m_mat.EnableKeyword("_ALPHATEST_ON");
                m_mat.SetFloat("_AlphaClip", 1f);
            }
            if (GUILayout.Button("Disable Alpha Clip"))
            {
                m_mat.DisableKeyword("_ALPHATEST_ON");
                m_mat.SetFloat("_AlphaClip", 0f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Opaque"))
            {
                m_mat.SetFloat("_SrcBlend", 1f);
                m_mat.SetFloat("_DstBlend", 0f);
            }
            if (GUILayout.Button("Transparent"))
            {
                m_mat.SetFloat("_SrcBlend", 5f);
                m_mat.SetFloat("_DstBlend", 10f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ZWrite On"))
            {
                m_mat.SetFloat("_ZWrite", 1f);
            }
            if (GUILayout.Button("ZWrite Off"))
            {
                m_mat.SetFloat("_ZWrite", 0f);
            }
            GUILayout.EndHorizontal();
        }
        base.OnInspectorGUI();
    }
}
