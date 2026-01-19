using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static partial class PyKeyboard
{
    private const  int  LogCapacity     = 100;
    private static bool _activeKeysOnly = true;
    private static bool _logDownEvent;
    private static bool _logPressEvent   = true;
    private static bool _logReleaseEvent = true;

    private static readonly string[] FocusLostNames = Enum.GetNames<FocusLostInputBehaviour>();

    private static readonly List<string> EventLog = new(LogCapacity);

    private static void InitializeImGui()
    {
        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        OnKeyDown += (_, args) =>
        {
            if (!_logDownEvent)
            {
                return;
            }

            ImGuiLog("OnKeyDown");
            ImGuiLog($"     -Key: {args.Key}");
            ImGuiLog("separator");
        };

        OnKeyPressed += (_, args) =>
        {
            if (!_logPressEvent)
            {
                return;
            }

            ImGuiLog("OnKeyPressed");
            ImGuiLog($"     -Key: {args.Key}");
            ImGuiLog("separator");
        };

        OnKeyReleased += (_, args) =>
        {
            if (!_logReleaseEvent)
            {
                return;
            }

            ImGuiLog("OnKeyReleased");
            ImGuiLog($"     -Key: {args.Key}");
            ImGuiLog("separator");
        };
    }

    private static void ImGuiLog(string message)
    {
        EventLog.Add(message);

        if (EventLog.Count >= LogCapacity)
        {
            EventLog.RemoveAt(0);
        }
    }

    private static void ImGuiOnLayout(object? sender, EventArgs args)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.Keyboard} Keyboard ");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        if (ImGui.CollapsingHeader("Config"))
        {
            int focusLost = (int)_focusLostInputBehaviour;

            bool focusLostChanged =
                ImGui.Combo("FocusLost Behaviour", ref focusLost, FocusLostNames, FocusLostNames.Length);

            if (focusLostChanged)
            {
                _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
            }
        }

        if (ImGui.CollapsingHeader("Time stamps"))
        {
            ImGui.Text($"Last Input: {LastInputTime}");
        }

        if (ImGui.CollapsingHeader("Keys"))
        {
            ImGui.TreePush("Keys");

            if (ImGui.CollapsingHeader("All keys down"))
            {
                string keys = string.Join(", ", GetAllKeysDown());
                ImGui.Text(keys);
                ImGui.Separator();
            }

            if (ImGui.CollapsingHeader("Key states"))
            {
                ImGui.Checkbox("Only active keys", ref _activeKeysOnly);

                const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;

                if (ImGui.BeginTable("Keys", 4, flags))
                {
                    ImGui.TableSetupColumn("Key");
                    ImGui.TableSetupColumn("State");
                    ImGui.TableSetupColumn("Pressed");
                    ImGui.TableSetupColumn("Released");
                    ImGui.TableHeadersRow();

                    foreach (Keys key in AllKeys)
                    {
                        InputStates state    = GetKeyState(key);
                        bool        pressed  = WasKeyPressed(key);
                        bool        released = WasKeyReleased(key);

                        if (_activeKeysOnly && state == InputStates.Up)
                        {
                            continue;
                        }

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        if (!IsKeyDown(key))
                        {
                            ImGui.BeginDisabled();
                        }

                        ImGui.Text(key.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{(IsKeyDown(key) ? "Down" : "Up")}");

                        if (!IsKeyDown(key))
                        {
                            ImGui.EndDisabled();
                        }

                        ImGui.TableNextColumn();
                        ImGui.Checkbox($"##{key}pressed", ref pressed);
                        ImGui.TableNextColumn();
                        ImGui.Checkbox($"##{key}released", ref released);
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.CollapsingHeader("Events log"))
        {
            ImGui.TreePush("Events");

            if (ImGui.CollapsingHeader("Events"))
            {
                ImGui.Checkbox("DownEvent", ref _logDownEvent);
                ImGui.Checkbox("PressEvent", ref _logPressEvent);
                ImGui.Checkbox("ReleaseEvent", ref _logReleaseEvent);
            }

            ImGui.TreePop();

            bool clearLogs = ImGui.Button("Clear");

            if (clearLogs)
            {
                EventLog.Clear();
            }

            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar;

            if (ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle, windowFlags))
            {
                foreach (string message in EventLog)
                {
                    if (message == "separator")
                    {
                        ImGui.Separator();
                    }
                    else
                    {
                        ImGui.Text(message);
                    }
                }

                ImGui.SetScrollHereY();
            }
            
            ImGui.EndChild();
        }

        ImGui.End();
    }
}