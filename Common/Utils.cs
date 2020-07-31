using System;
using System.Runtime.CompilerServices;

namespace Common
{
    public static class Utils
    {
        public static void GuardNotNull<T>(T obj, [CallerMemberName] string name = "unknown")
        {
            if (obj == null)
                throw new ArgumentNullException($"Method {name} was passed a null value (parameter type {typeof(T)}) ");
        }
    }
}
