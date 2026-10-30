using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// Login-skærm: tjekker email + password mod GameBackend (POST /api/game/login).
/// Har kontoen købt spillet, gemmes username/userId/gameId og DungeonScene loades.
///
/// Setup: læg scriptet på et GameObject i LoginScene (første scene i Build Settings).
/// </summary>
public class LoginScreen : MonoBehaviour
{
    [SerializeField] private string baseUrl = "https://localhost:7188";
    [SerializeField] private string gameScene = "DungeonScene";

    public static LoginScreen Instance { get; private set; }

    public bool IsVerified { get; private set; }
    public string Username { get; private set; }
    public int UserId { get; private set; }
    public int GameId { get; private set; }

    private string email = "";
    private string password = "";
    private string errorMessage = "";
    private bool loggingIn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Login()
    {
        loggingIn = true;
        errorMessage = "";

        var body = JsonUtility.ToJson(new LoginRequest { email = email.Trim(), password = password });
        using var request = UnityWebRequest.Post($"{baseUrl.TrimEnd('/')}/api/game/login", body, "application/json");
        yield return request.SendWebRequest();
        loggingIn = false;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"LoginScreen: login fejlede for '{email.Trim()}' ({request.responseCode}): {request.error} {request.downloadHandler.text}", this);
            errorMessage = request.responseCode switch
            {
                401 => "Forkert email eller password.",
                403 => "Kontoen har ikke købt spillet.",
                _ => "Kan ikke nå game-serveren."
            };
            yield break;
        }

        var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
        Username = response.username;
        UserId = response.userId;
        GameId = response.gameId;
        IsVerified = true;
        SceneManager.LoadScene(gameScene);
    }

    //---------LOGIN-SKÆRM---------
    private GUIStyle titleStyle, buttonStyle, fieldStyle, labelStyle;

    private void OnGUI()
    {
        if (IsVerified) return;

        if (titleStyle == null) // GUI.skin findes kun inde i OnGUI, så styles laves her første gang
        {
            titleStyle  = new GUIStyle(GUI.skin.label)     { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button)    { fontSize = 32 };
            fieldStyle  = new GUIStyle(GUI.skin.textField) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
            labelStyle  = new GUIStyle(GUI.skin.label)     { fontSize = 24, alignment = TextAnchor.MiddleCenter };
        }

        // en kolonne midt på skærmen
        float w = 500;
        GUILayout.BeginArea(new Rect((Screen.width - w) / 2, Screen.height * 0.15f, w, Screen.height * 0.8f));
        GUILayout.Label("Dungeon Quest", titleStyle, GUILayout.Height(120));
        GUILayout.Space(40);

        GUILayout.Label("Email", labelStyle);
        email = GUILayout.TextField(email, fieldStyle, GUILayout.Height(60));
        GUILayout.Space(10);
        GUILayout.Label("Password", labelStyle);
        password = GUILayout.PasswordField(password, '*', fieldStyle, GUILayout.Height(60));
        GUILayout.Space(20);

        GUI.enabled = !loggingIn;
        if (GUILayout.Button(loggingIn ? "Logger ind..." : "Log ind", buttonStyle, GUILayout.Height(70))) StartCoroutine(Login());
        GUI.enabled = true;

        if (errorMessage != "") GUILayout.Label(errorMessage, labelStyle);
        GUILayout.EndArea();

        DrawHelpPanel();
    }

    private GUIStyle helpStyle;

    private const string HelpText =
        "<b>Testkonti</b> (password: Test1234)\n" +
        "Markus: markus@dungeon.dk\n" +
        "John: john@dungeon.dk\n" +
        "Gustav: gustav@dungeon.dk\n\n" +
        "<b>\"Kan ikke nå game-serveren\"</b>\n" +
        "cd prj4-2026-09-19_07-44-18\\GameBackend\n" +
        "docker compose up -d\n" +
        "cd GameBackend.Api\n" +
        "dotnet run --launch-profile https";

    private void DrawHelpPanel()
    {
        helpStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.UpperLeft, richText = true, wordWrap = true, padding = new RectOffset(15, 15, 15, 15) };

        float w = 420;
        GUI.Box(new Rect(Screen.width - w - 20, 20, w, 380), HelpText, helpStyle);
    }

    // Matcher LoginRequest og VerifyResponse i GameBackend (ASP.NET bruger camelCase).
    [Serializable]
    private class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    private class LoginResponse
    {
        public int userId;
        public string username;
        public int gameId;
    }
}
