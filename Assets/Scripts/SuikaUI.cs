using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SuikaGame))]
public class SuikaUI : MonoBehaviour
{
    [Header("UI - Score / Best")]
    public float scoreY = 10f;
    public int scoreFontSize = 32;
    public float bestY = 50f;
    public int bestFontSize = 20;

    [Header("UI - Next 아이콘 (우상단)")]
    public Vector2 nextCardOffset = new Vector2(20f, 20f);
    public Vector2 nextCardSize = new Vector2(110f, 130f);
    public float nextIconSize = 80f;
    public int nextLabelFontSize = 18;

    [Header("UI - Game Over (중앙 기준 px)")]
    public float gameOverTitleOffsetY = -120f;
    public float gameOverResultOffsetY = -20f;
    public float restartButtonOffsetY = 60f;
    public Vector2 restartButtonSize = new Vector2(160f, 50f);
    public float newRecordOffsetY = -100f;

    [Header("UI - Pause (중앙 기준 px)")]
    public float pauseTitleOffsetY = -100f;
    public Vector2 pauseButtonSize = new Vector2(220f, 60f);
    public float pauseButtonSpacing = 14f;
    public float pauseFirstButtonOffsetY = -10f;
    public int pauseButtonFontSize = 24;

    [Header("UI - Pause 버튼 색")]
    public Color resumeColor = new Color(0.55f, 0.80f, 0.40f);
    public Color restartColor = new Color(0.40f, 0.65f, 0.95f);
    public Color quitColor = new Color(0.95f, 0.45f, 0.45f);
    public Color buttonTextColor = Color.white;

    [Header("UI - Volume Settings (좌상단)")]
    public Vector2 volumeOffset = new Vector2(20f, 20f);
    public float volumeSpacing = 10f;
    public float volumeUIScale = 1.15f;
    public Color volumePanelColor = new Color(1f, 0.94f, 0.96f, 0.95f);

    SuikaGame game;
    Texture2D _volumeBgTex;

    void Awake() { game = GetComponent<SuikaGame>(); }

    void OnGUI()
    {
        if (game == null) return;
        DrawScoreAndBest();
        DrawNextFruitIcon();
        DrawVolumeToggles();
        if (game.gameOver) DrawGameOverPanel();
        else if (game.paused) DrawPauseMenu();
    }

    void DrawScoreAndBest()
    {
        var scoreStyle = MakeStyle(scoreFontSize, TextAnchor.UpperCenter, Color.black);
        var bestStyle = MakeStyle(bestFontSize, TextAnchor.UpperCenter, new Color(0.4f, 0.3f, 0.2f));
        GUI.Label(new Rect(0, scoreY, Screen.width, scoreFontSize + 18), "Score: " + game.score, scoreStyle);
        GUI.Label(new Rect(0, bestY, Screen.width, bestFontSize + 10), "Best: " + game.highScore, bestStyle);
    }

    void DrawNextFruitIcon()
    {
        if (game.fruits == null || game.fruits.Length == 0) return;
        var data = game.fruits[game.nextLevel];
        if (data == null || data.sprite == null) return;

        float cardX = Screen.width - nextCardSize.x - nextCardOffset.x;
        float cardY = nextCardOffset.y;
        var cardRect = new Rect(cardX, cardY, nextCardSize.x, nextCardSize.y);

        var prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.Box(cardRect, GUIContent.none);
        GUI.color = prev;

        var labelStyle = MakeStyle(nextLabelFontSize, TextAnchor.UpperCenter, new Color(0.4f, 0.3f, 0.2f));
        labelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(cardX, cardY + 6f, nextCardSize.x, 24f), "NEXT", labelStyle);

        var sprite = data.sprite;
        var tex = sprite.texture;
        var rect = sprite.textureRect;
        var uv = new Rect(rect.x / tex.width, rect.y / tex.height, rect.width / tex.width, rect.height / tex.height);

        float iconX = cardX + (nextCardSize.x - nextIconSize) * 0.5f;
        float iconY = cardY + 32f;
        var iconRect = new Rect(iconX, iconY, nextIconSize, nextIconSize);

        GUI.color = data.tint;
        GUI.DrawTextureWithTexCoords(iconRect, tex, uv);
        GUI.color = prev;
    }

    void DrawVolumeToggles()
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        if (volumeUIScale != 1f)
        {
            GUI.matrix = Matrix4x4.TRS(new Vector3(volumeOffset.x, volumeOffset.y, 0), Quaternion.identity, new Vector3(volumeUIScale, volumeUIScale, 1f));
        }

        float x = 0;
        float y = 0;
        float panelWidth = 240;
        float rowHeight = 32;

        if (_volumeBgTex == null) _volumeBgTex = MakeRoundedTex(volumePanelColor);
        var bgRect = new Rect(x - 8, y - 8, panelWidth, (rowHeight + volumeSpacing) * 2 + 16);
        
        // 마우스 오버 체크
        if (bgRect.Contains(Event.current.mousePosition))
        {
            game.isMouseOverUI = true;
        }

        GUI.DrawTexture(bgRect, _volumeBgTex);

        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.45f, 0.25f, 0.35f) } };

        // BGM
        bool bgmMuted = game.bgmMuted;
        bool newBgmMuted = !GUI.Toggle(new Rect(x, y, 85, rowHeight), !bgmMuted, " 🎵 BGM", labelStyle);
        if (newBgmMuted != bgmMuted) game.SetBGMMuted(newBgmMuted);
        float newBgmVol = GUI.HorizontalSlider(new Rect(x + 90, y + 10, 140, 20), game.bgmVolume, 0f, 1f);
        if (newBgmVol != game.bgmVolume) game.SetBGMVolume(newBgmVol);
        y += rowHeight + volumeSpacing;

        // SFX
        bool sfxMuted = game.sfxMuted;
        bool newSfxMuted = !GUI.Toggle(new Rect(x, y, 85, rowHeight), !sfxMuted, " 🔊 SFX", labelStyle);
        if (newSfxMuted != sfxMuted) game.sfxMuted = newSfxMuted;
        game.sfxVolume = GUI.HorizontalSlider(new Rect(x + 90, y + 10, 140, 20), game.sfxVolume, 0f, 1f);

        GUI.matrix = oldMatrix;
    }

    void DrawGameOverPanel()
    {
        float cx = Screen.width / 2f; float cy = Screen.height / 2f;
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        var overStyle = MakeStyle(72, TextAnchor.MiddleCenter, Color.red); overStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(0, cy - 120, Screen.width, 120), "GAME OVER", overStyle);
        var resultStyle = MakeStyle(36, TextAnchor.UpperCenter, Color.white);
        GUI.Label(new Rect(0, cy - 20, Screen.width, 40), "Score: " + game.score + "   Best: " + game.highScore, resultStyle);
        if (game.isNewRecord) { var recordStyle = MakeStyle(28, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.2f)); recordStyle.fontStyle = FontStyle.Bold; GUI.Label(new Rect(0, cy - 100, Screen.width, 30), "★ 신기록! ★", recordStyle); }
        if (PrettyButton(new Rect(cx - 80, cy + 60, 160, 50), "↻  Restart", restartColor)) game.RestartScene();
    }

    void DrawPauseMenu()
    {
        float cx = Screen.width / 2f; float cy = Screen.height / 2f;
        var prev = GUI.color; GUI.color = new Color(0, 0, 0, 0.55f); GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none); GUI.color = prev;
        var titleStyle = MakeStyle(64, TextAnchor.MiddleCenter, Color.white); titleStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(0, cy - 100, Screen.width, 80), "PAUSED", titleStyle);
        float bw = 220; float bh = 60; float bx = cx - bw / 2f; float by = cy - 10;
        if (PrettyButton(new Rect(bx, by, bw, bh), "▶  Resume", resumeColor)) game.TogglePause(); by += bh + 14;
        if (PrettyButton(new Rect(bx, by, bw, bh), "↻  Restart", restartColor)) game.RestartScene(); by += bh + 14;
        if (PrettyButton(new Rect(bx, by, bw, bh), "✕  Quit", quitColor)) game.QuitGame();
    }

    struct ButtonTextures { public Texture2D normal, hover, active; }
    readonly Dictionary<Color, ButtonTextures> _btnCache = new Dictionary<Color, ButtonTextures>();
    Texture2D _shadowTex; GUIStyle _prettyStyle;

    bool PrettyButton(Rect rect, string label, Color baseColor)
    {
        var tex = GetButtonTextures(baseColor);
        if (_prettyStyle == null) _prettyStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, border = new RectOffset(8, 8, 8, 8), padding = new RectOffset(12, 12, 8, 8) };
        _prettyStyle.fontSize = 24; _prettyStyle.normal.background = tex.normal; _prettyStyle.hover.background = tex.hover; _prettyStyle.active.background = tex.active; _prettyStyle.focused.background = tex.normal;
        _prettyStyle.normal.textColor = Color.white; _prettyStyle.hover.textColor = Color.white; _prettyStyle.active.textColor = Color.white; _prettyStyle.focused.textColor = Color.white;
        if (_shadowTex == null) _shadowTex = MakeRoundedTex(Color.black);
        var shadowRect = new Rect(rect.x, rect.y + 4, rect.width, rect.height);
        var prev = GUI.color; GUI.color = new Color(0, 0, 0, 0.25f); GUI.DrawTexture(shadowRect, _shadowTex); GUI.color = prev;
        return GUI.Button(rect, label, _prettyStyle);
    }

    ButtonTextures GetButtonTextures(Color baseColor)
    {
        if (_btnCache.TryGetValue(baseColor, out var t) && t.normal != null) return t;
        t = new ButtonTextures { normal = MakeRoundedTex(baseColor), hover = MakeRoundedTex(Lighten(baseColor, 0.10f)), active = MakeRoundedTex(Darken(baseColor, 0.15f)) };
        _btnCache[baseColor] = t; return t;
    }

    static Texture2D MakeRoundedTex(Color color)
    {
        int size = 32; int radius = 8; var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp; var px = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            int dx = x < radius ? radius - x : (x >= size - radius ? x - (size - radius - 1) : 0);
            int dy = y < radius ? radius - y : (y >= size - radius ? y - (size - radius - 1) : 0);
            float d = Mathf.Sqrt(dx * dx + dy * dy); float alpha = d < radius - 1 ? 1f : (d < radius ? radius - d : 0f);
            px[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
        }
        tex.SetPixels(px); tex.Apply(); return tex;
    }

    static Color Lighten(Color c, float amount) => new Color(Mathf.Min(1f, c.r + amount), Mathf.Min(1f, c.g + amount), Mathf.Min(1f, c.b + amount), c.a);
    static Color Darken(Color c, float amount) => new Color(Mathf.Max(0f, c.r - amount), Mathf.Max(0f, c.g - amount), Mathf.Max(0f, c.b - amount), c.a);
    static GUIStyle MakeStyle(int fontSize, TextAnchor anchor, Color color) { var s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = anchor }; s.normal.textColor = color; return s; }
}