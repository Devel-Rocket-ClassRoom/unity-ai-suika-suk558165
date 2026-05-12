using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "Suika/Fruit Data")]
public class FruitData : ScriptableObject
{
    [Tooltip("0=가장 작은 과일 ... 10=수박")]
    public int level;

    [Tooltip("Inspector에서 끌어 놓을 과일 스프라이트")]
    public Sprite sprite;

    [Tooltip("스프라이트가 없을 때 적용할 색")]
    public Color tint = Color.white;

    [Tooltip("월드 단위 직경 (스케일로 사용)")]
    public float size = 0.5f;

    [Tooltip("Local 콜라이더 반경 (0.5 = 스프라이트 풀사이즈 절반). 자동 계산 권장.")]
    public float colliderRadius = 0.5f;

    [Tooltip("이 과일이 만들어졌을 때 가산되는 점수")]
    public int score = 10;

    [Tooltip("머지 시 재생할 효과음 (옵션)")]
    public AudioClip mergeSfx;
}
