using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// ═══════════════════════════════════════════════════════════════
//  HƯỚNG DẪN SETUP
//  1. Tạo GameObject tên "LevelManager" trong scene đầu tiên
//  2. Gắn script này vào đó
//  3. (Tùy chọn) Kéo WinPanel UI vào slot winPanel
//  4. Thêm tất cả scene vào Build Settings theo đúng thứ tự màn
// ═══════════════════════════════════════════════════════════════

public class LevelManager : MonoBehaviour
{
    [Header("UI (để trống nếu chưa có)")]
    public GameObject winPanel;
    public UnityEngine.UI.Text levelText;

    [Header("Thời gian chờ trước khi chuyển màn")]
    public float delayBeforeLoad = 1.2f;

    private static LevelManager _instance;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateLevelText();
        if (winPanel != null) winPanel.SetActive(false);
    }

    // Gọi từ BloxorzController.WinSequence()
    public void LoadNextLevel()
    {
        StartCoroutine(WinAndLoad());
    }

    IEnumerator WinAndLoad()
    {
        if (winPanel != null) winPanel.SetActive(true);

        yield return new WaitForSeconds(delayBeforeLoad);

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = "Màn " + (SceneManager.GetActiveScene().buildIndex + 1);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}