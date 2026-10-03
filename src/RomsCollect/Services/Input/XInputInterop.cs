// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RomsCollect.Services.Input;

/// <summary>
/// Raw P/Invoke bindings to the XInput API (<c>xinput1_4.dll</c>), bundled
/// with Windows 10/11 — no third-party gamepad library or NuGet dependency
/// is pulled in. Covers Xbox-compatible controllers only, per the
/// "generic DirectInput/Xbox" scope of this feature.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class XInputInterop
{
    private const string DllName = "xinput1_4.dll";

    [Flags]
    public enum Buttons : ushort
    {
        DPadLeft = 0x0004,
        DPadRight = 0x0008,
        A = 0x1000,
        B = 0x2000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Gamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct State
    {
        public uint dwPacketNumber;
        public Gamepad Gamepad;
    }

    [DllImport(DllName)]
    public static extern int XInputGetState(int dwUserIndex, out State pState);
}
