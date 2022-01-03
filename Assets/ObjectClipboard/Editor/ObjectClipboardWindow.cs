using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

// 프로젝트나 씬의 오브젝트들을 잠시 담아두는 유틸리티
public class ObjectClipboardWindow : EditorWindow
{
    private string m_projectName = "";

    private const string mc_EditorPrefsWordToolName = "SoxOCB_";
    private const string mc_EditorPrefsWordInsID = "_InsID_"; // 씬 오브젝트 기록용
    private const string mc_EditorPrefsWordGUID = "_GUID_"; // 프로젝트 오브젝트 기록용
    private const string mc_EditorPrefsWordLock = "_Lock_";
    private const string mc_EditorPrefsWordDescription = "_Desc_";
    private const string mc_EditorPrefsWordOptRCount = "_OptRCount_"; // 툴 옵션 레지스트리 카운트
    private const string mc_EditorPrefsWordOptSDescription = "_OptSDesc_"; // 툴 옵션 레지스트리 카운트

    // The maximum number to record in the registry.
    private const int mc_maxRegistryLimit = 1000;

    // 메뉴 오픈 여부는 레지스트리에 기록하지 않음
    private bool m_menuOpen = false;

    // 레지스트리에 기록하는 툴 옵션
    private int m_optRegistryCount = 50;
    private bool m_optShowDescription = false;

    private GUIContent m_iconToolSelect;
    private GUIContent m_iconToolMove;
    private GUIContent m_iconToolTrash;
    private GUIContent m_iconToolLock;

    private GUIContent m_iconCamViewToGame;
    string m_iconTooltipCamViewToGame = "";
    private GUIContent m_iconCamGameToView;
    string m_iconTooltipCamGameToView = "";
    string m_iconTooltipCamDisabled = "";

    // 토글 버튼 스타일
    private static GUIStyle ToggleButtonStyleNormal = null;
    private static GUIStyle ToggleButtonStyleToggled = null;

    [System.Serializable]
    public class ClippedObject // 값 타입인 struct를 쓰면 m_objects[i].m_object = EditorGUILayout.ObjectField 에서 문제됨 (참조 타입을 사용해야...)
    {
        public Object m_object;
        public bool m_locked; // 잠긴 것은 삭제되지 않는다.
        public string m_description;

        public ClippedObject(Object obj, bool locked, string description)
        {
            m_object = obj;
            m_locked = locked;
            m_description = description;
        }
    }

    public List<ClippedObject> m_objects = new List<ClippedObject>();

    public static Vector2 scrollPos;

    private bool m_controlKeyDown = false;

    [MenuItem("Window/Object Clipboard")]
    public static void ShowWindow()
    {
        ObjectClipboardWindow window = (ObjectClipboardWindow)EditorWindow.GetWindow(typeof(ObjectClipboardWindow), false, "Object Clipboard");
        window.minSize = new Vector2(200, 150);
    }

    void OnSelectionChange()
    {
        Repaint();
    }

    void OnEnable()
    {
        m_projectName = GetProjectName();

        #if UNITY_2017_2_OR_NEWER
        string selectIconName = "Grid.Default"; // 2017.2 부터 추가된 아이콘
        #else
        string selectIconName = "ViewToolZoom On";
        #endif

        m_iconToolSelect = EditorGUIUtility.IconContent(selectIconName);
        m_iconToolSelect.tooltip = "Select";
        m_iconToolMove = EditorGUIUtility.IconContent("d_CollabMoved Icon");
        m_iconToolMove.tooltip = "Move Asset in the Project";
        m_iconToolTrash = EditorGUIUtility.IconContent("TreeEditor.Trash");
        m_iconToolTrash.tooltip = "Erase the object. When used with the Control key, the item is completely removed.";
        m_iconToolLock = EditorGUIUtility.IconContent("InspectorLock");
        m_iconToolLock.tooltip = "Lock it so that it cannot be moved or erased.";

        m_iconCamViewToGame = EditorGUIUtility.IconContent("Camera Icon");
        m_iconTooltipCamViewToGame = "Change the Game Camera :\nChange the game camera (same as the editor camera in the scene view)\n게임 카메라를 변경 (씬 뷰의 에디터 카메라와 동일하게)";
        m_iconCamGameToView = EditorGUIUtility.IconContent("Camera Gizmo");
        m_iconTooltipCamGameToView = "Change the Editor Camera :\nChanged the editor camera of the scene view (same as Hierarchy's game camera)\n씬 뷰의 에디터 카메라를 변경 (Hierarchy의 게임 카메라와 동일하게)";
        m_iconTooltipCamDisabled = "This function is activated when a Camera is selected in the Hierarchy view.\n이 기능은 Hierarchy 뷰에서 카메라가 선택되었을 떄 활성화됩니다.";

        GetEditorPrefs();
        AutoListCount(); // 리스트가 하나도 없는지 등을 검사
    }

    void OnDisable()
    {
        UpdateEditorPrefs();
    }

    // 레지스트리에 기록될 일련번호 키 스트링을 세팅하는 함수
    private void SetOptKey(out string keyRegCount, out string keySDescription)
    {
        keyRegCount = mc_EditorPrefsWordToolName + m_projectName + mc_EditorPrefsWordOptRCount;
        keySDescription = mc_EditorPrefsWordToolName + m_projectName + mc_EditorPrefsWordOptSDescription;
    }

    // 레지스트리에 기록될 일련번호 키 스트링을 세팅하는 함수
    private void SetIndexedKey(int index, out string keyInsID, out string keyGUID, out string keyLock, out string keyDescription)
    {
        keyInsID = mc_EditorPrefsWordToolName + m_projectName + mc_EditorPrefsWordInsID + index.ToString();
        keyGUID = mc_EditorPrefsWordToolName + m_projectName + mc_EditorPrefsWordGUID + index.ToString();
        keyLock = mc_EditorPrefsWordToolName + m_projectName + mc_EditorPrefsWordLock + index.ToString();
        keyDescription = mc_EditorPrefsWordToolName + m_projectName + mc_EditorPrefsWordDescription + index.ToString();
    }

    private void GetEditorPrefs()
    {
        // 툴 옵션
        string keyRegCount = "";
        string keySDescription = "";
        SetOptKey(out keyRegCount, out keySDescription);
        if (EditorPrefs.HasKey(keyRegCount))
        {
            m_optRegistryCount = EditorPrefs.GetInt(keyRegCount);
        }
        if (EditorPrefs.HasKey(keySDescription))
        {
            m_optShowDescription = EditorPrefs.GetBool(keySDescription);
        }

        m_objects.Clear();
        string keyInsID = "";
        string keyGUID = "";
        string keyLock = "";
        string keyDescription = "";
        int insId = 0;
        string guid = "";
        bool locked = false;
        string description;
        for (int i = 0; i < m_optRegistryCount; i++)
        {
            SetIndexedKey(i, out keyInsID, out keyGUID, out keyLock, out keyDescription);
            if (EditorPrefs.HasKey(keyInsID))
            {
                insId = EditorPrefs.GetInt(keyInsID);
                guid = EditorPrefs.GetString(keyGUID);
                if (insId == 0 && guid == "")
                {
                    m_objects.Add(new ClippedObject(null, false, ""));
                }
                else // 씬오브젝트거나 프로젝트 오브젝트거나
                {
                    locked = EditorPrefs.GetBool(keyLock);
                    description = EditorPrefs.GetString(keyDescription);
                    if (guid == "") // 씬오브젝트의 경우
                    {
                        Object idObj = EditorUtility.InstanceIDToObject(insId);
                        m_objects.Add(new ClippedObject(idObj, locked, description));
                    }
                    else // 프로젝트 오브트의 경우
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Object idObj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                        m_objects.Add(new ClippedObject(idObj, locked, description));
                    }
                }
            }
            else
            {
                break;
            }
        }
    }

    private void SetEditorPrefs()
    {
        if (m_objects.Count == 0)
            return;

        // 툴 옵션
        string keyRegCount = "";
        string keySDescription = "";
        SetOptKey(out keyRegCount, out keySDescription);
        EditorPrefs.SetInt(keyRegCount, m_optRegistryCount);
        EditorPrefs.SetBool(keySDescription, m_optShowDescription);

        string keyInsID = "";
        string keyGUID = "";
        string keyLock = "";
        string keyDescription = "";
        int count = Mathf.Min(m_optRegistryCount, m_objects.Count);
        for (int i = 0; i < count; i++)
        {
            SetIndexedKey(i, out keyInsID, out keyGUID, out keyLock, out keyDescription);
            if (m_objects[i].m_object != null)
            {
                if (AssetDatabase.Contains(m_objects[i].m_object))
                {
                    // 프로젝트 애셋
                    EditorPrefs.SetInt(keyInsID, 0);
                    string guid;
                    #if UNITY_2018_2_OR_NEWER
                    long file; // 사용하지 않지만 세팅
                    #else
                    int file; // 사용하지 않지만 세팅
                    #endif
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_objects[i].m_object, out guid, out file))
                    {
                        // GUID를 얻어올 수 있으면
                        EditorPrefs.SetString(keyGUID, guid);
                    }
                    else
                    {
                        // GUID를 얻어오는데 실패하면 (이미 프로젝트 애셋임을 검사한 뒤라서 그럴 일은 없겠지만)
                        EditorPrefs.SetString(keyGUID, "");
                    }
                }
                else
                {
                    // 씬 하이러키 애셋
                    EditorPrefs.SetInt(keyInsID, m_objects[i].m_object.GetInstanceID());
                    EditorPrefs.SetString(keyGUID, "");
                }
                EditorPrefs.SetBool(keyLock, m_objects[i].m_locked);
                EditorPrefs.SetString(keyDescription, m_objects[i].m_description);
            }
            else
            {
                EditorPrefs.SetInt(keyInsID, 0);
                EditorPrefs.SetString(keyGUID, "");
                EditorPrefs.SetBool(keyLock, false);
                EditorPrefs.SetString(keyDescription, "");
            }
        }
    }

    private void UpdateEditorPrefs()
    {
        ClearEditorPrefs();
        SetEditorPrefs();
    }

    private void ClearEditorPrefs()
    {
        // 툴 옵션
        string keyRegCount = "";
        string keySDescription = "";
        SetOptKey(out keyRegCount, out keySDescription);
        if (EditorPrefs.HasKey(keyRegCount))
        {
            EditorPrefs.DeleteKey(keyRegCount);
        }
        if (EditorPrefs.HasKey(keySDescription))
        {
            EditorPrefs.DeleteKey(keySDescription);
        }

        string keyInsID = "";
        string keyGUID = "";
        string keyLock = "";
        string keyDescription = "";
        for (int i = 0; i < mc_maxRegistryLimit; i++)
        {
            SetIndexedKey(i, out keyInsID, out keyGUID, out keyLock, out keyDescription);
            bool breakTest = false;
            if (EditorPrefs.HasKey(keyInsID) == false && EditorPrefs.HasKey(keyGUID) == false && EditorPrefs.HasKey(keyLock) == false && EditorPrefs.HasKey(keyDescription) == false)
            {
                breakTest = true;
            }
            
            if (EditorPrefs.HasKey(keyInsID))
            {
                EditorPrefs.DeleteKey(keyInsID);
            }
            if (EditorPrefs.HasKey(keyGUID))
            {
                EditorPrefs.DeleteKey(keyGUID);
            }
            if (EditorPrefs.HasKey(keyLock))
            {
                EditorPrefs.DeleteKey(keyLock);
            }
            if (EditorPrefs.HasKey(keyDescription))
            {
                EditorPrefs.DeleteKey(keyDescription);
            }

            if (breakTest)
                break;
        }
    }

    private string GetProjectName()
    {
        string[] s = Application.dataPath.Split('/');
        string projectName = s[s.Length - 2];
        return projectName;
    }

    private void InitToggleButtonStyle()
    {
        ToggleButtonStyleNormal = "Button";
        ToggleButtonStyleToggled = new GUIStyle(ToggleButtonStyleNormal);
        Texture2D activeButtonReadOnly = ToggleButtonStyleToggled.active.background;
        Texture2D buttonTex;
        Color[] buttonColors;
        if (activeButtonReadOnly != null)
        {
            // 이쪽 코드에서 유니티 신버전(2019.3)은 buttonTex 를 얻어오지 못해 null 에러가 나고, 구버전에서 에러가 나지 않더라도 rgb 값을 제대로 얻어오지 못하는 문제가 있으나 차후 수정 예정
            buttonTex = new Texture2D(activeButtonReadOnly.width, activeButtonReadOnly.height);
            buttonTex.LoadImage(activeButtonReadOnly.GetRawTextureData()); // 빌트인 UI 텍스쳐는 Read/Write가 꺼져있어서 이런 식으로 우회해서 새로운 텍스쳐럴 복제한다.
            buttonColors = buttonTex.GetPixels();
            for (int i = 0; i < buttonColors.Length; i++)
            {
                // 50% 어두운 빨간 색으로
                buttonColors[i].r *= 0.5f;
                buttonColors[i].g = 0f;
                buttonColors[i].b = 0f;
            }
        }
        else
        {
            buttonTex = new Texture2D(1, 1);
            buttonColors = new Color[] { new Color(0.5f, 0f, 0f) };
        }
        buttonTex.SetPixels(buttonColors);
        buttonTex.Apply();
        ToggleButtonStyleToggled.normal.background = buttonTex;
    }

    // 1컬럼, 2컬럼, 씬에 상관 없이 마지막으로 클릭된 애셋이나 폴더의 경로를 리턴한다. 문제가 있으면 공백 리턴
    private string GetLastActivePath()
    {
        // http://blog.codestage.ru/2015/03/30/select-in-project-browser/ 프로젝트 브라우저 접근하는 방법 소개
        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/ProjectBrowser.cs 프로젝트 브라우저 소스코드 (리플렉션으로 무엇을 가져올지 코드를 알면 편함)
        System.Type projectBrowserType = System.Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
        FieldInfo lastProjectBrowser = projectBrowserType.GetField("s_LastInteractedProjectBrowser", BindingFlags.Static | BindingFlags.Public);
        object lastProjectBrowserInstance = lastProjectBrowser.GetValue(null);

        FieldInfo projectBrowserViewMode = projectBrowserType.GetField("m_ViewMode", BindingFlags.Instance | BindingFlags.NonPublic);
        int viewMode = (int)projectBrowserViewMode.GetValue(lastProjectBrowserInstance);

        string path = "";

        // Asset인 경우에만 경로 세팅
        // 0 - one column, 1 - two column
        if (viewMode == 1)
        {
            // Two column
            MethodInfo activeFolderPathInfo = projectBrowserType.GetMethod("GetActiveFolderPath", BindingFlags.NonPublic | BindingFlags.Instance);
            path = (string)activeFolderPathInfo.Invoke(lastProjectBrowserInstance, new object[] { });
        }
        else
        {
            // One column
            if (Selection.activeObject != null)
            {
                if (AssetDatabase.Contains(Selection.activeObject))
                {
                    // activeObject가 프로젝트에서 선택된 경우
                    path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    if (Directory.Exists(path) == false)
                    {
                        // 폴더가 아닌 애셋이 선택된 경우 path에서 파일명을 없애야함
                        path = Path.GetDirectoryName(path);
                    }
                }
                else
                {
                    // activeObject가 씬에서 선택된 경우
                    return "";
                }
            }
            else
            {
                // null
                return "";
            }
        }

        return path;
    }

    void OnGUI()
    {
        float buttonWidth = 27f;
        float buttonHeight = 22f;
        float menuHeight = 20f;

        // 토글 버튼 스타일 정의
        if (ToggleButtonStyleNormal == null || ToggleButtonStyleToggled.normal.background == null) // ToggleButtonStyleToggled.normal.background 은 에디터 실행 종료 직후 null로 초기화되기때문에 다시 빨간 텍스쳐로 입혀줘야함.
        {
            InitToggleButtonStyle();
        }

        GUILayout.BeginHorizontal();

        GUIContent menuButton = m_menuOpen ? new GUIContent("<", "Settings") : new GUIContent("...", "Settings");

        if (GUILayout.Button(menuButton, GUILayout.Width(30f), GUILayout.Height(menuHeight)))
        {
            m_menuOpen = !m_menuOpen;
        }

        if (GUILayout.Button("Clear", GUILayout.Height(menuHeight)))
        {
            for (int i = 0; i < m_objects.Count; i++)
            {
                if (m_objects[i].m_locked == false)
                    m_objects[i] = new ClippedObject(null, false, "");
            }
            UpdateEditorPrefs();
        }

        if (GUILayout.Button(new GUIContent("▼", "Shift Down"), GUILayout.Height(menuHeight)))
        {
            m_objects.Insert(0, new ClippedObject(null, false, ""));
            UpdateEditorPrefs();
        }

        if (GUILayout.Button(new GUIContent("▲", "Shift Up"), GUILayout.Height(menuHeight)))
        {
            if (m_objects.Count > 1 && m_objects[0].m_locked == false)
            {
                m_objects.RemoveAt(0);
                m_objects.Add(new ClippedObject(null, false, ""));
                UpdateEditorPrefs();
            }
        }

        // 참고용 메모, EditorWindow의 가로 폭은 OnGUI에 기본으로 사용되는 position 변수가 들고있음. https://forum.unity.com/threads/solved-how-to-get-the-size-of-editorwindow.39263/

        GUILayout.Space(12f);

        // 씬 하이러키에서 카메라가 선택된 상태인지 체크
        Camera gameCam = GameCameraSelected();
        if (gameCam == null)
        {
            m_iconCamViewToGame.tooltip = m_iconTooltipCamDisabled;
            m_iconCamGameToView.tooltip = m_iconTooltipCamDisabled;
            GUI.enabled = false;
        }
        else
        {
            m_iconCamViewToGame.tooltip = m_iconTooltipCamViewToGame;
            m_iconCamGameToView.tooltip = m_iconTooltipCamGameToView;
        }

        if (GUILayout.Button(m_iconCamViewToGame, GUILayout.Width(38f), GUILayout.Height(menuHeight)))
        {
            ViewToGameCamera(gameCam);
        }

        if (GUILayout.Button(m_iconCamGameToView, GUILayout.Width(38f), GUILayout.Height(menuHeight)))
        {
            GameToViewCamera(gameCam);
        }

        GUI.enabled = true;

        GUILayout.EndHorizontal();

        // 옵션이 켜져있으면
        if (m_menuOpen)
        {
            EditorGUI.BeginChangeCheck();
            {
                EditorGUI.indentLevel++;
                m_optRegistryCount = EditorGUILayout.IntField(new GUIContent("Registry usage", "The maximum number to write to the Unity Editor registry. The default value is 50, and using too large a value is not recommended.\n에디터 레지스트리에 기록하는 최대 값. 디폴트는 50이며 너무 큰 값을 사용하지 말아주세요."), m_optRegistryCount);
                if (m_optRegistryCount > mc_maxRegistryLimit)
                {
                    m_optRegistryCount = mc_maxRegistryLimit;
                }
                if (m_optRegistryCount < 1)
                {
                    m_optRegistryCount = 1;
                }
                m_optShowDescription = EditorGUILayout.Toggle(new GUIContent("Show description", ""), m_optShowDescription);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                UpdateEditorPrefs();
            }
        }

        // Control 키가 눌려있는지 검사. Input.GetKey 방식은 OnGUI에서 작동하지 않는다. 또한 버튼이 눌러진 직후 검사하면 그 순간에 다른 이벤트가 작동하고있어서 이렇게 미리 이벤트 발생할 때마다 플래그를 세팅해야한다.
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftControl)
        {
            m_controlKeyDown = true;
        }
        if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftControl)
        {
            m_controlKeyDown = false;
        }

        AutoListCount(); // 자동 리스트 카운트 변경

        string newPath = GetLastActivePath(); // 최적화를 위해 for 밖으로 이동
        //GUILayoutUtility.GetRect 해서 텍스트 입력칸의 가로 크기를 비율로 적용해야함.
        // Draw List
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        //EditorGUILayout.PropertyField(m_objectsSerial, true);
        for (int i = 0; i < m_objects.Count;)
        {
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            // 오브젝트 필드에 Tooltip을 띄우려면 동일한 Rect를 사용해서 공백의 Labelfield 를 중첩해야하는데, Rect를 썼을 때 자동 사이즈 조절이 잘 안되서 나중으로 미룸
            m_objects[i].m_object = EditorGUILayout.ObjectField(m_objects[i].m_object, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
                UpdateEditorPrefs();

            if (m_objects[i].m_object != null)
            {
                if (m_optShowDescription)
                {
                    EditorGUI.BeginChangeCheck();
                    m_objects[i].m_description = EditorGUILayout.TextField(m_objects[i].m_description, GUILayout.Width(50f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateEditorPrefs();
                    }
                }
                
                // IconContent의 툴팁은 안나오는데, 아마 아이콘에 포함되어있는듯.
                if (GUILayout.Button(m_iconToolSelect, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                {
                    if (m_objects[i].m_object != null)
                    {
                        Selection.activeObject = m_objects[i].m_object;
                    }
                }

                /*
                // Control 키 입력에 아이콘이 연동되는 기능인데, OnGUI라서 이벤트가 즉각즉각 반응하지 않아서 봉인
                string removeIconName;
                if (m_controlKeyDown)
                {
                    removeIconName = "TreeEditor.Trash";
                }
                else
                {
                    removeIconName = "Grid.EraserTool";
                }
                */

                // 이동버튼은 프로젝트에 없거나 잠겨있거나 경로를 얻어올 수 없는 상태면 비활성
                if (AssetDatabase.Contains(m_objects[i].m_object) == false || m_objects[i].m_locked || newPath == "")
                    GUI.enabled = false;

                if (GUILayout.Button(m_iconToolMove, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                {
                    string oldPath = AssetDatabase.GetAssetPath(m_objects[i].m_object);
                    newPath += "/";
                    newPath += Path.GetFileName(oldPath);
                    if (newPath != "")
                    {
                        string error = AssetDatabase.MoveAsset(oldPath, newPath);
                        if (error != "")
                        {
                            Debug.Log(error);
                        }
                    }
                }

                GUI.enabled = true;
            } // 오브젝트가 비어있어도 삭제버튼부터는 기능이 작동하도록 여기까지만 조건 체크

            // 잠겨있으면 삭제 버튼 비활성화
            if (m_objects[i].m_locked)
                GUI.enabled = false;

            if (GUILayout.Button(m_iconToolTrash, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                // Control 키를 누른 채 지우면 해당 배열이 완전히 제거된다.
                if (m_controlKeyDown)
                {
                    m_objects.RemoveAt(i);
                    UpdateEditorPrefs();
                    continue; // i++ 없이 다음 루프로 진입
                }
                else
                {
                    m_objects[i].m_object = null;
                    UpdateEditorPrefs();
                }
            }
            GUI.enabled = true;
            if (GUILayout.Button(m_iconToolLock, m_objects[i].m_locked ? ToggleButtonStyleToggled : ToggleButtonStyleNormal, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                m_objects[i].m_locked = !m_objects[i].m_locked;
                if (m_objects[i].m_object == null)
                {
                    m_objects[i].m_locked = false;
                }
                UpdateEditorPrefs();
            }
            GUILayout.EndHorizontal();

            i++; // 지워진게 없으면 i++
        }
        EditorGUILayout.EndScrollView();
    }

    // Hierarchy에서 Selection.activeObject 가 카메라면 카메라 리턴
    private Camera GameCameraSelected()
    {
        Object activeObj = Selection.activeObject;
        if (activeObj != null)
        {
            // activeObject가 씬에서 선택된 경우
            if (AssetDatabase.Contains(activeObj) == false)
            {
                // 중복체크. Package Manager 창에서 선택된 오브젝트는 AssetDatabase.Contains 에서 false임.
                // GameObject.Find 만으로 하지 않는 이유는, 이름만으로 체크하기에는 불안해서. 우연히 같은 이름이 있을지도.
                if (GameObject.Find(activeObj.name) != null)
                {
                    GameObject go = activeObj as GameObject;
                    if (go == null) // 유니티 2D본같은 경우 씬에서 선택은 가능하지만 GameObject는 아니라서 null
                    {
                        return null;
                    }
                    Camera gameCam = go.GetComponent<Camera>();
                    if (gameCam != null)
                    {
                        return gameCam;
                    }
                }
            }
        }
        return null;
    }

    private void ViewToGameCamera (Camera gameCam)
    {
        Camera viewCam = SceneView.lastActiveSceneView.camera;
        if (gameCam != null && viewCam != null)
        {
            gameCam.fieldOfView = viewCam.fieldOfView;
            gameCam.transform.position = viewCam.transform.position;
            gameCam.transform.rotation = viewCam.transform.rotation;
        }
    }

    private void GameToViewCamera(Camera gameCam)
    {
        Camera viewCam = SceneView.lastActiveSceneView.camera;
        if (gameCam != null && viewCam != null)
        {
            // 뷰 카메라는 pivot을 중심으로 Orbit 회전함. 그래서 gameCam의 forward 방향으로 특정 거리 위치를 pivot으로 삼아야함 (여기서는 기존 뷰카메라 거리를 재활용)
            // FOV를 먼저 세팅해야 DIstance 관련한 문제가 생기지 않는다. (순서 중요)
            #if UNITY_2019_1_OR_NEWER // 에디터 카메라의 FOV 변경은 2019부터 가능
            SceneView.lastActiveSceneView.cameraSettings.fieldOfView = gameCam.fieldOfView;
            #endif
            SceneView.lastActiveSceneView.pivot = gameCam.transform.TransformPoint(Vector3.forward * SceneView.lastActiveSceneView.cameraDistance);
            SceneView.lastActiveSceneView.rotation = gameCam.transform.rotation;
            SceneView.lastActiveSceneView.Repaint();
        }
    }

    // m_objects 리스트 수를 자동으로 제어한다. 주로 리스트 마지막을 검사 (중간에 빈 것들은 무시)
    private void AutoListCount()
    {
        // 리스트 수가 전혀 없으면 true 리턴
        if (m_objects.Count == 0)
        {
            m_objects.Add(new ClippedObject(null, false, ""));
            UpdateEditorPrefs();
            return;
        }

        int end = m_objects.Count - 1;

        // 리스트 끝이 빈 칸이 아니면 빈 칸을 하나 추가
        if (m_objects[end].m_object != null)
        {
            m_objects.Add(new ClippedObject(null, false, ""));
            UpdateEditorPrefs();
            return;
        }

        // 리스트 끝에 빈 칸이 두 개 있으면 마지막 하나를 지운다
        if (m_objects.Count >= 2) // 일단 리스트가 두 개 이상인 경우만 검사
        {
            if (m_objects[end].m_object == null && m_objects[end - 1].m_object == null)
            {
                m_objects.RemoveAt(end);
                UpdateEditorPrefs();
                return;
            }
        }
    }
}