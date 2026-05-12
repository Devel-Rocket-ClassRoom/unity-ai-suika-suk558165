using UnityEngine;

/// <summary>
/// SuikaGame의 모든 IMGUI(OnGUI) 표시를 담당.
/// GameManager에 SuikaGame과 함께 부착.
/// </summary>
[RequireComponent(typeof(SuikaGame))]
public class SuikaUI : MonoBehaviour
{
    [Header("UI - Score / Best")]
    public float scoreY = 10f;
    public int scoreFontSize = 32;
    public float bestY = 50f;
    public int bestFontSize = 20;

    [Header("UI - Next 아이콘 (우상단)")]
    public Vector2 nextCardOffset = new Vector2(20f, 20f); // 우상단 모서리에서 안쪽으로
    public Vector2 nextCardSize = new Vector2(110f, 130f);
    public float nextIconSize = 80f;
    public int nextLabelFontSize = 18;

    [Header("UI - Game Over (중앙 기준 px)")]
    public float gameOverTitleOffsetY = -60f;
    public float gameOverResultOffsetY = 10f;
    public float restartButtonOffsetY = 60f;
    public Vector2 restartButtonSize = new Vector2(160f, 50f);
    public float newRecordOffsetY = -100f;

    [Header("UI - Pause (중앙 기준 px)")]
    public float pauseTitleOffsetY = -100f;
    public Vector2 pauseButtonSize = new Vector2(180f, 50f);
    public float pauseButtonSpacing = 12f;
    public float pauseFirstButtonOffsetY = -10f;

    SuikaGame game;

    void Awake()
    {
        game = GetComponent<SuikaGame>();
    }

    void OnGUI()
    {
        if (game == null)
            return;

        DrawScoreAndBest();
        DrawNextFruitIcon();

        if (game.gameOver)
            DrawGameOverPanel();
        else if (game.paused)
            DrawPauseMenu();
    }

    // ============ 게임 중 UI ============

    void DrawScoreAndBest()
    {
        var scoreStyle = MakeStyle(scoreFontSize, TextAnchor.UpperCenter, Color.black);
        var bestStyle = MakeStyle(bestFontSize, TextAnchor.UpperCenter, new Color(0.4f, 0.3f, 0.2f));

        GUI.Label(
            new Rect(0, scoreY, Screen.width, scoreFontSize + 18),
            "Score: " + game.score,
            scoreStyle
        );
        GUI.Label(
            new Rect(0, bestY, Screen.width, bestFontSize + 10),
            "Best: " + game.highScore,
            bestStyle
        );
    }

    void DrawNextFruitIcon()
    {
        if (game.fruits == null || game.fruits.Length == 0)
            return;
        var data = game.fruits[game.nextLevel];
        if (data == null || data.sprite == null)
            return;

        // 우상단 카드
        float cardX = Screen.width - nextCardSize.x - nextCardOffset.x;
        float cardY = nextCardOffset.y;
        var cardRect = new Rect(cardX, cardY, nextCardSize.x, nextCardSize.y);

        var prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.Box(cardRect, GUIContent.none);
        GUI.color = prev;

        // "NEXT" 라벨
        var labelStyle = MakeStyle(nextLabelFontSize, TextAnchor.UpperCenter, new Color(0.4f, 0.3f, 0.2f));
        labelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(cardX, cardY + 6f, nextCardSize.x, 24f), "NEXT", labelStyle);

        // 아이콘
        var sprite = data.sprite;
        var tex = sprite.texture;
        var rect = sprite.textureRect;
        var uv = new Rect(
            rect.x / tex.width,
            rect.y / tex.height,
            rect.width / tex.width,
            rect.height / tex.height
        );

        float iconX = cardX + (nextCardSize.x - nextIconSize) * 0.5f;
        float iconY = cardY + 32f;
        var iconRect = new Rect(iconX, iconY, nextIconSize, nextIconSize);

        GUI.color = data.tint;
        GUI.DrawTextureWithTexCoords(iconRect, tex, uv);
        GUI.color = prev;
    }

    // ============ 게임오버 UI ============

    void DrawGameOverPanel()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

        var overStyle = MakeStyle(72, TextAnchor.MiddleCenter, Color.red);
        overStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(0, cy + gameOverTitleOffsetY, Screen.width, 120), "GAME OVER", overStyle);

        var resultStyle = MakeStyle(36, TextAnchor.UpperCenter, Color.white);
        GUI.Label(
            new Rect(0, cy + gameOverResultOffsetY, Screen.width, 40),
            "Score: " + game.score + "   Best: " + game.highScore,
            resultStyle
        );

        if (game.isNewRecord)
        {
            var recordStyle = MakeStyle(28, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.2f));
            recordStyle.fontStyle = FontStyle.Bold;
            GUI.Label(
                new Rect(0, cy + newRecordOffsetY, Screen.width, 30),
                "★ 신기록! ★",
                recordStyle
            );
        }

        var btnRect = new Rect(
            cx - restartButtonSize.x / 2f,
            cy + restartButtonOffsetY,
            restartButtonSize.x,
            restartButtonSize.y
        );
        if (GUI.Button(btnRect, "Restart"))
            game.RestartScene();
    }

    // ============ 일시정지 UI ============

    void DrawPauseMenu()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        var prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.55f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        GUI.color = prev;

        var titleStyle = MakeStyle(64, TextAnchor.MiddleCenter, Color.white);
        titleStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(0, cy + pauseTitleOffsetY, Screen.width, 80), "PAUSED", titleStyle);

        float bw = pauseButtonSize.x;
        float bh = pauseButtonSize.y;
        float bx = cx - bw / 2f;
        float by = cy + pauseFirstButtonOffsetY;

        if (GUI.Button(new Rect(bx, by, bw, bh), "Resume"))
            game.TogglePause();
        by += bh + pauseButtonSpacing;
        if (GUI.Button(new Rect(bx, by, bw, bh), "Restart"))
            game.RestartScene();
        by += bh + pauseButtonSpacing;
        if (GUI.Button(new Rect(bx, by, bw, bh), "Quit"))
            game.QuitGame();
    }

    // ============ 헬퍼 ============

    static GUIStyle MakeStyle(int fontSize, TextAnchor anchor, Color color)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = anchor,
        };
        s.normal.textColor = color;
        return s;
    }
}
