using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RestaurantSceneSmokeTests
{
    private const string HotelSceneName = "02 - Hotel";

    private const string BedroomSceneName = "03 - Bedroom";
    private const string RestaurantSceneName = "05 - Restaurant";
    private const string TeacherPanelSceneName = "07 - TeacherDashboard";

    private const int MaximumWaitFrames = 300;

    [UnityTest]
    public IEnumerator BedroomScene_LoadsSuccessfully()
    {
        yield return LoadHotelThenTargetScene(BedroomSceneName);
    }

    [UnityTest]
    public IEnumerator RestaurantScene_LoadsSuccessfully()
    {
        yield return LoadHotelThenTargetScene(RestaurantSceneName);
    }

    [UnityTest]
    public IEnumerator TeacherPanelScene_LoadsSuccessfully()
    {
        yield return LoadHotelThenTargetScene(TeacherPanelSceneName);
    }

    private IEnumerator LoadHotelThenTargetScene(string targetSceneName)
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(targetSceneName),
            "The target scene name cannot be empty."
        );

        yield return LoadHotelScene();

        MonoBehaviour hotelGameDataBeforeSceneChange =
            FindHotelGameDataComponent();

        Assert.IsNotNull(
            hotelGameDataBeforeSceneChange,
            $"The scene '{HotelSceneName}' loaded, but no active " +
            "HotelGameData component was found."
        );

        yield return LoadScene(targetSceneName);

        Scene activeScene = SceneManager.GetActiveScene();

        Assert.IsTrue(
            activeScene.IsValid(),
            "The active scene is not valid."
        );

        Assert.IsTrue(
            activeScene.isLoaded,
            $"The scene '{targetSceneName}' did not finish loading."
        );

        Assert.AreEqual(
            targetSceneName,
            activeScene.name,
            $"Expected scene '{targetSceneName}', but the active scene " +
            $"is '{activeScene.name}'."
        );

        MonoBehaviour hotelGameDataAfterSceneChange =
            FindHotelGameDataComponent();

        Assert.IsNotNull(
            hotelGameDataAfterSceneChange,
            $"HotelGameData disappeared after loading '{targetSceneName}'. " +
            "Verify that HotelGameData calls DontDestroyOnLoad."
        );
    }

    private IEnumerator LoadHotelScene()
    {
        Assert.IsTrue(
            Application.CanStreamedLevelBeLoaded(HotelSceneName),
            $"The initial scene '{HotelSceneName}' cannot be loaded. " +
            "Check its exact name and add it to Build Profiles."
        );

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
            HotelSceneName,
            LoadSceneMode.Single
        );

        Assert.IsNotNull(
            loadOperation,
            $"Unity could not begin loading '{HotelSceneName}'."
        );

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        int waitedFrames = 0;

        while (
            FindHotelGameDataComponent() == null &&
            waitedFrames < MaximumWaitFrames
        )
        {
            waitedFrames++;
            yield return null;
        }

        Assert.IsNotNull(
            FindHotelGameDataComponent(),
            $"No HotelGameData component was found after loading " +
            $"'{HotelSceneName}'. Verify that the scene contains an active " +
            "GameObject with the HotelGameData component."
        );
    }

    private IEnumerator LoadScene(string sceneName)
    {
        Assert.IsTrue(
            Application.CanStreamedLevelBeLoaded(sceneName),
            $"The scene '{sceneName}' cannot be loaded. " +
            "Add it to File → Build Profiles → Scene List."
        );

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
            sceneName,
            LoadSceneMode.Single
        );

        Assert.IsNotNull(
            loadOperation,
            $"Unity could not begin loading '{sceneName}'."
        );

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;
    }

    private static MonoBehaviour FindHotelGameDataComponent()
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

            if (behaviour.GetType().Name == "HotelGameData")
                return behaviour;
        }

        return null;
    }

    [UnityTearDown]
    public IEnumerator CleanUpAfterTest()
    {
        MonoBehaviour hotelGameData = FindHotelGameDataComponent();

        if (hotelGameData != null)
        {
            Object.Destroy(hotelGameData.gameObject);
            yield return null;
        }
    }
}