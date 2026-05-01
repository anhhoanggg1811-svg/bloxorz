using UnityEngine;

// ═══════════════════════════════════════════════════════════════
//  HƯỚNG DẪN SETUP TRONG UNITY
//  1. Tạo 1 GameObject (Cube dẹt, Scale: 1, 0.05, 1)
//  2. Gắn script này vào GameObject đó
//  3. Đặt Tag = "Goal" cho GameObject này  ← QUAN TRỌNG NHẤT
//  4. Đặt vị trí đúng trên bản đồ (Y = 0)
//  5. Đảm bảo có Collider (BoxCollider) trên ô đích
// ═══════════════════════════════════════════════════════════════

public class GoalTile : MonoBehaviour
{
    [Header("Màu nhấp nháy")]
    public Color colorA = new Color(0.1f, 0.9f, 1f);   // Cyan
    public Color colorB = new Color(1f, 0.85f, 0f);    // Vàng
    public float pulseSpeed = 2.5f;

    private Renderer goalRenderer;
    private Material goalMat;
    private GameObject[] rings = new GameObject[3];

    void Start()
    {
        goalRenderer = GetComponent<Renderer>();
        goalMat = new Material(goalRenderer.sharedMaterial);
        goalRenderer.material = goalMat;
        CreateRings();
    }

    void Update()
    {
        // Nhấp nháy màu theo sin wave
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        goalMat.color = Color.Lerp(colorA, colorB, t);

        if (goalMat.HasProperty("_EmissionColor"))
        {
            goalMat.SetColor("_EmissionColor", Color.Lerp(colorA, colorB, t) * 0.6f);
            goalMat.EnableKeyword("_EMISSION");
        }

        AnimateRings();
    }

    void CreateRings()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            rings[i] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rings[i].name = "GoalRing_" + i;
            rings[i].transform.SetParent(transform);

            float scale = 0.85f + i * 0.08f;
            rings[i].transform.localScale    = new Vector3(scale, 0.01f, scale);
            rings[i].transform.localPosition = new Vector3(0f, 0.01f + i * 0.005f, 0f);

            Destroy(rings[i].GetComponent<Collider>());

            // Material trong suốt
            Material m = new Material(Shader.Find("Standard"));
            m.color = new Color(colorA.r, colorA.g, colorA.b, 0.35f);
            m.SetFloat("_Mode", 3);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
            rings[i].GetComponent<Renderer>().material = m;
        }
    }

    void AnimateRings()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] == null) continue;

            float speed = (i % 2 == 0 ? 1f : -1f) * (40f + i * 15f);
            rings[i].transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);

            float pulse     = 1f + 0.06f * Mathf.Sin(Time.time * 2f + i * 1.2f);
            float baseScale = 0.85f + i * 0.08f;
            rings[i].transform.localScale = new Vector3(baseScale * pulse, 0.01f, baseScale * pulse);

            Renderer rend = rings[i].GetComponent<Renderer>();
            if (rend != null)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed + i * Mathf.PI / 3f) + 1f) / 2f;
                Color c = Color.Lerp(colorB, colorA, t);
                c.a = 0.3f + 0.2f * Mathf.Sin(Time.time * 3f + i);
                rend.material.color = c;
            }
        }
    }

    // Hiển thị ô đích trong Scene View để dễ đặt vị trí
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one);

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.02f, new Vector3(0.9f, 0.04f, 0.9f));
    }

    void OnDestroy()
    {
        if (goalMat != null) Destroy(goalMat);
        foreach (var r in rings)
            if (r != null) Destroy(r);
    }
}