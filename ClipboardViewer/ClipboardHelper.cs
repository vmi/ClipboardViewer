using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipboardViewer
{
    internal class NativeMethods
    {
        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
         internal static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        internal extern static IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    }

    public class ClipboardHelper
    {
        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CHANGECBCHAIN = 0x030D;

        private Action callback;
        private HwndSource hWndSource = null;
        private IntPtr nextHandle;

        public void RegisterHandler(Window window, Action callback)
        {
            if (hWndSource != null)
                throw new InvalidOperationException("Handler is already registered.");
            this.callback = callback;
            WindowInteropHelper wih = new WindowInteropHelper(window);
            this.hWndSource = HwndSource.FromHwnd(wih.Handle);
            this.hWndSource.AddHook(WndProc);
            this.nextHandle = NativeMethods.SetClipboardViewer(hWndSource.Handle);
        }

        public void DeregisterHandler()
        {
            if (hWndSource == null)
                return;
            NativeMethods.ChangeClipboardChain(hWndSource.Handle, nextHandle);
            hWndSource.RemoveHook(WndProc);
            hWndSource = null;
            nextHandle = IntPtr.Zero;
            callback = null;
        }

        public bool IsRegistered()
        {
            return hWndSource != null;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception)
                    {
                        DeregisterHandler();
                        throw;
                    }
                    NativeMethods.SendMessage(nextHandle, msg, wParam, lParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (wParam == nextHandle)
                        nextHandle = lParam;
                    else if (nextHandle != IntPtr.Zero)
                        NativeMethods.SendMessage(nextHandle, msg, wParam, lParam);
                    break;
            }
            return IntPtr.Zero;
        }
    }
}
