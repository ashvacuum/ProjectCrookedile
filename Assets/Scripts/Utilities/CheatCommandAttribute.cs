using System;

namespace Crookedile.Utilities
{
    /// <summary>
    /// Attribute to mark a method as a cheat command.
    /// The method will be automatically registered in the console with parameter parsing.
    /// </summary>
    /// <example>
    /// [CheatCommand("add_money", "Give the player money")]
    /// public void AddMoney(int amount) { }
    ///
    /// [CheatCommand("spawn", "Spawn an entity")]
    /// public void Spawn(string entityName, int count = 1) { }
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CheatCommandAttribute : Attribute
    {
        /// <summary>
        /// The command keyword to type in console (e.g., "add_money")
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// Description of what the command does
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Optional category for organizing commands (e.g., "Resources", "Cards", "Battle")
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Creates a cheat command attribute.
        /// </summary>
        /// <param name="command">Command keyword (e.g., "add_money")</param>
        /// <param name="description">What the command does</param>
        public CheatCommandAttribute(string command, string description = "")
        {
            Command = command;
            Description = description;
        }
    }
}
