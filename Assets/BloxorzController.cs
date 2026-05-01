using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BloxorzController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float rollDuration = 0.25f;
    public AnimationCurve rollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Block Dimensions (Base size khi đứng thẳng)")]
    public Vector3 standingSize = new Vector3(1f, 2f, 1f);

    private bool isMoving = false;

    private enum Orientation { Standing, LyingX, LyingZ }
    private Orientation currentOrientation = Orientation.Standing;

    // Optional references — kéo tay trong Inspector, KHÔNG bắt buộc
    // Nếu chưa có 2 script kia thì để trống, game vẫn chạy bình thường
    [Header("─── Optional (để trống nếu chưa có) ───")]
    public MonoBehaviour lightingManager;   // Kéo BloxorzLighting vào đây
    public MonoBehaviour textureManager;    // Kéo BloxorzTextureManager vào đây

    void Start()
    {
        SnapToGrid();
    }

    void Update()
    {
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) StartCoroutine(Roll(Vector3.right));
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) StartCoroutine(Roll(Vector3.left));
        else if (Input.GetKeyDown(KeyCode.UpArrow))   StartCoroutine(Roll(Vector3.forward));
        else if (Input.GetKeyDown(KeyCode.DownArrow)) StartCoroutine(Roll(Vector3.back));
    }

    // ── Gọi lighting/texture qua SendMessage để tránh hard dependency ──
    void CallLighting(string method)
    {
        if (lightingManager != null)
            lightingManager.SendMessage(method, SendMessageOptions.DontRequireReceiver);
    }

    void CallTexture(string method)
    {
        if (textureManager != null)
            textureManager.SendMessage(method, SendMessageOptions.DontRequireReceiver);
        else
            // Tự tìm trên cùng GameObject
            SendMessage(method, SendMessageOptions.DontRequireReceiver);
    }

    IEnumerator Roll(Vector3 direction)
    {
        isMoving = true;

        CallTexture("FlashOnRoll");

        Vector3 size = GetLogicalSize();
        float halfW = size.x / 2f;
        float halfH = size.y / 2f;
        float halfD = size.z / 2f;

        float distToEdge = (direction.x != 0) ? halfW : halfD;
        Vector3 anchor = transform.position
                         + (direction * distToEdge)
                         + (Vector3.down * halfH);

        Vector3 axis = Vector3.Cross(Vector3.up, direction);

        float elapsed   = 0f;
        float lastAngle = 0f;
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t           = Mathf.Clamp01(elapsed / rollDuration);
            float targetAngle = rollCurve.Evaluate(t) * 90f;

            transform.RotateAround(anchor, axis, targetAngle - lastAngle);
            lastAngle = targetAngle;
            yield return null;
        }

        UpdateOrientation(direction);
        SnapToGrid();

        if (CheckGoal())
        {
            StartCoroutine(WinSequence());
            yield break;
        }

        CheckFall();

        if (!IsFalling())
            isMoving = false;
    }

    // ═══════════════════════════════════════════════════════════════
    //  GOAL / WIN LOGIC
    // ═══════════════════════════════════════════════════════════════

    bool CheckGoal()
    {
        if (currentOrientation != Orientation.Standing) return false;

        GameObject goal = GameObject.FindWithTag("Goal");
        if (goal == null) return false;

        float dx = Mathf.Abs(transform.position.x - goal.transform.position.x);
        float dz = Mathf.Abs(transform.position.z - goal.transform.position.z);

        return dx < 0.3f && dz < 0.3f;
    }

    IEnumerator WinSequence()
    {
        isMoving = true;

        CallLighting("TriggerWinLight");
        CallTexture("FlashOnRoll");

        float    timer    = 0f;
        float    duration = 0.55f;
        Vector3  startPos = transform.position;
        Vector3  startScl = transform.localScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            transform.position   = startPos + Vector3.down * (t * 1.5f);
            transform.localScale = Vector3.Lerp(startScl, Vector3.zero, t);
            yield return null;
        }

        gameObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ORIENTATION STATE MACHINE
    // ═══════════════════════════════════════════════════════════════

    void UpdateOrientation(Vector3 direction)
    {
        switch (currentOrientation)
        {
            case Orientation.Standing:
                currentOrientation = (direction.x != 0) ? Orientation.LyingX : Orientation.LyingZ;
                break;
            case Orientation.LyingX:
                currentOrientation = (direction.x != 0) ? Orientation.Standing : Orientation.LyingX;
                break;
            case Orientation.LyingZ:
                currentOrientation = (direction.z != 0) ? Orientation.Standing : Orientation.LyingZ;
                break;
        }
    }

    Vector3 GetLogicalSize()
    {
        switch (currentOrientation)
        {
            case Orientation.LyingX: return new Vector3(standingSize.y, standingSize.x, standingSize.z);
            case Orientation.LyingZ: return new Vector3(standingSize.x, standingSize.z, standingSize.y);
            default:                 return standingSize;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FALL DETECTION
    // ═══════════════════════════════════════════════════════════════

    private bool falling = false;
    private int  groundMask;
    private int  groundOrGoalMask;

    void InitLayerMasks()
    {
        groundMask       = LayerMask.GetMask("Ground");
        groundOrGoalMask = LayerMask.GetMask("Ground", "Goal");
    }

    void CheckFall()
    {
        if (groundMask == 0) InitLayerMasks();

        Vector3 singleCellBox = new Vector3(0.7f, 0.1f, 0.7f);
        bool hasFloor;

        if (currentOrientation == Orientation.Standing)
        {
            hasFloor = IsFloorAt(transform.position, singleCellBox, groundMask);
        }
        else if (currentOrientation == Orientation.LyingX)
        {
            Vector3 cellA = transform.position + new Vector3(-0.5f, 0f, 0f);
            Vector3 cellB = transform.position + new Vector3( 0.5f, 0f, 0f);
            hasFloor = IsFloorAt(cellA, singleCellBox, groundOrGoalMask)
                    && IsFloorAt(cellB, singleCellBox, groundOrGoalMask);
        }
        else
        {
            Vector3 cellA = transform.position + new Vector3(0f, 0f, -0.5f);
            Vector3 cellB = transform.position + new Vector3(0f, 0f,  0.5f);
            hasFloor = IsFloorAt(cellA, singleCellBox, groundOrGoalMask)
                    && IsFloorAt(cellB, singleCellBox, groundOrGoalMask);
        }

        if (!hasFloor)
        {
            falling = true;
            StartCoroutine(FallAndRestart());
        }
    }

    bool IsFalling() => falling;

    bool IsFloorAt(Vector3 pos, Vector3 boxSize, int layerMask)
    {
        Vector3 castOrigin = new Vector3(pos.x, 0.5f, pos.z);
        return Physics.BoxCast(
            castOrigin,
            boxSize / 2f,
            Vector3.down,
            out _,
            Quaternion.identity,
            1.5f,
            layerMask
        );
    }

    IEnumerator FallAndRestart()
    {
        isMoving = true;

        CallLighting("TriggerFallLight");

        float timer = 0f;
        while (timer < 0.8f)
        {
            transform.position += Vector3.down * 12f * Time.deltaTime;
            transform.Rotate(new Vector3(5, 2, 1) * Time.deltaTime * 60f);
            timer += Time.deltaTime;
            yield return null;
        }

        falling  = false;
        isMoving = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ═══════════════════════════════════════════════════════════════
    //  SNAP TO GRID
    // ═══════════════════════════════════════════════════════════════

    void SnapToGrid()
    {
        Vector3 size = GetLogicalSize();
        Vector3 pos  = transform.position;

        pos.x = FixAxis(pos.x, size.x);
        pos.z = FixAxis(pos.z, size.z);
        pos.y = size.y / 2f;

        transform.position = pos;

        Vector3 angles = transform.eulerAngles;
        angles.x = Mathf.Round(angles.x / 90f) * 90f;
        angles.y = Mathf.Round(angles.y / 90f) * 90f;
        angles.z = Mathf.Round(angles.z / 90f) * 90f;
        transform.rotation = Quaternion.Euler(angles);
    }

    float FixAxis(float val, float size)
    {
        if (Mathf.Abs(Mathf.Round(size) - 2f) < 0.1f)
            return Mathf.Floor(val) + 0.5f;
        return Mathf.Round(val);
    }
}