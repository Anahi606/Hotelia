using System.Collections;
using UnityEngine;

[RequireComponent(typeof(GuestNPC))]
[RequireComponent(typeof(RandomGridRoamer))]
public class NPCTourismQuestionBrain : MonoBehaviour
{
    [Header("Question Chance")]
    public float minCheckDelay = 5f;
    public float maxCheckDelay = 10f;

    [Range(0f, 1f)]
    public float questionChance = 1f;

    [Header("Cooldown")]
    public float minTimeBetweenQuestions = 20f;
    public float maxTimeBetweenQuestions = 40f;

    [Header("Player Detection")]
    public float maxDistanceToPlayer = 8f;
    public float talkDistance = 1.3f;

    [Header("Portrait")]
    public Sprite npcPortraitSprite;

    private GuestNPC npc;
    private RandomGridRoamer roamer;
    private NPCVisibleTravelBrain travelBrain;
    private Transform player;

    private bool isTryingToTalk;
    private float nextAllowedQuestionTime;

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

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("No se encontró Player con tag Player.");

        nextAllowedQuestionTime = Time.time + 2f;

        StartCoroutine(QuestionLoop());
    }

    private IEnumerator QuestionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minCheckDelay, maxCheckDelay));

            if (HotelGamePause.IsPaused)
                continue;

            TryStartQuestionInteraction();
        }
    }

    public bool TryStartQuestionInteraction()
    {
        if (HotelGamePause.IsPaused)
            return false;

        if (isTryingToTalk)
            return false;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("No se encontró Player con tag Player.");
            return false;
        }

        if (TourismQuestionDialogueUI.Instance == null)
        {
            Debug.LogWarning("No existe TourismQuestionDialogueUI en la escena.");
            return false;
        }

        if (TourismQuestionDialogueUI.Instance.IsOpen)
            return false;

        if (TourismQuestionBank.Instance == null)
        {
            Debug.LogWarning("No existe TourismQuestionBank en la escena.");
            return false;
        }

        if (Time.time < nextAllowedQuestionTime)
        {
            Debug.Log("NPC todavía está en cooldown para preguntar.");
            return false;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > maxDistanceToPlayer)
        {
            Debug.Log("NPC no pregunta porque está lejos. Distancia: " + distance);
            return false;
        }

        if (Random.value > questionChance)
        {
            Debug.Log("NPC no preguntó por probabilidad.");
            return false;
        }

        TourismQuestion question = TourismQuestionBank.Instance.GetRandomQuestion();

        if (question == null)
            return false;

        StartCoroutine(ApproachPlayerAndAsk(question));
        return true;
    }

    private IEnumerator ApproachPlayerAndAsk(TourismQuestion question)
    {
        isTryingToTalk = true;

        if (travelBrain != null)
            travelBrain.enabled = false;

        roamer.StopRoaming();

        float timeout = 8f;

        yield return StartCoroutine(
            roamer.FollowTargetUntilClose(player, talkDistance, timeout)
        );

        if (player == null)
        {
            CancelQuestionAttempt();
            yield break;
        }

        float finalDistance = Vector2.Distance(transform.position, player.position);

        if (finalDistance > talkDistance + 0.4f)
        {
            Debug.Log("NPC no alcanzó al player para preguntar. Distancia final: " + finalDistance);
            CancelQuestionAttempt();
            yield break;
        }

        roamer.StopRoaming();

        if (HotelGamePause.IsPaused)
        {
            CancelQuestionAttempt();
            yield break;
        }

        OpenQuestion(question);
    }

    private void CancelQuestionAttempt()
    {
        nextAllowedQuestionTime = Time.time + Random.Range(
            minTimeBetweenQuestions,
            maxTimeBetweenQuestions
        );

        if (travelBrain != null)
            travelBrain.enabled = true;

        roamer.ResumeNormalRoaming();

        isTryingToTalk = false;
    }

    private void OpenQuestion(TourismQuestion question)
    {
        TourismQuestionDialogueUI.Instance.ShowQuestion(
            question,
            npcPortraitSprite,
            OnQuestionFinished
        );
    }

    private void OnQuestionFinished(bool wasCorrect)
    {
        nextAllowedQuestionTime = Time.time + Random.Range(
            minTimeBetweenQuestions,
            maxTimeBetweenQuestions
        );

        if (travelBrain != null)
            travelBrain.enabled = true;

        roamer.ResumeNormalRoaming();

        isTryingToTalk = false;

        Debug.Log(wasCorrect ? "Respuesta correcta." : "Respuesta incorrecta.");
    }
}