using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    public float duration = 0.5f;
    float timer = 0f;
    Vector3 startScale;
    SpriteRenderer sr;

    void Start()
    {
        startScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;
        transform.localScale = startScale * (1f + t * 0.5f);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
        }

        if (timer >= duration)
            Destroy(gameObject);
    }
}
