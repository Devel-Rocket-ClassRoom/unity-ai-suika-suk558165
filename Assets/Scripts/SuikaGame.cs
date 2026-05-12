using UnityEngine;

public class SuikaGame : MonoBehaviour
{
    public static SuikaGame Instance;

    [Header("References")]
    [Tooltip("Fruit 프리팹 (SpriteRenderer + Collider2D + Rigidbody2D + Fruit)")]
    public Fruit fruitPrefab;

    [Tooltip("레벨 0부터 순서대로 ScriptableObject 11개")]
    public FruitData[] fruits;

    [Header("Drop")]
    public float spawnY = 4.5f;
    public float minX = -2.5f;
    public float maxX = 2.5f;
    public float dropCooldown = 0.3f;

    [Header("Game Over")]
    public float gameOverY = 4.0f;

    [Header("Spawn Pool")]
    [Tooltip("드롭에서 나올 수 있는 최대 레벨 (이 이상은 머지로만 등장)")]
    public int spawnableMaxLevel = 4;

    public int maxLevel => fruits.Length - 1;
    public int currentLevel;
    public int nextLevel;
    public int score;
    public bool gameOver;
    public bool paused;

    [Header("UI - Pause (중앙 기준 px)")]
    [Tooltip("PAUSED 글자 y 오프셋")]
    public float pauseTitleOffsetY = -100f;

    [Tooltip("일시정지 버튼 가로 크기")]
    public Vector2 pauseButtonSize = new Vector2(180f, 50f);

    [Tooltip("버튼 사이 간격")]
    public float pauseButtonSpacing = 12f;

    [Tooltip("첫 번째 버튼의 y 오프셋")]
    public float pauseFirstButtonOffsetY = -10f;

    GameObject previewObj;
    float lastDropTime = -10f;
    public float LastDropTime => lastDropTime;

    public void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        Time.timeScale = 1f; // pause 중이었어도 정상화
        paused = false;
        if (previewObj != null) previewObj.SetActive(false);
    }

    public void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    void OnDisable()
    {
        // 씬 전환/오브젝트 파괴 시 timeScale 복구
        Time.timeScale = 1f;
    }

    GUIStyle scoreStyle,
        overStyle,
        smallStyle;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (fruits == null || fruits.Length == 0)
        {
            Debug.LogError("[SuikaGame] FruitData 배열이 비었습니다. Inspector에서 채워주세요.");
            enabled = false;
            return;
        }
        currentLevel = Random.Range(0, spawnableMaxLevel + 1);
        nextLevel = Random.Range(0, spawnableMaxLevel + 1);
        ShowPreview();
    }

    void Update()
    {
        // ESC로 일시정지 토글 (게임오버가 아닐 때만)
        if (!gameOver && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (gameOver || paused)
            return;

        float mx = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
        mx = Mathf.Clamp(mx, minX, maxX);
        if (previewObj != null)
            previewObj.transform.position = new Vector3(mx, spawnY, 0);

        if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime >= dropCooldown)
        {
            lastDropTime = Time.time;
            DropFruit(mx);
        }

    }

    void DropFruit(float x)
    {
        SpawnFruit(new Vector3(x, spawnY, 0), currentLevel);
        currentLevel = nextLevel;
        nextLevel = Random.Range(0, spawnableMaxLevel + 1);
        ShowPreview();
    }

    void ShowPreview()
    {
        if (previewObj != null)
            Destroy(previewObj);
        var data = fruits[currentLevel];

        previewObj = new GameObject("Preview");
        var sr = previewObj.AddComponent<SpriteRenderer>();
        sr.sprite = data.sprite;
        var c = data.tint;
        c.a = 0.6f;
        sr.color = c;
        sr.sortingOrder = 10;
        previewObj.transform.position = new Vector3(0, spawnY, 0);
        previewObj.transform.localScale = new Vector3(data.size, data.size, 1);
    }

    public Fruit SpawnFruit(Vector3 pos, int level)
    {
        var data = fruits[level];
        var fruit = Instantiate(fruitPrefab, pos, Quaternion.identity);
        fruit.Init(data);
        return fruit;
    }

    public void MergeAt(Vector3 pos, int newLevel)
    {
        SpawnFruit(pos, newLevel);
        var data = fruits[newLevel];
        score += data.score;
        if (data.mergeSfx != null)
            AudioSource.PlayClipAtPoint(data.mergeSfx, pos);
    }

    void OnGUI()
    {
        if (scoreStyle == null)
        {
            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                alignment = TextAnchor.UpperCenter,
            };
            scoreStyle.normal.textColor = Color.black;
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.UpperRight,
            };
            smallStyle.normal.textColor = Color.black;
            overStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 72,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            overStyle.normal.textColor = Color.red;
        }

        GUI.Label(new Rect(0, 10, Screen.width, 50), "Score: " + score, scoreStyle);
        GUI.Label(
            new Rect(0, 10, Screen.width - 20, 50),
            "Next: lv" + (nextLevel + 1),
            smallStyle
        );

        if (gameOver)
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.Label(
                new Rect(0, Screen.height / 2 - 60, Screen.width, 120),
                "GAME OVER",
                overStyle
            );
            if (
                GUI.Button(
                    new Rect(Screen.width / 2 - 80, Screen.height / 2 + 40, 160, 50),
                    "Restart"
                )
            )
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }
        else if (paused)
        {
            DrawPauseMenu();
        }
    }

    void DrawPauseMenu()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        // 반투명 어두운 배경
        var prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.55f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        GUI.color = prev;

        // "PAUSED" 타이틀
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 64,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        };
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, cy + pauseTitleOffsetY, Screen.width, 80), "PAUSED", titleStyle);

        // 버튼 3개
        float bw = pauseButtonSize.x;
        float bh = pauseButtonSize.y;
        float bx = cx - bw / 2f;
        float by = cy + pauseFirstButtonOffsetY;

        if (GUI.Button(new Rect(bx, by, bw, bh), "Resume"))
        {
            TogglePause();
        }
        by += bh + pauseButtonSpacing;
        if (GUI.Button(new Rect(bx, by, bw, bh), "Restart"))
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
        by += bh + pauseButtonSpacing;
        if (GUI.Button(new Rect(bx, by, bw, bh), "Quit"))
        {
            Time.timeScale = 1f;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
