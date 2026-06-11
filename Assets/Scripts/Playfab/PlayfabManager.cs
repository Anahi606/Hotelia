using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class PlayfabManager : MonoBehaviour
{
    public static bool IsLoggedInWithEmail { get; private set; }
    public static string CurrentEmail { get; private set; }
    public static string CurrentPlayFabId { get; private set; }
    public static string CurrentRole { get; private set; }

    public static bool IsTeacher => CurrentRole == TeacherRole;
    public static bool IsStudent => CurrentRole == StudentRole;

    private const string StudentRole = "student";
    private const string TeacherRole = "teacher";
    private const string RoleKey = "Role";
    private const string TeacherAccessCodeTitleDataKey = "TeacherAccessCode";

    [Header("Login UI")]
    [SerializeField] private TMP_Text loginMessageText;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Register UI")]
    [SerializeField] private TMP_Text registerMessageText;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerTeacherCodeInput;

    [Header("Panels")]
    [SerializeField] private GameObject loginPanelDialogue;
    [SerializeField] private GameObject buttonsPanel;
    [SerializeField] private GameObject registerPanelDialogue;

    [Header("Main Menu Buttons")]
    [SerializeField] private GameObject loginButton;
    [SerializeField] private GameObject logoutButton;

    private string pendingRegisterRole = StudentRole;

    private void Start()
    {
        UpdateLoginUI();
    }

    public void RegisterButton()
    {
        string email = registerEmailInput != null ? registerEmailInput.text.Trim() : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";
        string teacherCode = registerTeacherCodeInput != null ? registerTeacherCodeInput.text.Trim() : "";

        if (string.IsNullOrEmpty(email))
        {
            SetMessage("Enter an email address.");
            return;
        }

        if (password.Length < 6)
        {
            SetMessage("Password must be at least 6 characters long.");
            return;
        }

        if (string.IsNullOrEmpty(teacherCode))
        {
            RegisterAccount(StudentRole, email, password);
            return;
        }

        RegisterTeacherThroughBackend(email, password, teacherCode);
    }

    private void ValidateTeacherCodeBeforeRegister(string teacherCode, Action<bool> onFinished)
    {
        var request = new GetTitleDataRequest
        {
            Keys = new List<string> { TeacherAccessCodeTitleDataKey }
        };

        PlayFabClientAPI.GetTitleData(
            request,
            result =>
            {
                if (result.Data == null || !result.Data.ContainsKey(TeacherAccessCodeTitleDataKey))
                {
                    Debug.LogError(TeacherAccessCodeTitleDataKey + " was not found in Title Data.");
                    SetMessage("Teacher code configuration was not found.");
                    onFinished?.Invoke(false);
                    return;
                }

                string validCode = result.Data[TeacherAccessCodeTitleDataKey].Trim();

                bool isValid = string.Equals(
                    teacherCode.Trim(),
                    validCode,
                    StringComparison.Ordinal
                );

                Debug.Log("Teacher code validation before register: " + isValid);
                onFinished?.Invoke(isValid);
            },
            error =>
            {
                Debug.LogError("Could not read Title Data: " + error.GenerateErrorReport());
                SetMessage("Could not validate teacher code.");
                onFinished?.Invoke(false);
            }
        );
    }

    private void RegisterAccount(string role, string email, string password)
    {
        pendingRegisterRole = role;

        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnError);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        IsLoggedInWithEmail = true;
        CurrentEmail = registerEmailInput != null ? registerEmailInput.text.Trim() : "";
        CurrentPlayFabId = result.PlayFabId;
        CurrentRole = pendingRegisterRole;

        string displayName = GetNameFromEmail(CurrentEmail);

        SetMessage("Account registered. Saving account role...");
        Debug.Log("PlayFab registration successful. PlayFabId: " + CurrentPlayFabId + " | Role: " + CurrentRole);

        UpdateDisplayName(displayName);

        SaveRoleToPlayFab(CurrentRole, () =>
        {
            FinishRegisterOnlyFlow();
        });
    }

    private void FinishRegisterOnlyFlow()
    {
        PlayFabClientAPI.ForgetAllCredentials();

        IsLoggedInWithEmail = false;
        CurrentEmail = "";
        CurrentPlayFabId = "";
        CurrentRole = "";
        pendingRegisterRole = StudentRole;

        ClearLoginFields();

        if (registerPanelDialogue != null)
            registerPanelDialogue.SetActive(false);

        if (loginPanelDialogue != null)
            loginPanelDialogue.SetActive(true);

        if (buttonsPanel != null)
            buttonsPanel.SetActive(false);

        UpdateLoginUI();

        SetMessage("Account registered. Please log in.");
        Debug.Log("Account registered only. User was logged out and returned to login panel.");
    }

    private void SaveRoleToPlayFab(string role, Action onSuccess)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { RoleKey, role }
        },
            Permission = UserDataPermission.Private
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log("Role saved to PlayFab: " + role);
                onSuccess?.Invoke();
            },
            error =>
            {
                SetMessage("Account created, but role could not be saved.");
                Debug.LogError("Error saving role: " + error.GenerateErrorReport());
            }
        );
    }

    public void LoginButton()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email))
        {
            SetMessage("Enter an email address.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            SetMessage("Enter your password.");
            return;
        }

        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        IsLoggedInWithEmail = true;
        CurrentEmail = emailInput.text.Trim();
        CurrentPlayFabId = result.PlayFabId;

        string displayName = GetNameFromEmail(CurrentEmail);

        SetMessage("Logged in. Checking account role...");
        Debug.Log("Login successful with email. PlayFabId: " + CurrentPlayFabId);

        UpdateDisplayName(displayName);

        LoadRoleFromPlayFab(() =>
        {
            CloseLoginAndReturnToButtons();
            UpdateLoginUI();

            if (IsStudent)
            {
                SetMessage("Student logged in. Syncing local progress...");
                PlayfabCloudSaveManager.UploadLocalSQLiteSaveToPlayFab(OnUploadFinished);
            }
            else if (IsTeacher)
            {
                SetMessage("Teacher logged in.");
                Debug.Log("Teacher logged in. Local SQLite progress will not be uploaded.");
            }
        });
    }

    private void LoadRoleFromPlayFab(Action onFinished)
    {
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { RoleKey }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey(RoleKey))
                {
                    CurrentRole = result.Data[RoleKey].Value;
                }
                else
                {
                    CurrentRole = StudentRole;
                    Debug.LogWarning("No role found. Defaulting to student.");
                }

                Debug.Log("Loaded account role: " + CurrentRole);
                onFinished?.Invoke();
            },
            error =>
            {
                CurrentRole = StudentRole;
                Debug.LogWarning("Could not load role. Defaulting to student. Error: " + error.GenerateErrorReport());
                onFinished?.Invoke();
            }
        );
    }

    public void LogoutButton()
    {
        PlayFabClientAPI.ForgetAllCredentials();

        IsLoggedInWithEmail = false;
        CurrentEmail = "";
        CurrentPlayFabId = "";
        CurrentRole = "";
        pendingRegisterRole = StudentRole;

        ClearLoginFields();

        Debug.Log("Player logged out from PlayFab.");

        if (buttonsPanel != null)
            buttonsPanel.SetActive(true);

        if (loginPanelDialogue != null)
            loginPanelDialogue.SetActive(false);

        UpdateLoginUI();
    }

    private void ClearLoginFields()
    {
        if (emailInput != null)
            emailInput.text = "";

        if (passwordInput != null)
            passwordInput.text = "";

        if (registerEmailInput != null)
            registerEmailInput.text = "";

        if (registerPasswordInput != null)
            registerPasswordInput.text = "";

        if (registerTeacherCodeInput != null)
            registerTeacherCodeInput.text = "";

        if (loginMessageText != null)
            loginMessageText.text = "";

        if (registerMessageText != null)
            registerMessageText.text = "";
    }

    public void OpenLoginPanel()
    {
        ClearLoginFields();

        if (buttonsPanel != null)
            buttonsPanel.SetActive(false);

        if (registerPanelDialogue != null)
            registerPanelDialogue.SetActive(false);

        if (loginPanelDialogue != null)
            loginPanelDialogue.SetActive(true);
    }

    private void CloseLoginAndReturnToButtons()
    {
        ClearLoginFields();

        if (loginPanelDialogue != null)
            loginPanelDialogue.SetActive(false);

        if (registerPanelDialogue != null)
            registerPanelDialogue.SetActive(false);

        if (buttonsPanel != null)
            buttonsPanel.SetActive(true);
    }

    private void UpdateLoginUI()
    {
        if (loginButton != null)
            loginButton.SetActive(!IsLoggedInWithEmail);

        if (logoutButton != null)
            logoutButton.SetActive(IsLoggedInWithEmail);
    }

    public void BackToLoginPanel()
    {
        ClearLoginFields();

        if (registerPanelDialogue != null)
            registerPanelDialogue.SetActive(false);

        if (loginPanelDialogue != null)
            loginPanelDialogue.SetActive(true);

        if (buttonsPanel != null)
            buttonsPanel.SetActive(false);
    }

    private void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
            return;

        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = displayName
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(
            request,
            result =>
            {
                Debug.Log("Display name updated: " + result.DisplayName);
            },
            error =>
            {
                Debug.LogWarning("Could not update display name: " + error.ErrorMessage);
            }
        );
    }

    private string GetNameFromEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "Player";

        int atIndex = email.IndexOf("@");

        if (atIndex <= 0)
            return email;

        string name = email.Substring(0, atIndex);

        name = name.Replace(".", "_");
        name = name.Replace("-", "_");
        name = name.Replace(" ", "_");

        return name;
    }

    private void OnUploadFinished(bool success)
    {
        if (success)
        {
            SetMessage("Login successful. Progress uploaded to PlayFab.");
        }
        else
        {
            SetMessage("Login successful, but no local progress was uploaded.");
        }
    }

    public void ResetPasswordButton()
    {
        string email = emailInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            SetMessage("Enter your email address.");
            return;
        }

        var request = new SendAccountRecoveryEmailRequest
        {
            Email = email,
            TitleId = "1EB598"
        };

        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnPasswordReset, OnError);
    }

    private void OnPasswordReset(SendAccountRecoveryEmailResult result)
    {
        SetMessage("Password recovery email sent.");
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());

        string friendlyMessage = GetFriendlyPlayFabError(error);
        SetMessage(friendlyMessage);
    }

    private string GetFriendlyPlayFabError(PlayFabError error)
    {
        if (error == null)
            return "An unexpected error occurred.";

        switch (error.Error)
        {
            case PlayFabErrorCode.AccountNotFound:
            case PlayFabErrorCode.InvalidEmailOrPassword:
            case PlayFabErrorCode.InvalidUsernameOrPassword:
                return "Email or password is incorrect, or the account does not exist.";

            case PlayFabErrorCode.InvalidEmailAddress:
                return "Enter a valid email address.";

            case PlayFabErrorCode.EmailAddressNotAvailable:
                return "This email is already registered. Please log in instead.";

            case PlayFabErrorCode.InvalidPassword:
                return "Invalid password. It must be at least 6 characters long.";

            case PlayFabErrorCode.ServiceUnavailable:
            case PlayFabErrorCode.DownstreamServiceUnavailable:
                return "PlayFab service is temporarily unavailable. Try again later.";

            case PlayFabErrorCode.ConnectionError:
                return "Could not connect to PlayFab. Check your internet connection.";

            default:
                if (!string.IsNullOrEmpty(error.ErrorMessage) &&
                    error.ErrorMessage.ToLower().Contains("unable to read data"))
                {
                    return "Login failed. Check your email and password, or create an account first.";
                }

                return error.ErrorMessage;
        }
    }

    private void SetMessage(string message)
    {
        if (loginMessageText != null)
            loginMessageText.text = message;

        if (registerMessageText != null)
            registerMessageText.text = message;

        Debug.Log("UI Message: " + message);
    }

    private void RegisterTeacherThroughBackend(string email, string password, string teacherCode)
    {
        StartCoroutine(RegisterTeacherRequest(email, password, teacherCode));
    }

    private IEnumerator RegisterTeacherRequest(string email, string password, string teacherCode)
    {
        SetMessage("Validating teacher account...");

        TeacherRegisterRequestData data = new TeacherRegisterRequestData
        {
            email = email,
            password = password,
            teacherCode = teacherCode
        };

        string json = JsonUtility.ToJson(data);

        string url = "https://TU-AZURE-FUNCTION-URL/api/registerTeacher";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetMessage("Could not register teacher account.");
            Debug.LogError("Teacher backend request failed: " + request.error);
            Debug.LogError("Backend response: " + request.downloadHandler.text);
            yield break;
        }

        TeacherRegisterResponseData response = JsonUtility.FromJson<TeacherRegisterResponseData>(
            request.downloadHandler.text
        );

        if (response == null || !response.success)
        {
            string message = response != null && !string.IsNullOrEmpty(response.message)
                ? response.message
                : "Invalid teacher access code.";

            SetMessage(message);
            Debug.LogWarning("Teacher account was not created.");
            yield break;
        }

        SetMessage("Teacher account created. Please log in.");

        if (registerPanelDialogue != null)
            registerPanelDialogue.SetActive(false);

        if (loginPanelDialogue != null)
            loginPanelDialogue.SetActive(true);

        if (buttonsPanel != null)
            buttonsPanel.SetActive(false);

        ClearLoginFields();
    }
}

[System.Serializable]
public class TeacherRegisterRequestData
{
    public string email;
    public string password;
    public string teacherCode;
}

[System.Serializable]
public class TeacherRegisterResponseData
{
    public bool success;
    public string message;
}