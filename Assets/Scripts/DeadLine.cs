using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데드라인 트리거 영역. 과일이 라인에 닿은 상태가 holdTime(기본 5초) 이상이면 게임오버.
/// GameOverLine GameObject에 BoxCollider2D(isTrigger=true)와 함께 부착.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DeadLine : MonoBehaviour
{
    [Tooltip("이 시간(초) 이상 닿아 있으면 게임오버")]
    public float holdTime = 5f;

    [Tooltip("드롭 후 이 시간까지는 카운트하지 않음 (드롭 직후 일시 통과 무시)")]
    public float dropGrace = 0.5f;

    [Tooltip("디버그용 — 현재 닿아 있는 시간")]
    public float currentHold;

    readonly HashSet<Fruit> contacts = new HashSet<Fruit>();

    void Reset()
    {
        var bc = GetComponent<BoxCollider2D>();
        bc.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var f = other.GetComponent<Fruit>();
        if (f != null) contacts.Add(f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var f = other.GetComponent<Fruit>();
        if (f != null) contacts.Remove(f);
    }

    void Update()
    {
        if (SuikaGame.Instance == null || SuikaGame.Instance.gameOver) return;

        // 파괴된 과일 정리
        contacts.RemoveWhere(f => f == null);

        // 드롭 직후 grace 기간엔 카운트 중지
        bool inGrace = Time.time - SuikaGame.Instance.LastDropTime < dropGrace;

        if (contacts.Count > 0 && !inGrace)
        {
            currentHold += Time.deltaTime;
            if (currentHold >= holdTime)
            {
                SuikaGame.Instance.TriggerGameOver();
                currentHold = 0f;
            }
        }
        else
        {
            currentHold = 0f;
        }
    }
}
