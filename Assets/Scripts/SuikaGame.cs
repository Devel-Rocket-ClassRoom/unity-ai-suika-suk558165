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
        GUI.Label(new Rect(0, 10, Screen.width - 20, 50), "Next: lv" + (nextLevel + 1), smallStyle);

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
    }
}
