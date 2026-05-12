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

    // === 상태 ===
    public int maxLevel => fruits.Length - 1;
    public int currentLevel;
    public int nextLevel;
    public int score;
    public int highScore;
    public bool isNewRecord;
    public bool gameOver;
    public bool paused;

    const string HighScoreKey = "HighScore";

    GameObject previewObj;
    float lastDropTime = -10f;
    public float LastDropTime => lastDropTime;

    Fruit lastDropped;

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
        // ESC로 일시정지 토글 (게임오버가 아닐 때만)
        if (!gameOver && Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        if (gameOver || paused)
            return;

        // 다음 미리보기는 마지막 드롭 과일이 무언가에 닿은 뒤 등장
        if (previewObj == null && IsReadyForNextPreview())
            ShowPreview();

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
            return true;
        return lastDropped.hasLanded;
    }

    void DropFruit(float x)
    {
        lastDropped = SpawnFruit(new Vector3(x, spawnY, 0), currentLevel);
        currentLevel = nextLevel;
        nextLevel = Random.Range(0, spawnableMaxLevel + 1);

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

    public void TriggerGameOver()
    {
        if (gameOver)
            return;
        gameOver = true;
        Time.timeScale = 1f;
        paused = false;
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

    public void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDisable()
    {
        // 씬 전환/오브젝트 파괴 시 timeScale 복구
        Time.timeScale = 1f;
    }
}
