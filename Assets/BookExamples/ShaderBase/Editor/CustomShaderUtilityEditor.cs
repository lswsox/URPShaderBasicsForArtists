using UnityEditor;

public class CustomShaderUtilityEditor : Editor
{
    // 프로퍼티 배열에서 이름으로 인덱스 리턴
    public static int GetPropertyIndex(MaterialProperty[] properties, string name)
    {
        for (int i = 0; i < properties.Length; i++)
        {
            if (properties[i].name == name)
                return i;
        }
        return -1; // 뭔가 문제가 있으면 -1 리턴
    }
}
