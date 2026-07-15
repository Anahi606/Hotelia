using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

public class HoteliaSceneEditModeTests
{
    [TestCase("03 - Bedroom")]
    [TestCase("05 - Restaurant")]
    [TestCase("07 - TeacherDashboard")]
    public void SceneAsset_Exists(string sceneName)
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(sceneName),
            "The scene name cannot be empty."
        );

        string[] sceneGuids = AssetDatabase.FindAssets(
            $"t:Scene {sceneName}"
        );

        string[] matchingScenePaths = sceneGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path =>
                string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    sceneName,
                    StringComparison.Ordinal
                )
            )
            .ToArray();

        Assert.AreEqual(
            1,
            matchingScenePaths.Length,
            matchingScenePaths.Length == 0
                ? $"Scene asset '{sceneName}' was not found in the project."
                : $"More than one scene named '{sceneName}' was found. " +
                  "Scene names must be unique."
        );
    }

    [TestCase("03 - Bedroom")]
    [TestCase("05 - Restaurant")]
    [TestCase("07 - TeacherDashboard")]
    public void Scene_IsEnabledInActiveBuildProfile(string sceneName)
    {
        EditorBuildSettingsScene matchingScene =
            EditorBuildSettings.scenes.FirstOrDefault(scene =>
                string.Equals(
                    Path.GetFileNameWithoutExtension(scene.path),
                    sceneName,
                    StringComparison.Ordinal
                )
            );

        Assert.IsNotNull(
            matchingScene,
            $"Scene '{sceneName}' is not included in the active " +
            "Build Profile Scene List."
        );

        Assert.IsTrue(
            matchingScene.enabled,
            $"Scene '{sceneName}' exists in the active Build Profile, " +
            "but its checkbox is disabled."
        );
    }

    [Test]
    public void HotelScene_IsConfiguredForGameplayTests()
    {
        const string hotelSceneName = "02 - Hotel";

        EditorBuildSettingsScene hotelScene =
            EditorBuildSettings.scenes.FirstOrDefault(scene =>
                string.Equals(
                    Path.GetFileNameWithoutExtension(scene.path),
                    hotelSceneName,
                    StringComparison.Ordinal
                )
            );

        Assert.IsNotNull(
            hotelScene,
            $"The required initialization scene '{hotelSceneName}' " +
            "is not included in the active Build Profile."
        );

        Assert.IsTrue(
            hotelScene.enabled,
            $"The required initialization scene '{hotelSceneName}' " +
            "is disabled in the active Build Profile."
        );
    }
}