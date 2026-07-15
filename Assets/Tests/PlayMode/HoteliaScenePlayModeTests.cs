using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class HoteliaScenePlayModeTests
{
    private const string HotelSceneName = "02 - Hotel";
    private const string BedroomSceneName = "03 - Bedroom";
    private const string RestaurantSceneName = "05 - Restaurant";
    private const string TeacherDashboardSceneName =
        "07 - TeacherDashboard";

    private const int MaximumInitializationFrames = 300;

    [UnityTest]
    public IEnumerator BedroomScene_LoadsSuccessfully()
    {
        yield return LoadHotelThenGameplayScene(
            BedroomSceneName
        );
    }

    [UnityTest]
    public IEnumerator RestaurantScene_LoadsSuccessfully()
    {
        yield return LoadHotelThenGameplayScene(
            RestaurantSceneName
        );
    }

    [UnityTest]
    public IEnumerator TeacherDashboardScene_LoadsSuccessfully()
    {
        yield return LoadStandaloneScene(
            TeacherDashboardSceneName
        );
    }

    private IEnumerator LoadHotelThenGameplayScene(
        string targetSceneName
    )
    {
        yield return LoadSceneAndWait(HotelSceneName);

        MonoBehaviour hotelGameData =
            FindComponentByClassName("HotelGameData");

        int waitedFrames = 0;

        while (
            hotelGameData == null &&
            waitedFrames < MaximumInitializationFrames
        )
        {
            waitedFrames++;
            yield return null;

            hotelGameData =
                FindComponentByClassName("HotelGameData");
        }

        Assert.IsNotNull(
            hotelGameData,
            $"Scene '{HotelSceneName}' loaded, but it did not create " +
            "an active HotelGameData component."
        );

        yield return LoadSceneAndWait(targetSceneName);

        ValidateActiveScene(targetSceneName);

        MonoBehaviour persistentHotelGameData =
            FindComponentByClassName("HotelGameData");

        Assert.IsNotNull(
            persistentHotelGameData,
            $"HotelGameData disappeared after loading " +
            $"'{targetSceneName}'. Verify that HotelGameData calls " +
            "DontDestroyOnLoad."
        );
    }

    private IEnumerator LoadStandaloneScene(string sceneName)
    {
        yield return LoadSceneAndWait(sceneName);

        ValidateActiveScene(sceneName);
    }

    private IEnumerator LoadSceneAndWait(string sceneName)
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(sceneName),
            "The scene name cannot be empty."
        );

        Assert.IsTrue(
            Application.CanStreamedLevelBeLoaded(sceneName),
            $"Scene '{sceneName}' cannot be loaded. Add it to " +
            "File → Build Profiles → Scene List and enable it."
        );

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single
            );

        Assert.IsNotNull(
            loadOperation,
            $"Unity could not begin loading scene '{sceneName}'."
        );

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;
    }

    private static void ValidateActiveScene(string expectedSceneName)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        Assert.IsTrue(
            activeScene.IsValid(),
            $"The active scene after loading '{expectedSceneName}' " +
            "is not valid."
        );

        Assert.IsTrue(
            activeScene.isLoaded,
            $"Scene '{expectedSceneName}' did not finish loading."
        );

        Assert.AreEqual(
            expectedSceneName,
            activeScene.name,
            $"Expected scene '{expectedSceneName}', but Unity loaded " +
            $"'{activeScene.name}'."
        );
    }

    private static MonoBehaviour FindComponentByClassName(
        string className
    )
    {
        MonoBehaviour[] behaviours =
            Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            if (behaviour.GetType().Name == className)
                return behaviour;
        }

        return null;
    }

    [UnityTearDown]
    public IEnumerator CleanUpAfterTest()
    {
        MonoBehaviour hotelGameData =
            FindComponentByClassName("HotelGameData");

        if (hotelGameData != null)
        {
            Object.Destroy(hotelGameData.gameObject);
            yield return null;
        }
    }
}