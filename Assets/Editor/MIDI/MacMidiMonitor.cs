#if UNITY_EDITOR_OSX
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XRMidi;
using XRMidi.Network;

public sealed class MacMidiMonitor : EditorWindow
{
    private MacMidiInput input;
    private List<KeyValuePair<uint, string>> sources = new List<KeyValuePair<uint, string>>();
    private readonly Queue<string> notes = new Queue<string>();
    private int selected, onCount, offCount, forwardedCount;
    private string status = "Not connected";
    private RhythmDemoController lesson;
    private bool forward;
    private double nextScan;
    private Vector2 scroll;
    [SerializeField] private string questAddress = "";
    private UdpMidiSender relay;
    private string relayStatus = "Wi-Fi relay stopped";
    [MenuItem("XR MIDI/MIDI/Open Mac MIDI Monitor")]
    public static void Open() => GetWindow<MacMidiMonitor>("Mac MIDI Monitor");
    private void OnEnable()
    {
        try { input = new MacMidiInput(() => EditorApplication.timeSinceStartup); input.NoteReceived += OnNote; Refresh(); }
        catch (Exception e) { status = e.Message; }
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }
    private void OnDisable()
    {
        StopRelay();
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        input?.Dispose(); input = null;
    }
    private void OnPlayMode(PlayModeStateChange state)
    {
        StopRelay();
        // Never forward queued Edit Mode or pre-pause notes into a new lesson.
        input?.Poll(true); forward = false; lesson = null; forwardedCount = 0;
    }
    private void Refresh()
    {
        if (input == null) return;
        sources = input.Sources();
        selected = Mathf.Clamp(selected, 0, Math.Max(0, sources.Count - 1));
        if (input.ConnectedSource != 0 && !sources.Any(s => s.Key == input.ConnectedSource))
        { StopRelay(); input.Disconnect(); status = "Device disconnected. Reconnect USB, then select Connect."; }
        nextScan = EditorApplication.timeSinceStartup + 1;
    }
    private void Tick()
    {
        if (input == null) return;
        if (EditorApplication.timeSinceStartup >= nextScan) Refresh();
        if (relay != null)
        {
            try { relay.Tick(); }
            catch (Exception e) { StopRelay(); relayStatus = "Relay error: " + e.Message; }
        }
        input.Poll(EditorApplication.isPaused);
        Repaint();
    }
    private void OnNote(NoteEvent note)
    {
        if (note.IsNoteOn) onCount++; else offCount++;
        notes.Enqueue($"{note.Timestamp:F3}s  {(note.IsNoteOn ? "ON " : "OFF")}  pitch {note.Pitch}  velocity {note.Velocity}  channel {note.Channel + 1}");
        while (notes.Count > 20) notes.Dequeue();
        if (relay != null)
        {
            try { relay.SendNote(note); }
            catch (Exception e) { StopRelay(); relayStatus = "Relay error: " + e.Message; }
        }
        if (forward && EditorApplication.isPlaying && lesson != null &&
            lesson.ReceivePhysicalMidi(note.Pitch, note.IsNoteOn, note.Velocity, note.Channel)) forwardedCount++;
    }
    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Connect your MPK mini and select its source. For Quest, start the Wi-Fi relay below; Mac Play Mode is not required. No sound synthesis.", MessageType.Info);
        if (GUILayout.Button("Refresh devices")) Refresh();
        if (sources.Count > 0)
        {
            selected = EditorGUILayout.Popup("MIDI source", selected, sources.Select(s => s.Value).ToArray());
            if (GUILayout.Button("Connect selected source"))
            {
                try { StopRelay(); input.Connect(sources[selected].Key); status = "Connected: " + sources[selected].Value; }
                catch (Exception e) { status = e.Message; }
            }
        }
        else EditorGUILayout.LabelField("No MIDI sources found. Check the USB data cable.");
        EditorGUILayout.LabelField(status);
        if (input != null && input.ConnectedSource != 0 && GUILayout.Button("Disconnect"))
        { StopRelay(); input.Disconnect(); status = "Disconnected"; }
        EditorGUILayout.LabelField($"Note-on: {onCount}    Note-off: {offCount}    Queue overflows: {input?.OverflowCount ?? 0}");
        questAddress = EditorGUILayout.TextField("Quest Wi-Fi IPv4", questAddress);
        if (relay == null)
        {
            using (new EditorGUI.DisabledScope(input == null || input.ConnectedSource == 0))
                if (GUILayout.Button("Start Wi-Fi MIDI relay"))
                {
                    try
                    {
                        input.Poll(true); relay = new UdpMidiSender(questAddress, UdpMidiReceiver.DefaultPort, () => EditorApplication.timeSinceStartup);
                        relay.Tick(); relayStatus = "Sending to " + questAddress;
                    }
                    catch (Exception e) { StopRelay(); relayStatus = "Relay error: " + e.Message; }
                }
        }
        else
        {
            EditorGUILayout.LabelField(relay.Acknowledged ? "Quest acknowledged the relay" : "Waiting for Quest acknowledgement");
            EditorGUILayout.LabelField($"Sent notes: {relay.SentNotes} | RTT median/p95: {relay.MedianRoundTripMs:0.0}/{relay.P95RoundTripMs:0.0} ms");
            if (GUILayout.Button("Stop Wi-Fi relay")) StopRelay();
        }
        EditorGUILayout.HelpBox(relayStatus + "\nRTT is an application round trip, not key-to-screen latency. Leave this window open. Play Mode transitions stop the relay.", MessageType.None);
        lesson = (RhythmDemoController)EditorGUILayout.ObjectField("Lesson in Play Mode", lesson, typeof(RhythmDemoController), true);
        forward = EditorGUILayout.Toggle("Send notes to lesson", forward);
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Connect to active lesson"))
            {
                var candidates = UnityEngine.Object.FindObjectsOfType<RhythmDemoController>();
                if (candidates.Length == 1) { lesson = candidates[0]; forward = true; }
                else status = "Expected one active lesson. Open Milestone1Demo, or assign the target explicitly.";
            }
        }
        string routing = !EditorApplication.isPlaying ? "Monitor only — enter Play Mode for the lesson."
            : EditorApplication.isPaused ? "Unity is paused — press the toolbar Pause button to resume."
            : !forward ? "Monitor only — click Connect to active lesson."
            : lesson == null ? "No lesson assigned — open Milestone1Demo and connect."
            : input == null || input.ConnectedSource == 0 ? "No MIDI source connected — connect MPK mini above."
            : "Lesson: " + lesson.MidiLessonStatus + (lesson.CanReceivePhysicalMidi ? " — receiving MIDI." : " — not receiving notes; Start or Resume.");
        EditorGUILayout.HelpBox("Desktop lesson routing (separate from Quest relay):\n" + routing, MessageType.Info);
        EditorGUILayout.LabelField("Events forwarded to lesson: " + forwardedCount);
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || EditorApplication.isPaused || lesson == null))
        {
            if (GUILayout.Button("Start / Resume lesson"))
            {
                input?.Poll(true);
                if (lesson.MidiLessonStatus == "Paused") lesson.TogglePause(); else lesson.StartLesson();
            }
            if (GUILayout.Button("Restart lesson")) { input?.Poll(true); lesson.RestartLesson(); }
        }
        EditorGUILayout.HelpBox("Use pitches 60, 62, 64, 65, 67. Forwarding resets when entering/exiting Play Mode. The lesson pauses on focus loss. Notes are evaluated only while Playing; no piano sound or live key highlighting is implemented. Timing includes Editor polling delay.", MessageType.None);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string note in notes) EditorGUILayout.LabelField(note);
        EditorGUILayout.EndScrollView();
    }
    private void StopRelay()
    {
        try { relay?.Dispose(); }
        catch (Exception e) { Debug.LogWarning("Closing MIDI relay: " + e.Message); }
        finally { relay = null; relayStatus = "Wi-Fi relay stopped"; }
    }
}
#endif
