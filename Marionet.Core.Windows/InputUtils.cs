using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Marionet.Core.Windows
{
    internal static class InputUtils
    {
        private const int InstancePointerValue = 186330933;
        public static IntPtr InstancePointer { get; } = new IntPtr(InstancePointerValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMarionetInstancePointer(this IntPtr ptr) => ptr == InstancePointer;

        public static bool SendSingleInput(this Native.INPUT input)
        {
            return Native.Methods.SendInput(1, new Native.INPUT[] { input }, Marshal.SizeOf(typeof(Native.INPUT))) == 1;
        }

        public static bool SendInputs(this IEnumerable<Native.INPUT> inputs)
        {
            var inputsArray = inputs.ToArray();
            return Native.Methods.SendInput((uint)inputsArray.Length, inputsArray, Marshal.SizeOf(typeof(Native.INPUT))) == inputsArray.Length;
        }
    }
}
