using System;

namespace Crookedile.Utilities
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DebuggableAttribute : Attribute
    {
        public string Category { get; private set; }
        public LogLevel DefaultLogLevel { get; private set; }

        public DebuggableAttribute(string category = "Default", LogLevel defaultLogLevel = LogLevel.Info)
        {
            Category = category;
            DefaultLogLevel = defaultLogLevel;
        }
    }
}
