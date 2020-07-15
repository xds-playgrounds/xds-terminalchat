using System.Reflection;

namespace XDS.Messaging.TerminalChat
{
    public static class AssemblyVersionUtil
    {
        public static string GetProgramDisplayName(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            var version = GetShortVersionString(assembly);
            // ReSharper disable once UnreachableCode
            var compilation = IsDebug ? " (Debug)" : "";
            return $"{name} {version}{compilation}";
        }

        /// <summary>
        /// // Pattern is: 1.0.*. The wildcard is: DateTime.Today.Subtract(new DateTime(2000, 1, 1)).Days;
        /// </summary>
        public static string GetShortVersionString(Assembly assembly)
        {
            var version = assembly.GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

#if DEBUG
        public const bool IsDebug = true;
#else
		public const bool IsDebug = false;
#endif
    }
}
