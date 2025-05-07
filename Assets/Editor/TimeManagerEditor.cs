using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class TimeManagerEditor : EditorWindow
{
    private bool isWorking = false;
    private float sessionTimer = 0f;
    private float lastUpdateTime;
    private float reminderInterval = 1500f; // Default to 25 minutes in seconds
    private float timeSinceLastReminder = 0f;
    private int reminderMinutes = 25;
    private int dailyGoalMinutes = 240; // 4 hours default
    private string logFilePath;
    private List<string> sessionLogs = new List<string>();

    private string workingMessage = "💻 You are currently working";
    private string breakMessage = "☕ You are on a break";
    private bool silentReminders = false;
    private List<string> motivationalMessages = new List<string> {
        "Take a deep breath! 🌬️",
        "Stretch your legs! 🦵",
        "You’re doing great! 🌟",
        "Hydrate a bit! 💧",
        "Time to refocus soon! 🎯"
    };

    [MenuItem("Tools/Dev Time Tracker")]
    public static void ShowWindow()
    {
        GetWindow<TimeManagerEditor>("Dev Time Tracker");
    }

    void OnEnable()
    {
        EditorApplication.update += Update;
        lastUpdateTime = Time.realtimeSinceStartup;
        logFilePath = Path.Combine(Application.dataPath, "Editor/DevTimeLog.txt");
        LoadSessionLog();
        reminderMinutes = EditorPrefs.GetInt("TimeTracker_ReminderMinutes", 25);
        workingMessage = EditorPrefs.GetString("TimeTracker_WorkingMsg", workingMessage);
        breakMessage = EditorPrefs.GetString("TimeTracker_BreakMsg", breakMessage);
        dailyGoalMinutes = EditorPrefs.GetInt("TimeTracker_DailyGoal", 240);
        silentReminders = EditorPrefs.GetBool("TimeTracker_Silent", false);
        reminderInterval = reminderMinutes * 60f;
    }

    void OnDisable()
    {
        EditorApplication.update -= Update;
        SaveSessionLog();
    }

    void Update()
    {
        if (!isWorking) return;

        float currentTime = Time.realtimeSinceStartup;
        float deltaTime = currentTime - lastUpdateTime;
        sessionTimer += deltaTime;
        timeSinceLastReminder += deltaTime;
        lastUpdateTime = currentTime;

        if (timeSinceLastReminder >= reminderInterval)
        {
            ShowReminder();
            timeSinceLastReminder = 0f;
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Developer Time Tracker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Session Time:", FormatTime(sessionTimer));
        float dailyGoalSeconds = dailyGoalMinutes * 60f;
        float progress = Mathf.Clamp01(sessionTimer / dailyGoalSeconds);
        EditorGUILayout.LabelField("Daily Goal:", FormatTime(dailyGoalSeconds));
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{(int)(progress * 100)}% of daily goal");

        EditorGUILayout.Space();
        reminderMinutes = EditorGUILayout.IntSlider("Break reminder every (minutes):", reminderMinutes, 5, 120);
        reminderInterval = reminderMinutes * 60f;
        EditorPrefs.SetInt("TimeTracker_ReminderMinutes", reminderMinutes);

        dailyGoalMinutes = EditorGUILayout.IntSlider("Daily Goal (minutes):", dailyGoalMinutes, 30, 600);
        EditorPrefs.SetInt("TimeTracker_DailyGoal", dailyGoalMinutes);

        silentReminders = EditorGUILayout.Toggle("Use Silent Reminders (no popup)", silentReminders);
        EditorPrefs.SetBool("TimeTracker_Silent", silentReminders);

        workingMessage = EditorGUILayout.TextField("Working Message:", workingMessage);
        breakMessage = EditorGUILayout.TextField("Break Message:", breakMessage);
        EditorPrefs.SetString("TimeTracker_WorkingMsg", workingMessage);
        EditorPrefs.SetString("TimeTracker_BreakMsg", breakMessage);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(isWorking ? "Pause Work" : "Start Work"))
        {
            if (isWorking)
            {
                isWorking = false;
                SaveSessionLog();
            }
            else
            {
                isWorking = true;
                lastUpdateTime = Time.realtimeSinceStartup;
            }
        }

        if (GUILayout.Button("Reset Session"))
        {
            if (EditorUtility.DisplayDialog("Reset Timer", "Are you sure you want to reset the session?", "Yes", "No"))
            {
                SaveSessionLog();
                sessionTimer = 0f;
                timeSinceLastReminder = 0f;
                isWorking = false;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            isWorking ? workingMessage : breakMessage,
            isWorking ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.Space();
        GUILayout.Label("Session Logs", EditorStyles.boldLabel);

        foreach (var entry in sessionLogs)
        {
            EditorGUILayout.LabelField("- " + entry);
        }
    }

    void ShowReminder()
    {
        string message = motivationalMessages[UnityEngine.Random.Range(0, motivationalMessages.Count)];
        if (!silentReminders)
        {
            EditorUtility.DisplayDialog("Break Reminder!", $"You've been working for {reminderMinutes} minutes. {message}", "Ok");
        }
        else
        {
            ShowNotification(new GUIContent($"Break time! {message}"));
        }
    }

    string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
    }

    void SaveSessionLog()
    {
        if (sessionTimer <= 1f) return; // skip very short sessions

        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - {FormatTime(sessionTimer)}";
        sessionLogs.Insert(0, logEntry);

        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        File.AppendAllText(logFilePath, logEntry + "\n");

        sessionTimer = 0f;
    }

    void LoadSessionLog()
    {
        if (File.Exists(logFilePath))
        {
            string[] lines = File.ReadAllLines(logFilePath);
            sessionLogs = new List<string>(lines);
            sessionLogs.Reverse();
        }
    }
}
