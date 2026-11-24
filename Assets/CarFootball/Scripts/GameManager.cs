using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>2-minute time challenge: score as many goals as possible.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Match")]
    public float matchDuration = 120f;
    [Tooltip("If the ball ends up this far below the pitch (fell out of the arena), restart from kick-off.")]
    public float outOfPlayDepth = 3f;

    [Header("References")]
    public Rigidbody carBody;
    public Rigidbody ballBody;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI fullTimeText;

    int goals;
    float timeLeft;
    bool fullTime;
    Vector3 carStartPos, ballStartPos;
    Quaternion carStartRot, ballStartRot;

    public bool IsFullTime => fullTime;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        carStartPos = carBody.position; carStartRot = carBody.rotation;
        ballStartPos = ballBody.position; ballStartRot = ballBody.rotation;
        timeLeft = matchDuration;
        if (fullTimeText) fullTimeText.gameObject.SetActive(false);
        UpdateScore();
        UpdateTimer();
    }

    void Update()
    {
        if (fullTime)
        {
#if ENABLE_INPUT_SYSTEM
            bool restart = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            bool restart = Input.GetKeyDown(KeyCode.R);
#endif
            if (restart) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : "CarFootballScene");
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateTimer();
            EndMatch();
            return;
        }
        UpdateTimer();

        // Safety net: if the ball ever escapes the arena and falls, bring it back for a new kick-off.
        if (ballBody.position.y < ballStartPos.y - outOfPlayDepth - 2f)
            ResetKickOff();
    }

    public void GoalScored()
    {
        if (fullTime) return;
        goals++;
        UpdateScore();
        ResetKickOff();
    }

    public void ResetKickOff()
    {
        Teleport(carBody, carStartPos, carStartRot);
        Teleport(ballBody, ballStartPos, ballStartRot);
    }

    static void Teleport(Rigidbody rb, Vector3 pos, Quaternion rot)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = pos;
        rb.rotation = rot;
        rb.transform.SetPositionAndRotation(pos, rot);
        rb.Sleep();
    }

    void EndMatch()
    {
        fullTime = true;
        var cc = carBody.GetComponent<CarController>();
        if (cc) cc.enabled = false;
        carBody.linearVelocity = Vector3.zero;
        carBody.angularVelocity = Vector3.zero;
        if (fullTimeText)
        {
            fullTimeText.text = $"FULL TIME\nGoals scored: {goals}\n<size=50%>Press R to play again</size>";
            fullTimeText.gameObject.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    void UpdateScore()
    {
        if (scoreText) scoreText.text = $"Goals: {goals}";
    }

    void UpdateTimer()
    {
        int t = Mathf.CeilToInt(timeLeft);
        if (timerText) timerText.text = $"{t / 60}:{t % 60:00}";
    }
}
