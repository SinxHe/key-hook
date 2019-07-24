using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;

class InterceptKeys
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
	private const int WM_KEYUP = 0x0101;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;
	private static Dictionary<Keys, DateTimeOffset> _keyHistory = new Dictionary<Keys, DateTimeOffset>();

    public static void Main()
    {
        _hookID = SetHook(_proc);
        Application.Run();
        UnhookWindowsHookEx(_hookID);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
			var key = (Keys)vkCode;
			if (key == Keys.A)
			{
				return (IntPtr)1;
			}
			if (wParam == (IntPtr)WM_KEYDOWN)
			{
				_keyHistory.TryGetValue(key, out var dt);
				var ts = DateTimeOffset.Now - dt;
				if (ts < TimeSpan.FromMilliseconds(60))
				{
					Console.WriteLine((Keys)vkCode + $" - {ts} - ignore");
					return (IntPtr)1;
				}
				_keyHistory[key] = DateTimeOffset.Now;
				Console.WriteLine((Keys)vkCode + " - down");
			}
			if (wParam == (IntPtr)WM_KEYUP)
			{
				if (_keyHistory.ContainsKey(key))
				{
					_keyHistory.Remove(key);
					Console.WriteLine((Keys)vkCode + " - removed");
				}
				Console.WriteLine((Keys)vkCode + " - up");
			}
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}