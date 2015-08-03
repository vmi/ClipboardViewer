using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipboardViewer
{
    public class ClipboardHelper
    {
        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CHANGECBCHAIN = 0x030D;

        private Action callback;
        private HwndSource hWndSource = null;
        private IntPtr nextHandle;

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public void RegisterHandler(Window window, Action callback)
        {
            if (hWndSource != null)
                throw new InvalidOperationException("Handler is already registered.");
            this.callback = callback;
            WindowInteropHelper wih = new WindowInteropHelper(window);
            this.hWndSource = HwndSource.FromHwnd(wih.Handle);
            this.hWndSource.AddHook(WndProc);
            this.nextHandle = SetClipboardViewer(hWndSource.Handle);
        }

        public void DeregisterHandler()
        {
            if (hWndSource == null)
                return;
            ChangeClipboardChain(hWndSource.Handle, nextHandle);
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
                    catch (Exception e)
                    {
                        DeregisterHandler();
                        throw e;
                    }
                    SendMessage(nextHandle, msg, wParam, lParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (wParam == nextHandle)
                        nextHandle = lParam;
                    else if (nextHandle != IntPtr.Zero)
                        SendMessage(nextHandle, msg, wParam, lParam);
                    break;
            }
            return IntPtr.Zero;
        }
    }
}
