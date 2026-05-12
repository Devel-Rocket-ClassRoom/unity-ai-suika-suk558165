using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Fruit : MonoBehaviour
{
    public int level;
    public bool merged;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(FruitData data)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        level = data.level;
        if (data.sprite != null) sr.sprite = data.sprite;
        sr.color = data.tint;
        transform.localScale = new Vector3(data.size, data.size, 1f);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (merged) return;
        var other = col.gameObject.GetComponent<Fruit>();
        if (other == null || other.merged) return;
        if (other.level != level) return;
        if (level >= SuikaGame.Instance.maxLevel) return;

        // 한 쌍에서 한쪽만 처리 (중복 머지 방지)
        if (GetInstanceID() < other.GetInstanceID()) return;

        merged = true;
        other.merged = true;

        Vector3 mid = (transform.position + other.transform.position) * 0.5f;
        SuikaGame.Instance.MergeAt(mid, level + 1);

        Destroy(other.gameObject);
        Destroy(gameObject);
    }
}
