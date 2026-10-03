// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows.Threading;

namespace RomsCollect.Services.Input;

/// <summary>
/// Polls the first connected Xbox-compatible gamepad via XInput and raises
/// one event per button transition (not per poll tick), so a held direction
/// does not fire the event on every frame. Used for wheel/carousel
/// navigation: D-Pad or left stick left/right to move, A ("Accept") to
/// launch, B ("Back") to return. Mapping is fixed (not configurable) for
/// this first iteration — see ROADMAP.md 11.6.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GamepadService : IDisposable
{
    private const short StickDeadZone = 16000;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(60);

    private readonly DispatcherTimer _timer;
    private bool _wasLeft;
    private bool _wasRight;
    private bool _wasAccept;
    private bool _wasBack;

    public event EventHandler? LeftPressed;
    public event EventHandler? RightPressed;
    public event EventHandler? AcceptPressed;
    public event EventHandler? BackPressed;

    public GamepadService()
    {
        _timer = new DispatcherTimer { Interval = PollInterval };
        _timer.Tick += (_, _) => Poll();
        _timer.Start();
    }

    private void Poll()
    {
        if (XInputInterop.XInputGetState(0, out var state) != 0)
        {
            // No controller connected at index 0 — nothing to report.
            _wasLeft = _wasRight = _wasAccept = _wasBack = false;
            return;
        }

        var buttons = (XInputInterop.Buttons)state.Gamepad.wButtons;
        var isLeft = buttons.HasFlag(XInputInterop.Buttons.DPadLeft) || state.Gamepad.sThumbLX < -StickDeadZone;
        var isRight = buttons.HasFlag(XInputInterop.Buttons.DPadRight) || state.Gamepad.sThumbLX > StickDeadZone;
        var isAccept = buttons.HasFlag(XInputInterop.Buttons.A);
        var isBack = buttons.HasFlag(XInputInterop.Buttons.B);

        RaiseOnRisingEdge(isLeft, ref _wasLeft, LeftPressed);
        RaiseOnRisingEdge(isRight, ref _wasRight, RightPressed);
        RaiseOnRisingEdge(isAccept, ref _wasAccept, AcceptPressed);
        RaiseOnRisingEdge(isBack, ref _wasBack, BackPressed);
    }

    private void RaiseOnRisingEdge(bool isPressed, ref bool wasPressed, EventHandler? handler)
    {
        if (isPressed && !wasPressed)
        {
            handler?.Invoke(this, EventArgs.Empty);
        }

        wasPressed = isPressed;
    }

    public void Dispose() => _timer.Stop();
}
