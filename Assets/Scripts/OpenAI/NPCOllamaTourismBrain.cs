using System.Collections;
using UnityEngine;

[RequireComponent(typeof(GuestNPC))]
[RequireComponent(typeof(RandomGridRoamer))]
public class NPCOllamaTourismBrain : MonoBehaviour
{
    [Header("Talk Chance")]
    public float minCheckDelay = 5f;
    public float maxCheckDelay = 10f;

    [Range(0f, 1f)]
    public float talkChance = 1f;

    [Header("Cooldown")]
    public float minTimeBetweenTalks = 20f;
    public float maxTimeBetweenTalks = 40f;

    [Header("Player Detection")]
    public float maxDistanceToPlayer = 8f;
    public float talkDistance = 1.3f;

    [Header("Portrait")]
    public Sprite npcPortraitSprite;

    private GuestNPC npc;
    private RandomGridRoamer roamer;
    private NPCVisibleTravelBrain travelBrain;
    private Transform player;

    private Coroutine talkLoopCoroutine;
    private Coroutine approachCoroutine;

    private bool isTryingToTalk;
    private float nextAllowedTalkTime;

    private void Awake()
    {
        npc = GetComponent<GuestNPC>();
        roamer = GetComponent<RandomGridRoamer>();
        travelBrain = GetComponent<NPCVisibleTravelBrain>();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        FindPlayer();

        nextAllowedTalkTime = Time.time + 2f;

        talkLoopCoroutine = StartCoroutine(TalkLoop());
    }
    private void OnEnable()
    {
        HotelGamePause.OnPauseChanged += OnGamePauseChanged;
    }

    private void OnGamePauseChanged(bool paused)
    {
        if (!paused)
            return;

        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
            approachCoroutine = null;
        }

        CancelTalkAttempt();
    }

    private void FindPlayer()
    {
        if (player != null)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("No se encontró Player con tag Player.");
    }

    private IEnumerator TalkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minCheckDelay, maxCheckDelay));

            if (HotelGamePause.IsPaused)
                continue;

            TryStartOllamaInteraction();
        }
    }

    public bool TryStartOllamaInteraction()
    {
        if (HotelGamePause.IsPaused)
            return false;

        if (StudentAIQuestionParametersLoader.Instance != null && !StudentAIQuestionParametersLoader.Instance.HasFinishedLoading)
        {
            return false;
        }

        if (!IsThisNpcValid())
            return false;

        if (isTryingToTalk)
            return false;

        FindPlayer();

        if (player == null)
            return false;

        if (Ollama_Handler.Instance == null)
        {
            Debug.LogWarning("No existe Ollama_Handler en la escena.");
            return false;
        }

        if (Ollama_Handler.Instance.IsOpen)
            return false;

        if (Time.time < nextAllowedTalkTime)
            return false;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > maxDistanceToPlayer)
            return false;

        if (Random.value > talkChance)
            return false;

        approachCoroutine = StartCoroutine(ApproachPlayerAndTalk());
        return true;
    }

    private IEnumerator ApproachPlayerAndTalk()
    {
        isTryingToTalk = true;

        if (HotelGamePause.IsPaused)
        {
            CancelTalkAttempt();
            yield break;
        }

        if (!IsRoamerValid())
        {
            CancelTalkAttempt();
            yield break;
        }

        if (travelBrain != null)
            travelBrain.enabled = false;

        roamer.StopRoaming();

        float timeout = 8f;

        yield return StartCoroutine(
             roamer.FollowTargetUntilClose(player, talkDistance, timeout)
         );

        approachCoroutine = null;

        if (HotelGamePause.IsPaused)
        {
            CancelTalkAttempt();
            yield break;
        }

        if (!IsThisNpcValid() || !IsRoamerValid() || player == null)
        {
            CancelTalkAttempt();
            yield break;
        }

        float finalDistance = Vector2.Distance(transform.position, player.position);

        if (finalDistance > talkDistance + 0.4f)
        {
            CancelTalkAttempt();
            yield break;
        }

        roamer.StopRoaming();

        OpenOllamaDialogue();
    }

    private void OpenOllamaDialogue()
    {
        if (HotelGamePause.IsPaused)
        {
            CancelTalkAttempt();
            return;
        }

        if (StudentAIQuestionParametersLoader.Instance != null && !StudentAIQuestionParametersLoader.Instance.HasFinishedLoading)
        {
            CancelTalkAttempt();
            return;
        }

        if (!IsThisNpcValid())
            return;

        if (Ollama_Handler.Instance == null)
        {
            CancelTalkAttempt();
            return;
        }

        if (Ollama_Handler.Instance.IsOpen)
        {
            CancelTalkAttempt();
            return;
        }

        Ollama_Handler.Instance.OpenDialogue(npcPortraitSprite, OnConversationFinished);
    }

    private void OnConversationFinished()
    {
        if (!IsThisNpcValid())
            return;

        nextAllowedTalkTime = Time.time + Random.Range(
            minTimeBetweenTalks,
            maxTimeBetweenTalks
        );

        if (travelBrain != null)
            travelBrain.enabled = true;

        if (IsRoamerValid())
            roamer.ResumeNormalRoaming();

        isTryingToTalk = false;

        Debug.Log("Conversación con IA terminada.");
    }

    private void CancelTalkAttempt()
    {
        if (!IsThisNpcValid())
            return;

        nextAllowedTalkTime = Time.time + Random.Range(
            minTimeBetweenTalks,
            maxTimeBetweenTalks
        );

        if (travelBrain != null)
            travelBrain.enabled = true;

        if (IsRoamerValid())
            roamer.ResumeNormalRoaming();

        isTryingToTalk = false;
    }

    private bool IsThisNpcValid()
    {
        return this != null && gameObject != null && isActiveAndEnabled;
    }

    private bool IsRoamerValid()
    {
        return roamer != null &&
               roamer.gameObject != null &&
               roamer.isActiveAndEnabled;
    }

    private void OnDisable()
    {
        HotelGamePause.OnPauseChanged -= OnGamePauseChanged;

        if (talkLoopCoroutine != null)
        {
            StopCoroutine(talkLoopCoroutine);
            talkLoopCoroutine = null;
        }

        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
            approachCoroutine = null;
        }

        isTryingToTalk = false;
    }

    private void OnDestroy()
    {
        if (Ollama_Handler.Instance != null && Ollama_Handler.Instance.IsOpen)
        {
            Ollama_Handler.Instance.ClearFinishedCallbackOnly();
        }
    }
}