using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    [SerializeField] private RectTransform cursorUI;
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Cursor.lockState = CursorLockMode.None;
    }

    void Start()
    {
        Cursor.visible = false;

        #if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalEval(@"
                document.body.style.cursor = 'none';
                var canvas = document.getElementById('unity-canvas');
                if (canvas) canvas.style.cursor = 'none';
                
                var style = document.createElement('style');
                style.innerHTML = '* { cursor: none !important; }';
                document.head.appendChild(style);
            ");
        #endif
    }

    void Update()
    {
        UpdateCursor();

        if (Cursor.visible) Cursor.visible = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.visible = false;
        }
    }

    private void UpdateCursor()
    {
        if (cursorUI == null || canvas == null) return;

        Vector2 movePos;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            cursorUI.position = Input.mousePosition;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out movePos
            );
            cursorUI.anchoredPosition = movePos;
        }
    }
}