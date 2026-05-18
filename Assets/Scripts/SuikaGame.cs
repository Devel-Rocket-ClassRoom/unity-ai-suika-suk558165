using UnityEngine;

public class SuikaGame : MonoBehaviour
{
    public static SuikaGame Instance;

    [Header("References")]
    public Fruit fruitPrefab;
    public FruitData[] fruits;

    [Header("Drop")]
    public float spawnY = 4.5f;
    public float minX = -2.5f;
    public float maxX = 2.5f;
    public float dropCooldown = 0.3f;

    [Header("Game Over")]
    public float gameOverY = 4.0f;

    [Header("Spawn Pool")]
    public int spawnableMaxLevel = 4;

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip mergeSfx;
    private AudioSource musicSource;

    [Header("Effects")]
    public GameObject mergeEffectPrefab;

    [Header("Audio Settings")]
    public bool sfxMuted;
    public bool bgmMuted;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;

    [HideInInspector] public bool isMouseOverUI;

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
        if (backgroundMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
    }

    void Start()
    {
        if (fruits == null || fruits.Length == 0) return;
        currentLevel = Random.Range(0, spawnableMaxLevel + 1);
        nextLevel = Random.Range(0, spawnableMaxLevel + 1);
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (musicSource != null) musicSource.Play();
        ShowPreview();
    }

    void Update()
    {
        if (!gameOver && Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (gameOver || paused) return;

        if (previewObj == null && IsReadyForNextPreview()) ShowPreview();

        float mx = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
        mx = Mathf.Clamp(mx, minX, maxX);
        if (previewObj != null) previewObj.transform.position = new Vector3(mx, spawnY, 0);

        // UI 위에 없을 때만 드롭 허용
        if (!isMouseOverUI && previewObj != null && Input.GetMouseButtonDown(0) && Time.time - lastDropTime >= dropCooldown)
        {
            lastDropTime = Time.time;
            DropFruit(mx);
        }
        
        if (musicSource != null) musicSource.volume = bgmVolume;

        // 매 프레임 리셋 (UI에서 체크함)
        isMouseOverUI = false;
    }

    bool IsReadyForNextPreview()
    {
        if (lastDropped == null) return true;
        return lastDropped.hasLanded;
    }

    void DropFruit(float x)
    {
        lastDropped = SpawnFruit(new Vector3(x, spawnY, 0), currentLevel);
        if (!sfxMuted)
        {
            var data = fruits[currentLevel];
            if (data.dropSfx != null) AudioSource.PlayClipAtPoint(data.dropSfx, lastDropped.transform.position, sfxVolume);
        }
        currentLevel = nextLevel;
        nextLevel = Random.Range(0, spawnableMaxLevel + 1);
        if (previewObj != null) { Destroy(previewObj); previewObj = null; }
    }

    void ShowPreview()
    {
        if (previewObj != null) Destroy(previewObj);
        var data = fruits[currentLevel];
        previewObj = new GameObject("Preview");
        var sr = previewObj.AddComponent<SpriteRenderer>();
        sr.sprite = data.sprite;
        var c = data.tint; c.a = 0.6f; sr.color = c;
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
        AudioClip clipToPlay = data.mergeSfx != null ? data.mergeSfx : mergeSfx;
        if (!sfxMuted && clipToPlay != null) AudioSource.PlayClipAtPoint(clipToPlay, pos, sfxVolume);
        if (mergeEffectPrefab != null) Instantiate(mergeEffectPrefab, pos, Quaternion.identity);
    }

    public void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        Time.timeScale = 1f; paused = false;
        if (musicSource != null) musicSource.Stop();
        if (previewObj != null) previewObj.SetActive(false);
        if (score > highScore) { highScore = score; PlayerPrefs.SetInt(HighScoreKey, highScore); PlayerPrefs.Save(); isNewRecord = true; }
    }

    public void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        if (musicSource != null) { if (paused) musicSource.Pause(); else if (!bgmMuted) musicSource.UnPause(); }
    }

    public void SetBGMMuted(bool muted)
    {
        bgmMuted = muted;
        if (musicSource != null) { if (bgmMuted) musicSource.Pause(); else if (!paused) musicSource.UnPause(); }
    }

    public void SetBGMVolume(float volume) { bgmVolume = volume; if (musicSource != null) musicSource.volume = bgmVolume; }
    public void RestartScene() { Time.timeScale = 1f; UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex); }
    public void QuitGame() { Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void OnDisable() { Time.timeScale = 1f; }
}