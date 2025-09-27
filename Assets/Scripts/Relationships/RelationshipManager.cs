using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Relationships
{
    public class RelationshipManager : MonoBehaviour
    {
        [Header("Character Database")]
        public List<Character> _allCharacters = new List<Character>();
        public List<Character> _unlockedCharacters = new List<Character>();

        [Header("Relationship Tracking")] private readonly Dictionary<string, Character> characterDatabase = new Dictionary<string, Character>();

        private void Start()
        {
            InitializeCharacters();
        }

        private void InitializeCharacters()
        {
            characterDatabase.Clear();

            foreach (Character character in _allCharacters)
            {
                characterDatabase[character.characterName] = character;

                if (!character.requiresUnlock)
                {
                    _unlockedCharacters.Add(character);
                }
            }

            Debug.Log($"Initialized {characterDatabase.Count} characters, {_unlockedCharacters.Count} unlocked");
        }

        public void ProcessRelationships()
        {
            foreach (Character character in _unlockedCharacters)
            {
                if (character.IsAllied())
                {
                    character.allyBonus.Apply();
                }
                else if (character.isBetrayed)
                {
                    character.betrayalPenalty.Apply();
                }
            }

            CheckForNewUnlocks();
        }

        public void ChangeRapport(string characterName, int amount)
        {
            if (characterDatabase.TryGetValue(characterName, out Character character))
            {
                character.ChangeRapport(amount);
            }
        }

        public int GetRapport(string characterName)
        {
            if (characterDatabase.TryGetValue(characterName, out Character character))
            {
                return character.currentRapport;
            }
            return 0;
        }

        public void BetrayCharacter(string characterName)
        {
            if (characterDatabase.TryGetValue(characterName, out Character character))
            {
                character.Betray();
            }
        }

        public void InteractWithCharacter(string characterName)
        {
            if (characterDatabase.TryGetValue(characterName, out Character character))
            {
                if (_unlockedCharacters.Contains(character))
                {
                    character.Interact();
                }
            }
        }

        public List<Character> GetAlliedCharacters()
        {
            return _unlockedCharacters.Where(c => c.IsAllied()).ToList();
        }

        public List<Character> GetHostileCharacters()
        {
            return _unlockedCharacters.Where(c => c.IsHostile()).ToList();
        }

        public List<Character> GetBetrayedCharacters()
        {
            return _unlockedCharacters.Where(c => c.isBetrayed).ToList();
        }

        public List<Character> GetCharactersBySector(CharacterSector sector)
        {
            return _unlockedCharacters.Where(c => c.sector == sector).ToList();
        }

        public int GetElectionBonus()
        {
            int bonus = 0;

            foreach (Character character in GetAlliedCharacters())
            {
                bonus += 5;
            }

            foreach (Character character in GetBetrayedCharacters())
            {
                bonus -= 10;
            }

            return bonus;
        }

        public bool CanUnlockCharacter(Character character)
        {
            if (!character.requiresUnlock) return true;
            if (_unlockedCharacters.Contains(character)) return true;

            return character.unlockCondition.IsMet();
        }

        private void CheckForNewUnlocks()
        {
            foreach (Character character in _allCharacters)
            {
                if (!_unlockedCharacters.Contains(character) && CanUnlockCharacter(character))
                {
                    UnlockCharacter(character);
                }
            }
        }

        private void UnlockCharacter(Character character)
        {
            _unlockedCharacters.Add(character);
            Debug.Log($"Unlocked character: {character.characterName}");
        }

        public Character GetCharacter(string characterName)
        {
            characterDatabase.TryGetValue(characterName, out Character character);
            return character;
        }

        public string GetRelationshipSummary()
        {
            int allied = GetAlliedCharacters().Count;
            int hostile = GetHostileCharacters().Count;
            int betrayed = GetBetrayedCharacters().Count;
            int neutral = _unlockedCharacters.Count - allied - hostile - betrayed;

            return $"Allied: {allied}, Hostile: {hostile}, Neutral: {neutral}, Betrayed: {betrayed}";
        }

        public float GetProjectCostMultiplier()
        {
            float multiplier = 1f;

            foreach (Character character in GetAlliedCharacters())
            {
                multiplier *= character.allyBonus.projectCostMultiplier;
            }

            foreach (Character character in GetBetrayedCharacters())
            {
                multiplier *= character.betrayalPenalty.projectCostMultiplier;
            }

            return multiplier;
        }

        public int GetProjectTimeModifier()
        {
            int modifier = 0;

            foreach (Character character in GetAlliedCharacters())
            {
                modifier += character.allyBonus.projectTimeReduction;
            }

            foreach (Character character in GetBetrayedCharacters())
            {
                modifier += character.betrayalPenalty.projectTimeIncrease;
            }

            return modifier;
        }

        public int GetProjectApprovalModifier()
        {
            int modifier = 0;

            foreach (Character character in GetAlliedCharacters())
            {
                modifier += character.allyBonus.projectApprovalBonus;
            }

            foreach (Character character in GetBetrayedCharacters())
            {
                modifier -= character.betrayalPenalty.projectApprovalPenalty;
            }

            return modifier;
        }
    }
}