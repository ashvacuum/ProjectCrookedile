using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Utilities
{
    [Serializable]
    public class CategoryDebugInfo
    {
        public string categoryName;
        public bool enabled = true;
        public LogLevel logLevel = LogLevel.Info;
        public List<string> scripts = new List<string>();
    }

    [CreateAssetMenu(fileName = "DebugSettings", menuName = "Crookedile/Debug Settings")]
    public class DebugSettings : ScriptableObject
    {
        [Header("Global Settings")]
        public bool globalLoggingEnabled = true;
        public LogLevel globalLogLevel = LogLevel.Info;
        public bool showTimestamp = true;
        public bool showStackTrace = false;

        [Header("Category Settings")]
        public List<CategoryDebugInfo> categories = new List<CategoryDebugInfo>();

        public void ApplySettings()
        {
            if (DebugLogger.Instance == null) return;

            DebugLogger.Instance.SetGlobalLoggingEnabled(globalLoggingEnabled);
            DebugLogger.Instance.SetGlobalLogLevel(globalLogLevel);

            foreach (var category in categories)
            {
                DebugLogger.Instance.RegisterCategory(category.categoryName, category.enabled, category.logLevel);
            }
        }

        public CategoryDebugInfo GetOrCreateCategory(string categoryName)
        {
            CategoryDebugInfo category = categories.Find(c => c.categoryName == categoryName);
            if (category == null)
            {
                category = new CategoryDebugInfo { categoryName = categoryName };
                categories.Add(category);
            }
            return category;
        }

        public void RefreshCategories()
        {
            // Clear script lists
            foreach (var category in categories)
            {
                category.scripts.Clear();
            }

            // Find all types with DebuggableAttribute
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var attribute = (DebuggableAttribute)Attribute.GetCustomAttribute(type, typeof(DebuggableAttribute));
                        if (attribute != null)
                        {
                            CategoryDebugInfo category = GetOrCreateCategory(attribute.Category);
                            if (!category.scripts.Contains(type.Name))
                            {
                                category.scripts.Add(type.Name);
                            }
                            category.logLevel = attribute.DefaultLogLevel;
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be reflected
                }
            }

            // Remove empty categories
            categories.RemoveAll(c => c.scripts.Count == 0);
        }
    }
}
