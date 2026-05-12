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
    public int highScore;
    public bool isNewRecord;
    public bool gameOver;

    const string HighScoreKey = "HighScore";

    [Header("UI - Score / Best (위에서부터 px)")]
    [Tooltip("Score 텍스트의 y 위치 (위에서 px)")]
    public float scoreY = 10f;

    [Tooltip("Score 폰트 크기")]
    public int scoreFontSize = 32;

    [Tooltip("Best 텍스트의 y 위치 (위에서 px)")]
    public float bestY = 50f;

    [Tooltip("Best 폰트 크기")]
    public int bestFontSize = 20;

    [Header("UI - Next 라벨")]
    [Tooltip("Next 텍스트의 우측 여백 (px)")]
    public float nextRightMargin = 20f;

    [Tooltip("Next 텍스트의 y 위치 (위에서 px)")]
    public float nextY = 10f;

    [Tooltip("Next 폰트 크기")]
    public int nextFontSize = 20;

    [Header("UI - Game Over (화면 중앙 기준 오프셋, px)")]
    [Tooltip("GAME OVER 글자 y 오프셋 (중앙 기준)")]
    public float gameOverTitleOffsetY = -60f;

    [Tooltip("최종 점수/베스트 y 오프셋")]
    public float gameOverResultOffsetY = 10f;

    [Tooltip("Restart 버튼 y 오프셋")]
    public float restartButtonOffsetY = 60f;

    [Tooltip("Restart 버튼 크기")]
    public Vector2 restartButtonSize = new Vector2(160f, 50f);

    [Tooltip("'신기록' 메시지 y 오프셋")]
    public float newRecordOffsetY = -100f;

    GameObject previewObj;
    float lastDropTime = -10f;
    public float LastDropTime => lastDropTime;

    Fruit lastDropped;

    public void TriggerGameOver()
    {
        if (gameOver)
            return;
        gameOver = true;
        if (previewObj != null)
            previewObj.SetActive(false);

        // 최고 점수 갱신
        if (score > highScore)
        {
            highScore = score;
            isNewRecord = true;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }

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
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        ShowPreview();
    }

    void Update()
    {
        if (gameOver)
            return;

        // 다음 미리보기는 마지막 드롭 과일이 충분히 내려가야 등장
        if (previewObj == null && IsReadyForNextPreview())
        {
            ShowPreview();
        }

        float mx = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
        mx = Mathf.Clamp(mx, minX, maxX);
        if (previewObj != null)
            previewObj.transform.position = new Vector3(mx, spawnY, 0);

        if (
            previewObj != null
            && Input.GetMouseButtonDown(0)
            && Time.time - lastDropTime >= dropCooldown
        )
        {
            lastDropTime = Time.time;
            DropFruit(mx);
        }
    }

    bool IsReadyForNextPreview()
    {
        if (lastDropped == null)
            return true; // 파괴되었거나(머지됨) 처음 시작
        return lastDropped.hasLanded; // 바닥/벽/다른 과일에 닿은 뒤에만 true
    }

    void DropFruit(float x)
    {
        lastDropped = SpawnFruit(new Vector3(x, spawnY, 0), currentLevel);
        currentLevel = nextLevel;
        nextLevel = Random.Range(0, spawnableMaxLevel + 1);

        // 미리보기 즉시 제거 — IsReadyForNextPreview()가 통과해야 새로 표시
        if (previewObj != null)
        {
            Destroy(previewObj);
            previewObj = null;
        }
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
        // 매 프레임 새 GUIStyle 만들어 폰트 크기 변경 즉시 반영
        var scoreS = new GUIStyle(GUI.skin.label)
        {
            fontSize = scoreFontSize,
            alignment = TextAnchor.UpperCenter,
        };
        scoreS.normal.textColor = Color.black;

        var bestS = new GUIStyle(GUI.skin.label)
        {
            fontSize = bestFontSize,
            alignment = TextAnchor.UpperCenter,
        };
        bestS.normal.textColor = new Color(0.4f, 0.3f, 0.2f);

        var nextS = new GUIStyle(GUI.skin.label)
        {
            fontSize = nextFontSize,
            alignment = TextAnchor.UpperRight,
        };
        nextS.normal.textColor = Color.black;

        var overS = new GUIStyle(GUI.skin.label)
        {
            fontSize = 72,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        };
        overS.normal.textColor = Color.red;

        GUI.Label(new Rect(0, scoreY, Screen.width, scoreFontSize + 18), "Score: " + score, scoreS);
        GUI.Label(new Rect(0, bestY, Screen.width, bestFontSize + 10), "Best: " + highScore, bestS);
        GUI.Label(
            new Rect(0, nextY, Screen.width - nextRightMargin, nextFontSize + 10),
            "Next: lv" + (nextLevel + 1),
            nextS
        );

        if (gameOver)
        {
            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.Label(
                new Rect(0, cy + gameOverTitleOffsetY, Screen.width, 120),
                "GAME OVER",
                overS
            );

            var resultS = new GUIStyle(scoreS)
            {
                fontSize = 36,
            };
            resultS.normal.textColor = Color.white;
            GUI.Label(
                new Rect(0, cy + gameOverResultOffsetY, Screen.width, 40),
                "Score: " + score + "   Best: " + highScore,
                resultS
            );

            if (isNewRecord)
            {
                var recordS = new GUIStyle(scoreS)
                {
                    fontSize = 28,
                    fontStyle = FontStyle.Bold,
                };
                recordS.normal.textColor = new Color(1f, 0.85f, 0.2f);
                GUI.Label(
                    new Rect(0, cy + newRecordOffsetY, Screen.width, 30),
                    "★ 신기록! ★",
                    recordS
                );
            }

            var btnRect = new Rect(
                cx - restartButtonSize.x / 2f,
                cy + restartButtonOffsetY,
                restartButtonSize.x,
                restartButtonSize.y
            );
            if (GUI.Button(btnRect, "Restart"))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }
    }
}
