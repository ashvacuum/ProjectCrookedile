using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using UnityEngine;
using Crookedile.Core;

namespace Crookedile.Managers
{
    /// <summary>
    /// Centralised feedback service. Holds shared MMF_Players that can be aimed at
    /// any target at runtime, so individual prefabs don't need their own players.
    ///
    /// Two entry points:
    ///   1. PlayMove(from, to)  – animates a transform to a destination via the
    ///      scene's configured _movePlayer (must contain an MMF_Position feedback).
    ///   2. Play(id, target)    – plays a named MMF_Player, patching its internal
    ///      MMF_Position feedbacks to target the supplied transform first.
    ///
    /// Setup: add a FeedbackManager GameObject to the scene (or let the Singleton
    /// create one), then assign _movePlayer and populate _entries in the inspector.
    /// Child MMF_Player GameObjects travel with the manager across scenes.
    /// </summary>
    public class FeedbackManager : Singleton<FeedbackManager>
    {
        // ─── Inspector Types ──────────────────────────────────────────────────────

        [Serializable]
        public struct FeedbackEntry
        {
            [Tooltip("Unique string key used to look up this player at runtime.")]
            public string Id;
            public MMF_Player Player;
        }

        // ─── Inspector Fields ─────────────────────────────────────────────────────

        [Header("Move Feedback")]
        [Tooltip("MMF_Player that contains an MMF_Position feedback. Used by PlayMove().\n\n" +
                 "Only one move can be patched at a time — if you need truly simultaneous moves " +
                 "on different objects, add extra entries to _entries and call Play() with their ids.")]
        [SerializeField] private MMF_Player _movePlayer;

        [Header("Named Feedbacks")]
        [Tooltip("Shared, reusable feedback players. Each id must be unique.\n" +
                 "Call Play(id) to play without a target, or Play(id, transform) to aim the " +
                 "player's MMF_Position feedbacks at a specific object before playing.")]
        [SerializeField] private FeedbackEntry[] _entries;

        [SerializeField] private Transform _cardHandParent;

        public Transform CardHandParent => _cardHandParent;

        // ─── Runtime State ────────────────────────────────────────────────────────

        private Dictionary<string, MMF_Player> _map;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            _map = new Dictionary<string, MMF_Player>(StringComparer.Ordinal);

            if (_entries == null) return;

            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Id) || entry.Player == null) continue;

                if (_map.ContainsKey(entry.Id))
                    Debug.LogWarning($"[FeedbackManager] Duplicate feedback id '{entry.Id}' — skipping.", this);
                else
                    _map[entry.Id] = entry.Player;
            }
        }

        // ─── Move API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Moves <paramref name="target"/> to <paramref name="destination"/> using the scene's
        /// _movePlayer. Works with both Transform and RectTransform.
        /// <paramref name="onComplete"/> is invoked once the move duration has elapsed.
        /// </summary>
        public void PlayMove(Transform target, Transform destination, Action onComplete = null)
        {
            if (_movePlayer == null)
            {
                Debug.LogWarning("[FeedbackManager] _movePlayer is not assigned.", this);
                onComplete?.Invoke();
                return;
            }

            var pos = _movePlayer.FeedbacksList.OfType<MMF_Position>().FirstOrDefault();
            if (pos == null)
            {
                Debug.LogWarning("[FeedbackManager] _movePlayer has no MMF_Position feedback.", this);
                onComplete?.Invoke();
                return;
            }

            pos.AnimatePositionTarget        = target.gameObject;
            pos.InitialPositionTransform     = target;
            pos.DestinationPositionTransform = destination;
            pos.DeterminePositionsOnPlay     = true;
            pos.Mode                         = MMF_Position.Modes.AtoB;
            pos.Space                        = target is RectTransform
                ? MMF_Position.Spaces.RectTransform
                : MMF_Position.Spaces.World;

            _movePlayer.PlayFeedbacks();

            if (onComplete != null)
                StartCoroutine(CallAfter(_movePlayer.TotalDuration, onComplete));
        }

        // ─── Named Feedback API ───────────────────────────────────────────────────

        /// <summary>Plays a named feedback player with no target override.</summary>
        public void Play(string id)
        {
            if (_map.TryGetValue(id, out var player))
                player.PlayFeedbacks();
            else
                Debug.LogWarning($"[FeedbackManager] No feedback registered with id '{id}'.", this);
        }

        /// <summary>
        /// Plays a named feedback player after patching all MMF_Position feedbacks
        /// inside it to aim at <paramref name="target"/>.
        /// RectTransform is detected automatically and the correct space is set.
        /// </summary>
        public void Play(string id, Transform target)
        {
            if (!_map.TryGetValue(id, out var player))
            {
                Debug.LogWarning($"[FeedbackManager] No feedback registered with id '{id}'.", this);
                return;
            }

            PatchPositionTarget(player, target);
            player.PlayFeedbacks();
        }

        /// <inheritdoc cref="Play(string,Transform)"/>
        public void Play(string id, GameObject target) => Play(id, target.transform);

        /// <summary>Stops a named feedback player mid-play.</summary>
        public void Stop(string id)
        {
            if (_map.TryGetValue(id, out var player))
                player.StopFeedbacks();
        }

        /// <summary>Returns the raw MMF_Player for a given id, or null.</summary>
        public bool TryGet(string id, out MMF_Player player) => _map.TryGetValue(id, out player);

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static void PatchPositionTarget(MMF_Player player, Transform target)
        {
            foreach (var feedback in player.FeedbacksList)
            {
                if (feedback is not MMF_Position pos) continue;

                pos.AnimatePositionTarget    = target.gameObject;
                pos.DeterminePositionsOnPlay = true;
                pos.Space = target is RectTransform
                    ? MMF_Position.Spaces.RectTransform
                    : MMF_Position.Spaces.World;
            }
        }

        private IEnumerator CallAfter(float delay, Action action)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
