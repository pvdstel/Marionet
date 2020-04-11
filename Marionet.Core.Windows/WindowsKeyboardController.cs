using Marionet.Core.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    internal class WindowsKeyboardController : IKeyboardController, IDisposable
    {
        private readonly HashSet<int> pressedKeyCodes = new HashSet<int>();
        private readonly InputSender inputSender;

        public WindowsKeyboardController(InputSender inputSender)
        {
            this.inputSender = inputSender;
        }

        public Task PressKeyboardButton(int keyCode)
        {
            if (keyCode > short.MaxValue)
            {
                return Task.CompletedTask;
            }

            var keyDown = new Native.INPUT()
            {
                Type = Native.Constants.INPUT_KEYBOARD,
            };
            keyDown.Data.Keyboard.wVk = (short)keyCode;
            if (IsExtendedKey((Native.VirtualKey)keyCode))
            {
                keyDown.Data.Keyboard.dwFlags |= Native.KEYEVENTF.EXTENDEDKEY;
            }
            keyDown.Data.Keyboard.dwExtraInfo = InputUtils.InstancePointer;
            pressedKeyCodes.Add(keyCode);
            inputSender.AddInput(keyDown);

            return Task.CompletedTask;
        }

        public Task ReleaseKeyboardButton(int keyCode)
        {
            if (keyCode > short.MaxValue)
            {
                return Task.CompletedTask;
            }

            var keyUp = new Native.INPUT()
            {
                Type = Native.Constants.INPUT_KEYBOARD,
            };
            keyUp.Data.Keyboard.wVk = (short)keyCode;
            keyUp.Data.Keyboard.dwFlags = Native.KEYEVENTF.KEYUP;
            if (IsExtendedKey((Native.VirtualKey)keyCode))
            {
                keyUp.Data.Keyboard.dwFlags |= Native.KEYEVENTF.EXTENDEDKEY;
            }
            keyUp.Data.Keyboard.dwExtraInfo = InputUtils.InstancePointer;
            inputSender.AddInput(keyUp);

            return Task.CompletedTask;
        }

        private static bool IsExtendedKey(Native.VirtualKey keyCode)
        {
            return
                keyCode == Native.VirtualKey.Menu ||
                keyCode == Native.VirtualKey.LeftMenu ||
                keyCode == Native.VirtualKey.RightMenu ||
                keyCode == Native.VirtualKey.Control ||
                keyCode == Native.VirtualKey.RightControl ||
                keyCode == Native.VirtualKey.Insert ||
                keyCode == Native.VirtualKey.Delete ||
                keyCode == Native.VirtualKey.Home ||
                keyCode == Native.VirtualKey.End ||
                keyCode == Native.VirtualKey.Prior ||
                keyCode == Native.VirtualKey.Next ||
                keyCode == Native.VirtualKey.Right ||
                keyCode == Native.VirtualKey.Up ||
                keyCode == Native.VirtualKey.Left ||
                keyCode == Native.VirtualKey.Down ||
                keyCode == Native.VirtualKey.NumLock ||
                keyCode == Native.VirtualKey.Cancel ||
                keyCode == Native.VirtualKey.Snapshot ||
                keyCode == Native.VirtualKey.Divide;
        }

        private void ReleaseAllPressed()
        {
            var events = pressedKeyCodes.Select(keyCode =>
            {
                var keyUp = new Native.INPUT()
                {
                    Type = Native.Constants.INPUT_KEYBOARD,
                };
                keyUp.Data.Keyboard.wVk = (short)keyCode;
                keyUp.Data.Keyboard.dwFlags = Native.KEYEVENTF.KEYUP;
                if (IsExtendedKey((Native.VirtualKey)keyCode))
                {
                    keyUp.Data.Keyboard.dwFlags |= Native.KEYEVENTF.EXTENDEDKEY;
                }
                keyUp.Data.Keyboard.dwExtraInfo = InputUtils.InstancePointer;
                return keyUp;
            });
            events.SendInputs();

            pressedKeyCodes.Clear();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                ReleaseAllPressed();

                disposedValue = true;
            }
        }

        ~WindowsKeyboardController()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
