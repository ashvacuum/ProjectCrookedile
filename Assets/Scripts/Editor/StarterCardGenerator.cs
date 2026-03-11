using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;

namespace Crookedile.Editor
{
    /// <summary>
    /// Unity Editor tool to auto-generate all 30 starter cards as ScriptableObjects.
    /// Right-click in Project window → Crookedile → Generate All Starter Cards
    /// </summary>
    public static class StarterCardGenerator
    {
        [MenuItem("Assets/Crookedile/Generate All Starter Cards", false, 1)]
        public static void GenerateAllStarterCards()
        {
            string basePath = "Assets/Data/Cards/Starter";

            // Ensure folders exist
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Cards"))
                AssetDatabase.CreateFolder("Assets/Data", "Cards");
            if (!AssetDatabase.IsValidFolder(basePath))
                AssetDatabase.CreateFolder("Assets/Data/Cards", "Starter");

            GenerateFaithLeaderCards(basePath);
            GenerateNepoBabyCards(basePath);
            GenerateActorCards(basePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Generated all 30 starter cards!");
        }

        [MenuItem("Assets/Crookedile/Generate Faith Leader Reward Cards", false, 2)]
        public static void GenerateFaithLeaderRewardCards()
        {
            string rewardPath  = "Assets/Data/Cards/Rewards";
            string flPath      = $"{rewardPath}/FaithLeader";
            string tokenPath   = $"{rewardPath}/Tokens";     // player-generated Pressure cards
            string statusPath  = $"{rewardPath}/Status";     // enemy-generated, playable at a cost
            string cursePath   = $"{rewardPath}/Curses";     // enemy-generated, always unplayable

            // Ensure folders exist
            foreach (var (parent, child, folder) in new[]
            {
                ("Assets",                    "Data",       "Assets/Data"),
                ("Assets/Data",               "Cards",      "Assets/Data/Cards"),
                ("Assets/Data/Cards",         "Rewards",    rewardPath),
                (rewardPath,                  "FaithLeader", flPath),
                (rewardPath,                  "Tokens",     tokenPath),
                (rewardPath,                  "Status",     statusPath),
                (rewardPath,                  "Curses",     cursePath),
            })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder(parent, child);
            }

            GenerateFaithLeaderRewardPool(flPath);
            GenerateFaithLeaderTokenCards(tokenPath);
            GenerateFaithLeaderStatusCards(statusPath);
            GenerateFaithLeaderCurseCards(cursePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Generated Faith Leader reward, token, status, and curse cards!");
        }

        #region Faith Leader Cards

        private static void GenerateFaithLeaderCards(string basePath)
        {
            string path = $"{basePath}/FaithLeader";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(basePath, "FaithLeader");

            // 1. Find Common Ground x4
            for (int i = 1; i <= 4; i++)
            {
                CreateCard(
                    path: $"{path}/FindCommonGround_{i}.asset",
                    name: "Find Common Ground",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Basic persuasion technique.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(3)
                    }
                );
            }

            // 2. Blessing x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/Blessing_{i}.asset",
                    name: "Blessing",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Convert all Composure into a powerful burst of conviction.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEqualToComposureEffect(),
                        CreateConsumeAllComposureEffect()
                    }
                );
            }

            // 3. Accusation x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/Accusation_{i}.asset",
                    name: "Accusation",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "Direct confrontation. Creates tension.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(4),
                        CreateReduceHostilityEffect(1)
                    }
                );
            }

            // 4. Deflect x1
            CreateCard(
                path: $"{path}/Deflect.asset",
                name: "Deflect",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Redirect aggression into grace.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(3),
                    CreateReduceHostilityEffect(1)
                }
            );

            // 5. Gather Thoughts x1
            CreateCard(
                path: $"{path}/GatherThoughts.asset",
                name: "Gather Thoughts",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Center yourself and build inner strength.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(4)
                }
            );
        }

        #endregion

        #region Nepo Baby Cards

        private static void GenerateNepoBabyCards(string basePath)
        {
            string path = $"{basePath}/NepoBaby";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(basePath, "NepoBaby");

            // 1. Family Name x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/FamilyName_{i}.asset",
                    name: "Family Name",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Leverage your family's reputation.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(3)
                    }
                );
            }

            // 2. Inherited Privilege x1
            CreateCard(
                path: $"{path}/InheritedPrivilege.asset",
                name: "Inherited Privilege",
                type: CardType.Pressure,
                rarity: CardRarity.Basic,
                description: "Your advantages open doors.",
                cost: 2,
                effects: new CardEffect[]
                {
                    CreateDamageEffect(5),
                    CreateDrawCardsEffect(1)
                }
            );

            // 3. Pull Strings x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/PullStrings_{i}.asset",
                    name: "Pull Strings",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "Use connections to apply pressure.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(4),
                        CreateReduceHostilityEffect(1)
                    }
                );
            }

            // 4. Call in Favor x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/CallInFavor_{i}.asset",
                    name: "Call in Favor",
                    type: CardType.Policy,
                    rarity: CardRarity.Basic,
                    description: "You know people.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDrawCardsEffect(2)
                    }
                );
            }

            // 5. Backroom Deal x1
            CreateCard(
                path: $"{path}/BackroomDeal.asset",
                name: "Backroom Deal",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Negotiate for future advantage.",
                cost: 2,
                effects: new CardEffect[]
                {
                    CreateDrawCardsEffect(2),
                    CreateGainActionPointsNextTurnEffect(1)
                }
            );

            // 6. Dynasty Network x1
            CreateCard(
                path: $"{path}/DynastyNetwork.asset",
                name: "Dynasty Network",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Cycle through your connections.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateDiscardCardsEffect(1),
                    CreateDrawCardsEffect(2)
                }
            );

            // 7. Trust Fund x1
            CreateCard(
                path: $"{path}/TrustFund.asset",
                name: "Trust Fund",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Money solves problems instantly.",
                cost: 0,
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(2),
                    CreateGainActionPointsEffect(1)
                }
            );
        }

        #endregion

        #region Actor Cards

        private static void GenerateActorCards(string basePath)
        {
            string path = $"{basePath}/Actor";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(basePath, "Actor");

            // 1. Charming Gambit x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/CharmingGambit_{i}.asset",
                    name: "Charming Gambit",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Charisma with a chance of deeper connection.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(3),
                        CreateDrawCardsEffect(1) // TODO: Should be 50% chance
                    }
                );
            }

            // 2. All or Nothing x1
            CreateCard(
                path: $"{path}/AllOrNothing.asset",
                name: "All or Nothing",
                type: CardType.Rhetoric,
                rarity: CardRarity.Basic,
                description: "High risk, high reward aggression.",
                cost: 2,
                effects: new CardEffect[]
                {
                    CreateRandomDamageEffect(3, 9)
                }
            );

            // 3. Bold Accusation x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/BoldAccusation_{i}.asset",
                    name: "Bold Accusation",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "Aggressive confrontation.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(5),
                        CreateReduceHostilityEffect(2)
                    }
                );
            }

            // 4. Spotlight Hog x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/SpotlightHog_{i}.asset",
                    name: "Spotlight Hog",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "All eyes on you - for better or worse.",
                    cost: 2,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(6),
                        CreateGainComposureEffect(3),
                        CreateReduceHostilityEffect(2)
                    }
                );
            }

            // 5. High Stakes x1
            CreateCard(
                path: $"{path}/HighStakes.asset",
                name: "High Stakes",
                type: CardType.Policy,
                rarity: CardRarity.Rare,
                description: "All in.",
                cost: 0,
                effects: new CardEffect[]
                {
                    CreateDiscardCardsEffect(99), // Discard entire hand
                    CreateDrawCardsEffect(3)
                }
            );

            // 6. Ego Trip x1
            CreateCard(
                path: $"{path}/EgoTrip.asset",
                name: "Ego Trip",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Convert your bad reputation into confidence.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateComposureEqualToHostilityEffect()
                }
            );

            // 7. Fan Favorite x1
            CreateCard(
                path: $"{path}/FanFavorite.asset",
                name: "Fan Favorite",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Trade popularity for damage reduction.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateLoseComposureEffect(3),
                    CreateReduceHostilityEffect(3)
                }
            );
        }

        #endregion

        #region Faith Leader Reward Pool

        private static void GenerateFaithLeaderRewardPool(string path)
        {
            // ── BASIC ────────────────────────────────────────────────────────────────

            // Sermon — Gain 3 Composure, Draw 1
            CreateCard(path: $"{path}/Sermon.asset",
                name: "Sermon",                     type: CardType.Pressure,
                rarity: CardRarity.Basic,           cost: 1,
                description: "Share your conviction. Build inner strength and keep the ideas flowing.",
                tags: new[] { "faithleader" },
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(3),
                    CreateDrawCardsEffect(1),
                });

            // Preach — Deal 4 damage, Gain 3 Composure
            CreateCard(path: $"{path}/Preach.asset",
                name: "Preach",                     type: CardType.Pressure,
                rarity: CardRarity.Basic,           cost: 1,
                description: "Press your moral argument. Your conviction grows as you speak.",
                tags: new[] { "faithleader" },
                effects: new CardEffect[]
                {
                    CreateDamageEffect(4),
                    CreateGainComposureEffect(3),
                });

            // Righteous Fury — Deal 4 damage, Lose 3 Composure
            CreateCard(path: $"{path}/RighteousFury.asset",
                name: "Righteous Fury",             type: CardType.Rhetoric,
                rarity: CardRarity.Basic,           cost: 1,
                description: "Channel your outrage into an attack. Composure gives way to righteous anger.",
                tags: new[] { "faithleader" },
                effects: new CardEffect[]
                {
                    CreateDamageEffect(4),
                    CreateLoseComposureEffect(3),
                });

            // Moral High Ground — Gain 3 Composure, Reduce Hostility 1
            // NOTE: 'Retain' mechanic requires the new BattleEffect system — configure in Unity Editor.
            CreateCard(path: $"{path}/MoralHighGround.asset",
                name: "Moral High Ground",          type: CardType.Policy,
                rarity: CardRarity.Basic,           cost: 1,
                description: "Stand firm and de-escalate. Retain.",
                tags: new[] { "faithleader" },
                configNotes: "Add RetainThisCard effect + configure Retain behaviour via Inspector.",
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(3),
                    CreateReduceHostilityEffect(1),
                });

            // False Prophet — Deal 4 damage
            // NOTE: "Enemy gains 2 Hostility" requires RaiseTargetHostilityEffect (new system).
            CreateCard(path: $"{path}/FalseProphet.asset",
                name: "False Prophet",              type: CardType.Rhetoric,
                rarity: CardRarity.Basic,           cost: 1,
                description: "Expose their hypocrisy. They become more agitated.",
                tags: new[] { "faithleader" },
                configNotes: "Add RaiseTargetHostilityEffect (amount 2) via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDamageEffect(4),
                });

            // ── ENHANCED ─────────────────────────────────────────────────────────────

            // Prayer — Gain 5 Composure (+ Add Blessed to hand via Inspector)
            CreateCard(path: $"{path}/Prayer.asset",
                name: "Prayer",                     type: CardType.Pressure,
                rarity: CardRarity.Enhanced,        cost: 1,
                description: "A moment of reflection. Gain strength and receive a divine token.",
                tags: new[] { "faithleader" },
                configNotes: "Add AddCardToHand 'Blessed' effect via Inspector.",
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(5),
                });

            // Congregation — Draw 3, Gain 9 Composure (approximated as 3 per card drawn)
            CreateCard(path: $"{path}/Congregation.asset",
                name: "Congregation",               type: CardType.Policy,
                rarity: CardRarity.Enhanced,        cost: 2,
                description: "Rally your followers. Draw strength from the crowd.",
                tags: new[] { "faithleader" },
                effects: new CardEffect[]
                {
                    CreateDrawCardsEffect(3),
                    CreateGainComposureEffect(9),
                });

            // Holy Patience — Gain 6 Composure (Retain + conditional gain via Inspector)
            CreateCard(path: $"{path}/HolyPatience.asset",
                name: "Holy Patience",              type: CardType.Pressure,
                rarity: CardRarity.Enhanced,        cost: 1,
                description: "Wait for the right moment. Retain. At end of turn, if still in hand: gain 6 Composure.",
                tags: new[] { "faithleader" },
                configNotes: "Add RetainThisCard effect + TurnEndTrigger passive: GainComposure 6 via Inspector.",
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(6),
                });

            // Excommunicate — Deal 5 damage (+ Apply Weakened 2 + Vulnerable 1 via Inspector)
            CreateCard(path: $"{path}/Excommunicate.asset",
                name: "Excommunicate",              type: CardType.Rhetoric,
                rarity: CardRarity.Enhanced,        cost: 2,
                description: "Cast them out. Apply Weakened 2. Apply Vulnerable 1. Deal 5 damage.",
                tags: new[] { "faithleader" },
                configNotes: "Add ApplyStatusEffect Weakened 2 + Vulnerable 1 via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDamageEffect(5),
                });

            // Pastoral Care — Gain 4 Composure (+ Heal 10 Resolve via Inspector)
            CreateCard(path: $"{path}/PastoralCare.asset",
                name: "Pastoral Care",              type: CardType.Policy,
                rarity: CardRarity.Enhanced,        cost: 2,
                description: "Tend to your own wounds and steady your resolve. Heal 10 Resolve. Gain 4 Composure.",
                tags: new[] { "faithleader" },
                configNotes: "Add HealResolveEffect (amount 10) via Inspector.",
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(4),
                });

            // ── RARE ─────────────────────────────────────────────────────────────────

            // Holy Alliance — Gain 6 Composure + Reduce Hostility 3
            // NOTE: "Double current Composure" requires new BattleEffect — configure in Inspector.
            CreateCard(path: $"{path}/HolyAlliance.asset",
                name: "Holy Alliance",              type: CardType.Policy,
                rarity: CardRarity.Rare,            cost: 2,
                description: "Rally powerful allies. Double your current Composure. Reduce Hostility 3.",
                tags: new[] { "faithleader" },
                configNotes: "Replace GainComposure effect with DoubleCurrentComposureEffect via Inspector.",
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(6),
                    CreateReduceHostilityEffect(3),
                });

            // Condemnation — Deal damage = enemy Hostility × 4
            // NOTE: "Scale with Hostility" requires DealDamageEqualToHostility (new system).
            CreateCard(path: $"{path}/Condemnation.asset",
                name: "Condemnation",               type: CardType.Rhetoric,
                rarity: CardRarity.Rare,            cost: 2,
                description: "Divine judgment. Deal damage equal to enemy Hostility × 4.",
                tags: new[] { "faithleader" },
                configNotes: "Replace DealDamage effect with DealDamageEqualToHostilityEffect (multiplier 4) via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDamageEffect(8),
                });

            // Revelation — Look at top 3 cards, play one free
            // NOTE: Full effect is "look at top 3, play one free" — configure Scry via Inspector.
            CreateCard(path: $"{path}/Revelation.asset",
                name: "Revelation",                 type: CardType.Pressure,
                rarity: CardRarity.Rare,            cost: 1,
                description: "The path forward becomes clear. Look at the top 3 cards. Play one for free.",
                tags: new[] { "faithleader" },
                configNotes: "Configure ScryEffect (count 3) + play-one-free logic via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDrawCardsEffect(3),
                });

            // Absolution — Exhaust hand, Gain 6 Composure per card exhausted, Draw 3
            // NOTE: Full version uses ExhaustHand + scale per card. Simplified for now.
            CreateCard(path: $"{path}/Absolution.asset",
                name: "Absolution",                 type: CardType.Policy,
                rarity: CardRarity.Rare,            cost: 2,
                description: "Sacrifice everything. Exhaust your hand. Gain 6 Composure per card exhausted. Draw 3.",
                tags: new[] { "faithleader" },
                configNotes: "Change DiscardCards to ExhaustHandEffect; replace flat GainComposure with GainComposurePerExhaustedCard (6 per card) via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDiscardCardsEffect(99),
                    CreateGainComposureEffect(6),
                    CreateDrawCardsEffect(3),
                });

            // Martyrdom — Lose all Resolve except 1, Gain Composure = Resolve lost
            // NOTE: Full effect is very complex — configure via Inspector.
            CreateCard(path: $"{path}/Martyrdom.asset",
                name: "Martyrdom",                  type: CardType.Policy,
                rarity: CardRarity.Rare,            cost: 0,
                description: "Give everything. Lose all Resolve except 1. Gain Composure equal to Resolve lost.",
                tags: new[] { "faithleader" },
                configNotes: "Replace DealDamage with LoseAllResolveExceptOneEffect + GainComposureEqualToResolveLostEffect via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDamageEffect(8),
                });
        }

        #endregion

        #region Faith Leader Player Token Cards (CardType.Pressure — generated by card effects)

        // Token cards are ordinary Pressure cards generated by other cards (Prayer, Congregation, etc.)
        // and added directly to the player's hand or deck during a run.
        // They cost 0 AP and Exhaust on play. Configure ExhaustThisCard effect in the Inspector.

        private static void GenerateFaithLeaderTokenCards(string path)
        {
            // Blessed — Pressure, 0 AP: Gain 3 Composure, Exhaust
            // Generated by: Prayer
            CreateCard(path: $"{path}/Blessed.asset",
                name: "Blessed",                    type: CardType.Pressure,
                rarity: CardRarity.Basic,           cost: 0,
                description: "A divine gift. Gain 3 Composure. Exhaust.",
                tags: new[] { "faithleader" },
                isUnplayable: false,
                configNotes: "Add ExhaustThisCard effect via Inspector.",
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(3),
                });

            // Fervor — Pressure, 0 AP: Next Blessing deals double damage, Exhaust
            // Generated by: future cards
            // NOTE: "Next Blessing deals double" is a passive trigger — configure via Inspector.
            CreateCard(path: $"{path}/Fervor.asset",
                name: "Fervor",                     type: CardType.Pressure,
                rarity: CardRarity.Basic,           cost: 0,
                description: "Divine fervour builds. Your next Blessing deals double damage. Exhaust.",
                tags: new[] { "faithleader" },
                isUnplayable: false,
                configNotes: "Add ExhaustThisCard + TurnEnd/CardPlayed passive: NextBlessingDealsDouble via Inspector.",
                effects: new CardEffect[] { });

            // Sermon Notes — Pressure, 0 AP: Draw 1, Exhaust
            // Generated by: Congregation
            CreateCard(path: $"{path}/SermonNotes.asset",
                name: "Sermon Notes",               type: CardType.Pressure,
                rarity: CardRarity.Basic,           cost: 0,
                description: "A scribbled reminder. Draw 1. Exhaust.",
                tags: new[] { "faithleader" },
                isUnplayable: false,
                configNotes: "Add ExhaustThisCard effect via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDrawCardsEffect(1),
                });
        }

        #endregion

        #region Enemy Status Cards (CardType.Status — enemy-generated, playable at a cost)

        // Status cards are put into the player's deck by enemies.
        // Unlike Curses they CAN be played to remove them, but playing them always costs something.
        // These are GENERAL — no origin tags — any enemy can inflict them on any class.
        // Configure ExhaustThisCard in the Inspector for each.
        //
        // Hounded additionally requires a TurnEndTrigger BattlePassive (Inspector):
        //   Trigger: TurnEndTrigger
        //   Effect:  RaiseAllOpponentsHostilityEffect (amount = 3)
        //   OneShot: false

        private static void GenerateFaithLeaderStatusCards(string path)
        {
            // Unnerved — Status, 0 AP: Lose 5 Composure, Exhaust
            // General: no origin tag. Brutally punishes Composure builds.
            CreateCard(path: $"{path}/Unnerved.asset",
                name: "Unnerved",                   type: CardType.Status,
                rarity: CardRarity.Basic,           cost: 0,
                description: "Your confidence cracks. Lose 5 Composure. Exhaust.",
                tags: null,
                isUnplayable: false,
                configNotes: "Add ExhaustThisCard effect via Inspector.",
                effects: new CardEffect[]
                {
                    CreateLoseComposureEffect(5),
                });

            // Hounded — Status, 0 AP: Lose 4 Resolve, Exhaust
            // General: no origin tag. On play costs HP; at end of turn ALL enemies gain 3 Hostility.
            // Wire TurnEndTrigger → RaiseAllOpponentsHostilityEffect(3) in Inspector.
            CreateCard(path: $"{path}/Hounded.asset",
                name: "Hounded",                    type: CardType.Status,
                rarity: CardRarity.Basic,           cost: 0,
                description: "They close in from every side. Lose 4 Resolve. Exhaust.\nEnd of turn while held: all enemies gain 3 Hostility.",
                tags: null,
                isUnplayable: false,
                configNotes: "Add ExhaustThisCard effect + TurnEndTrigger passive: RaiseAllOpponentsHostilityEffect (amount 3) via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDamageEffect(4),
                });

            // Stifled — Status, 2 AP: Draw 1, Exhaust
            // General: no origin tag. Pure tempo drain — 2 AP for 1 card is a losing exchange.
            CreateCard(path: $"{path}/Stifled.asset",
                name: "Stifled",                    type: CardType.Status,
                rarity: CardRarity.Basic,           cost: 2,
                description: "Every idea meets resistance. Spend 2 AP to shake it off. Draw 1. Exhaust.",
                tags: null,
                isUnplayable: false,
                configNotes: "Add ExhaustThisCard effect via Inspector.",
                effects: new CardEffect[]
                {
                    CreateDrawCardsEffect(1),
                });
        }

        #endregion

        #region Faith Leader Curse Cards

        private static void GenerateFaithLeaderCurseCards(string path)
        {
            // All curses are unplayable. Their trigger effects (on draw, turn start/end)
            // require BattlePassive configuration in the Inspector — the generator creates
            // the card shells with descriptions only.

            // Crisis of Faith — lose 4 Composure at end of turn if in hand
            CreateCard(path: $"{path}/CrisisOfFaith.asset",
                name: "Crisis of Faith",            type: CardType.Curse,
                rarity: CardRarity.Basic,           cost: 0,
                description: "Doubt creeps in. Lose 4 Composure at end of turn while held.",
                tags: new[] { "faithleader" },
                isUnplayable: true,
                configNotes: "Add TurnEndTrigger passive: LoseComposureEffect (amount 4) via Inspector.",
                effects: new CardEffect[] { });

            // Scandal — lose 3 Resolve at start of turn if in hand
            CreateCard(path: $"{path}/Scandal.asset",
                name: "Scandal",                    type: CardType.Curse,
                rarity: CardRarity.Basic,           cost: 0,
                description: "The headlines hurt. Lose 3 Resolve at start of turn while held.",
                tags: new[] { "faithleader" },
                isUnplayable: true,
                configNotes: "Add TurnStartTrigger passive: DamageResolveEffect (amount 3) via Inspector.",
                effects: new CardEffect[] { });

            // False Accusations — discard a random card when drawn
            CreateCard(path: $"{path}/FalseAccusations.asset",
                name: "False Accusations",          type: CardType.Curse,
                rarity: CardRarity.Basic,           cost: 0,
                description: "Chaos enters your hand when this does. On draw: discard a random card.",
                tags: new[] { "faithleader" },
                isUnplayable: true,
                configNotes: "Add CardDrawnTrigger passive: DiscardRandomCardEffect (amount 1, exclude Curses) via Inspector.",
                effects: new CardEffect[] { });

            // Doubt — lose 4 Composure on draw
            CreateCard(path: $"{path}/Doubt.asset",
                name: "Doubt",                      type: CardType.Curse,
                rarity: CardRarity.Basic,           cost: 0,
                description: "Uncertainty strikes the moment this enters your hand. On draw: lose 4 Composure.",
                tags: new[] { "faithleader" },
                isUnplayable: true,
                configNotes: "Add CardDrawnTrigger passive: LoseComposureEffect (amount 4) via Inspector.",
                effects: new CardEffect[] { });
        }

        #endregion

        #region Card Creation Helpers

        /// <summary>
        /// Creates a CardData ScriptableObject at <paramref name="path"/> with legacy CardEffect data.
        /// Use <paramref name="tags"/> to make reward cards discoverable by origin (e.g. "faithleader").
        /// Set <paramref name="isUnplayable"/> to true for Status and Curse cards that cannot be played.
        /// </summary>
        private static void CreateCard(string path, string name, CardType type, CardRarity rarity,
                                       string description, int cost, CardEffect[] effects,
                                       string[] tags = null, bool isUnplayable = false,
                                       string configNotes = null)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();

            // Use reflection to set private serialized fields
            var nameField        = typeof(CardData).GetField("_cardName",    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeField        = typeof(CardData).GetField("_cardType",    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rarityField      = typeof(CardData).GetField("_rarity",      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(CardData).GetField("_description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var costsField       = typeof(CardData).GetField("_costs",       System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var effectsField     = typeof(CardData).GetField("_effects",     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tagsField        = typeof(CardData).GetField("_tags",        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unplayableField  = typeof(CardData).GetField("_isUnplayable",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            nameField?.SetValue(card, name);
            typeField?.SetValue(card, type);
            rarityField?.SetValue(card, rarity);
            descriptionField?.SetValue(card, description);
            unplayableField?.SetValue(card, isUnplayable);

            if (tags != null && tags.Length > 0)
                tagsField?.SetValue(card, new List<string>(tags));

            var notesField = typeof(CardData).GetField("_configurationNotes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (!string.IsNullOrEmpty(configNotes))
                notesField?.SetValue(card, configNotes);

            List<CardCost> costs = new List<CardCost>();
            costs.Add(cost > 0
                ? new CardCost(CostType.ActionPoints, cost)
                : new CardCost(CostType.None, 0));
            costsField?.SetValue(card, costs);

            effectsField?.SetValue(card, new List<CardEffect>(effects));

            AssetDatabase.CreateAsset(card, path);
        }

        #endregion

        #region Effect Creation Helpers

        private static CardEffect CreateDamageEffect(int amount)
        {
            return CreateEffect(EffectCategory.Damage, TargetType.Opponent, damageType: DamageType.FixedDamage, damageAmount: amount);
        }

        private static CardEffect CreateRandomDamageEffect(int min, int max)
        {
            return CreateEffect(EffectCategory.Damage, TargetType.Opponent, damageType: DamageType.RandomDamage, randomMin: min, randomMax: max);
        }

        private static CardEffect CreateDamageEqualToComposureEffect()
        {
            return CreateEffect(EffectCategory.Damage, TargetType.Opponent, damageType: DamageType.DamageEqualToComposure);
        }

        private static CardEffect CreateGainComposureEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.GainComposure, resourceAmount: amount);
        }

        private static CardEffect CreateLoseComposureEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.LoseComposure, resourceAmount: amount);
        }

        private static CardEffect CreateConsumeAllComposureEffect()
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.ConsumeAllComposure);
        }

        private static CardEffect CreateComposureEqualToHostilityEffect()
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.ComposureEqualToHostility);
        }

        private static CardEffect CreateReduceHostilityEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.ReduceHostility, resourceAmount: amount);
        }

        private static CardEffect CreateGainActionPointsEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.GainActionPoints, resourceAmount: amount);
        }

        private static CardEffect CreateGainActionPointsNextTurnEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.GainActionPointsNextTurn, resourceAmount: amount);
        }

        private static CardEffect CreateDrawCardsEffect(int amount)
        {
            return CreateEffect(EffectCategory.CardManipulation, TargetType.Self, cardManipType: CardManipulationType.DrawCards, cardAmount: amount);
        }

        private static CardEffect CreateDiscardCardsEffect(int amount)
        {
            return CreateEffect(EffectCategory.CardManipulation, TargetType.Self, cardManipType: CardManipulationType.DiscardCards, cardAmount: amount);
        }

        private static CardEffect CreateEffect(
            EffectCategory category,
            TargetType target,
            DamageType damageType = DamageType.FixedDamage,
            int damageAmount = 0,
            int randomMin = 0,
            int randomMax = 0,
            ResourceEffectType resourceType = ResourceEffectType.GainComposure,
            int resourceAmount = 0,
            CardManipulationType cardManipType = CardManipulationType.DrawCards,
            int cardAmount = 0)
        {
            var effect = new CardEffect();
            var categoryField = typeof(CardEffect).GetField("_category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetField = typeof(CardEffect).GetField("_target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            categoryField?.SetValue(effect, category);
            targetField?.SetValue(effect, target);

            if (category == EffectCategory.Damage)
            {
                var damageTypeField = typeof(CardEffect).GetField("_damageType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var damageAmountField = typeof(CardEffect).GetField("_damageAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var randomMinField = typeof(CardEffect).GetField("_randomDamageMin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var randomMaxField = typeof(CardEffect).GetField("_randomDamageMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                damageTypeField?.SetValue(effect, damageType);
                damageAmountField?.SetValue(effect, damageAmount);
                randomMinField?.SetValue(effect, randomMin);
                randomMaxField?.SetValue(effect, randomMax);
            }
            else if (category == EffectCategory.Resource)
            {
                var resourceTypeField = typeof(CardEffect).GetField("_resourceType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var resourceAmountField = typeof(CardEffect).GetField("_resourceAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                resourceTypeField?.SetValue(effect, resourceType);
                resourceAmountField?.SetValue(effect, resourceAmount);
            }
            else if (category == EffectCategory.CardManipulation)
            {
                var cardManipTypeField = typeof(CardEffect).GetField("_cardManipulationType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cardAmountField = typeof(CardEffect).GetField("_cardAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                cardManipTypeField?.SetValue(effect, cardManipType);
                cardAmountField?.SetValue(effect, cardAmount);
            }

            return effect;
        }

        #endregion
    }
}
