# Events & Special Mechanics

[🏠 Home](README.md) | [📖 Full Documentation](README.md#-game-design-documentation)

**Related:** [Game Overview](game_overview.md) | [Locations](locations.md) | [Origins](origins.md) | [Resources](resources.md)

---

Random events, milestone encounters, and special systems that create dynamic gameplay.

## Table of Contents

- [Random Event System](#random-event-system)
- [Absurdist Comedy Events](#absurdist-comedy-events)
- [Heat-Triggered Events](#heat-triggered-events)
- [Utang na Loob Events](#utang-na-loob-events)
- [Milestone Events](#milestone-events)
- [Project Events](#project-events)
- [Special Mechanics Events](#special-mechanics-events)
- [Opponent AI Events](#opponent-ai-events)
- [Weather Events](#weather-events-flavor)

---

## Random Event System

### Event Trigger Mechanics
- **Daily Roll**: 30% chance per day for random event
- **Location-Specific**: Some events only trigger at certain locations
- **Heat-Triggered**: Events become more frequent/severe as Heat increases
- **Relationship-Triggered**: High/low Utang na Loob triggers special events
- **Story Events**: Scripted events at specific days

---

## Absurdist Comedy Events

### "Troll Farm Offers Services"
**Trigger**: Random, Days 10-40  
**Location**: Any Social location

**Event Description:**
A shady operator approaches you with an offer: "Boss, we can make you trend! Thousands of fake accounts, all supporting you!"

**Choices:**
1. **Accept Full Package** (₱500)
   - Gain +2000 Support (fake)
   - +20H (suspicious activity)
   - Risk: 20% chance they turn on you later (random betrayal event)

2. **Accept But Supervise** (₱800 + 3L)
   - Gain +1500 Support (more believable)
   - +10H
   - More control, lower betrayal risk (5%)

3. **Decline and Report** (Free)
   - +5U (ethical behavior)
   - -5H (good PR)
   - Lose potential Support

4. **Hire Them to Attack Opponent** (₱700 + 5L)
   - Opponent loses 1000 Support
   - +30H (dirty tactics)
   - High risk of exposure

**Outcomes:**
- If betrayed: Troll farm exposes your arrangement (-3000 Support, +50H, scandal risk)
- If successful: Maintain fake support until election

---

### "Viral Dance Challenge"
**Trigger**: Random, Days 5-35  
**Location**: Mall, Basketball Court, TV Studio

**Event Description:**
A popular dance trend is sweeping social media. Your team suggests you participate for visibility.

**Choices:**
1. **Go All In - Professional Video** (₱1000 + 20 Clout)
   - **Success (60% chance)**: +3000 Support, +50 Clout, unlock "Budots" charm card
   - **Failure (40% chance)**: Become cringe meme, +30H, but gain "So Bad It's Good" power card

2. **Casual Attempt - Smartphone Video** (Free)
   - **Success (40% chance)**: +1500 Support, +20 Clout
   - **Failure (60% chance)**: Minor embarrassment, +10H

3. **Hire a Body Double** (₱500)
   - Guaranteed +800 Support
   - +15H if discovered (likely 30% chance)

4. **Decline** (Free)
   - Miss opportunity
   - -500 Support (seen as out of touch)

**Origin Modifiers:**
- **Celebrity**: +30% success chance
- **Religious Leader**: -20% success chance (awkward)
- **Strongman**: Auto-fail, but gains "Tough Guy" reputation instead (+500 Support from traditionalists)
- **Nepo Baby**: Can pay ₱2000 to guarantee success

---

### "Faith Healer Endorsement"
**Trigger**: Random, Days 15-40  
**Location**: Church, Barangay Hall, Palengke

**Event Description:**
A famous faith healer offers to endorse you and perform "miracles" at your rallies.

**Choices:**
1. **Accept Fully** (Free)
   - Gain "Miracle Rally" power card (Legendary - massive Support gain)
   - +1000 Support (rural areas)
   - +15H (urban journalists mock you)
   - +20 Faith (Religious Leader only)

2. **Accept But Downplay** (₱300)
   - Gain +600 Support
   - +5H
   - Moderate benefits

3. **Decline Politely** (Free)
   - +2U (respectful)
   - 0H
   - Miss opportunity

4. **Expose as Fraud** (Free)
   - +1000 Support (rational voters)
   - -10U (offend believers)
   - +20H (controversial stance)
   - Make enemy of faith healer (future antagonist)

**Special:**
- **Religious Leader**: Can choose "Collaborate on Legitimate Healing Mission" (+1500 Support, +40 Faith, 0H)

---

### "Campaign Jingle Goes Viral (Wrong Reasons)"
**Trigger**: Automatic, Day 20  
**Location**: N/A (happens automatically)

**Event Description:**
Your campaign jingle is released. It's... memorable. For all the wrong reasons. But people can't stop humming it.

**Automatic Effects:**
- +1500 Support (memorable = votes)
- +15H (intelligentsia ridicule it)
- Unlock "Earworm" leverage card (Opponent skips next turn, too busy humming your song)

**Follow-up Choices:**
1. **Lean Into It - Make Remixes** (₱500)
   - +1000 additional Support
   - +10H (doubling down on cringe)
   - Become meme candidate (permanent +200 Support/day, +5H/day)

2. **Hire Better Musicians** (₱1500)
   - Release new jingle
   - -10H (damage control)
   - Lose "Earworm" card
   - +500 Support (better quality)

3. **Ignore and Move On** (Free)
   - Accept current state
   - No additional effects

**Origin Reactions:**
- **Celebrity**: Can convert to +30 Clout ("No such thing as bad publicity")
- **Religious Leader**: -15 Faith (undignified)
- **Strongman**: +10 Fear (even the jingle is threatening somehow)
- **Nepo Baby**: Can pay ₱3000 to bury it completely (-1500 Support, -25H)

---

### "The Prophecy"
**Trigger**: Random, Days 25-40  
**Location**: Prayer Mountain, Church, any Religious location

**Event Description:**
A mystic/prophet claims to have a vision about the election. They predict either your victory or doom.

**Random Outcome (50/50):**

**Positive Prophecy:**
- "You are destined to lead!"
- +1200 Support (believers)
- +25 Faith (Religious Leader)
- -10H (divine backing claimed)

**Negative Prophecy:**
- "Dark forces conspire against you!"
- -800 Support (superstitious voters scared)
- Can pay 20L to "change fate" (bribe the prophet)
- OR accept and gain "Underdog" status (+500 Support from sympathizers)

**Choices After Prophecy:**
1. **Embrace It** (Free)
   - Accept the outcome
   - Build narrative around it

2. **Discredit the Prophet** (₱800)
   - Reverse Support effects
   - +20H (attacking religious figure)
   - -5U

3. **Commission Your Own Prophecy** (10L)
   - Choose your own outcome
   - +40H (obvious manipulation)
   - Conflicting prophecies become scandal

---

## Heat-Triggered Events

### "Investigative Reporter Appears" (Heat 26-50)
**Trigger**: First time entering 26-50H range  
**Location**: Any public location

**Event Description:**
A journalist approaches with pointed questions about your campaign finances and tactics.

**Card Battle**: Journalist (Leverage-heavy deck, uses "Exposé" cards)

**Victory:**
- Successfully deflect questions
- -10H (handled well)
- +500 Support (press conference victory)

**Defeat:**
- Damaging story published
- +20H
- -1000 Support
- Unlock follow-up "Damage Control" event

**Alternative Resolution:**
- Pay 8L to "convince" journalist (no battle, +5H)
- Use 10U to appeal to their conscience (no battle, -5H)

---

### "Scandal Brewing" (Heat 51-75)
**Trigger**: Random when in this range, 20% chance per day  
**Location**: N/A (can trigger anywhere)

**Event Description:**
Your team warns you that journalists are close to uncovering something damaging.

**Scandal Type (Random):**
1. **Financial Irregularities** (if you've taken kickbacks)
2. **Hidden Relationships** (random personal scandal)
3. **False Credentials** (education/background questions)
4. **Caught on Camera** (compromising photos/video)

**Choices:**
1. **Aggressive Defense** (₱2000)
   - Legal team attacks story
   - 50% chance to prevent publication (+0H)
   - 50% chance to make it worse (+30H, Streisand effect)

2. **Get Ahead of Story** (Free)
   - Public confession/apology
   - +15H (controlled damage)
   - -1500 Support
   - But prevents worse scandal

3. **Bribe/Threaten Journalist** (15L)
   - Prevent publication
   - +10H (suspicious)
   - 30% chance journalist goes public anyway (massive scandal)

4. **Ignore and Hope** (Free)
   - 50% story fizzles out (0H)
   - 50% scandal breaks (+40H, -2500 Support)

---

### "Assassination Attempt" (Heat 76-99)
**Trigger**: When visiting risky locations (Cockpit, Alak, late night anywhere)  
**Location**: Specified risky locations

**Event Description:**
As you leave the area, your security detail shouts a warning. Gunshots ring out.

**Card Battle**: Assassins (Heavy Attack deck, fast and brutal)

**Victory:**
- Survive attempt
- +2000 Support (sympathy)
- +20H (violence attracts attention)
- Unlock "Martyr" status (temporary invulnerability to one scandal)

**Defeat:**
- **Game Over** (unless you have specific cards/services)
- "Security Detail" service prevents this (consumes service)
- "Martyr Complex" card can save you (Nepo Baby)

**Alternative Resolution:**
- Pay 10L before battle to "call off the hit" (no battle, +15H)
- Use 30U to reveal assassin identity and turn them (become ally, +1000 Support)

---

### "COMELEC Investigation Begins" (Heat 76-99)
**Trigger**: Automatic when entering this range  
**Location**: N/A (summons to COMELEC HQ)

**Event Description:**
The Commission on Elections has opened an investigation into your campaign practices.

**Investigation Focus (Based on Actions):**
- **If high Lagay use**: Financial crimes
- **If violence used**: Human rights violations
- **If lies exposed**: Electoral fraud
- **If multiple**: Combined charges

**Card Battle**: COMELEC Panel (Defense/Leverage-heavy, very difficult)

**Victory:**
- Investigation dismissed
- -20H
- +1000 Support (cleared name)

**Defeat:**
- Hearing continues
- Daily penalty: -200 Support until resolved
- Must win follow-up battle or face disqualification

**Alternative Resolutions:**
- Pay ₱5000 (legal defense, +30% win chance)
- Pay 30L (bribe officials, auto-win, +50H if discovered)
- Use 40U (political connections intervene, auto-win, -10U)

---

## Utang na Loob Events

### "A Favor Repaid" (Positive Utang na Loob >30)
**Trigger**: Random, when Utang na Loob >30  
**Location**: Any

**Event Description:**
Someone you helped in the past comes to your aid unprompted.

**Random Benefits:**
- Free card (Rare or Legendary)
- Resource gift (₱1000 or 5L or +1000 Support)
- Intelligence on opponent
- Scandal cover-up (-20H)
- Emergency assistance in battle

**No Choice Required**: Automatic benefit from good karma

---

### "Called In for a Favor" (Positive Utang na Loob >20)
**Trigger**: Random, when Utang na Loob >20  
**Location**: Any

**Event Description:**
An NPC you've helped asks you to return the favor. It's important to them.

**Request Types:**
1. **Help with Personal Problem** (Costs time: 1 day)
   - Keep Utang na Loob (no loss)
   - +5U (strengthens relationship)

2. **Financial Help** (₱500-1500)
   - Keep Utang na Loob
   - +3U

3. **Political Favor** (Use influence, +10H)
   - Keep Utang na Loob
   - +5U
   - Risky but loyal

**Refusal:**
- -15U (betrayal)
- -500 Support (word spreads)
- NPC becomes enemy

---

### "The Betrayed" (Negative Utang na Loob <-10)
**Trigger**: When Utang na Loob drops below -10  
**Location**: Any

**Event Description:**
People you've wronged are spreading damaging stories about you.

**Automatic Effects:**
- -1000 Support
- +15H
- Cannot gain positive Utang na Loob for 5 days

**Card Battle**: Betrayed NPCs (Motivated by revenge, difficult)

**Victory:**
- Silence opposition
- Effects reduced by half
- +10H (intimidation tactics)

**Defeat:**
- Effects double (-2000 Support, +30H)
- NPCs continue opposing you

**Alternative Resolution:**
- Make Amends (₱2000 + 10U)
  - Reverse betrayal
  - +20U
  - -1000 Support (takes time to rebuild)

---

### "Blood is Thicker" (Negative Utang na Loob <-25)
**Trigger**: Automatic when dropping below -25  
**Location**: N/A

**Event Description:**
Your betrayals have turned even family and close friends against you. A major supporter publicly withdraws endorsement.

**Automatic Effects:**
- -3000 Support
- +30H
- Lose one owned location
- "Traitor" status (all Utang na Loob gains reduced by 50% until redeemed)

**Redemption Quest:**
Must complete series of good deeds:
1. Help 5 NPCs without expecting return
2. Complete 2 community service events
3. Keep 3 promises
4. Win back former ally (difficult Charm challenge)

**Completion Reward:**
- Remove "Traitor" status
- +10U
- Restore reputation

---

## Milestone Events

### Day 7, 14, 21, 28, 35, 42: "TV Debate"
**Trigger**: Automatic every 7 days  
**Location**: TV Studio or Town Plaza (broadcast)

**Event Description:**
Face off against rival politicians in televised debate. High stakes, high visibility.

**Card Battle**: Rival Politician (Varies by day, gets harder)

**Battle Modifiers:**
- **Debate Format**: Special rules apply
  - Each turn, answer question (thematic card play required)
  - Audience reaction affects next turn
  - Media scoring tracks performance

**Victory:**
- +2000 Support
- -10H (good press)
- +30 Clout (Celebrity)
- +15 Fear (Strongman, if aggressive)
- +20 Faith (Religious Leader, if moral)
- +15 Influence (Nepo Baby, if confident)

**Defeat:**
- -1500 Support
- +20H (bad performance analyzed)
- Opponent gains momentum

**Draw:**
- +500 Support (participated)
- No Heat change
- Rematch offer

**Special Debate Topics:**
- **Day 7**: "Why Should They Vote for You?" (Introduction)
- **Day 14**: "The Economy" (₱-based strategies favored)
- **Day 21**: "Law and Order" (Strongman advantage)
- **Day 28**: "Moral Leadership" (Religious Leader advantage)
- **Day 35**: "Vision for Youth" (Celebrity advantage)
- **Day 42**: "Final Arguments" (No advantage, pure skill)

---

### Day 30: "COMELEC Filing Deadline"
**Trigger**: Automatic, Day 30  
**Location**: COMELEC HQ

**Event Description:**
Official filing deadline. Must have minimum requirements to continue.

**Requirements:**
- **Minimum Support**: 5,000
- **Maximum Heat**: <100 (scandal disqualification)
- **Filing Fee**: ₱500

**Passed Requirements:**
- Continue campaign normally
- Official candidate status
- Access to endgame locations

**Failed Support Requirement:**
- **Option 1**: Pay ₱5000 to bypass (desperate bribe)
  - Continue campaign
  - +40H (suspicious)
  - Must reach 7000 Support by Day 35 or disqualified

- **Option 2**: Accept Defeat
  - Game Over
  - "Failed to Qualify" ending

**Failed Heat Requirement (>100):**
- Automatic scandal event (see Scandal System)
- Must survive scandal to continue
- If survived, can file late (Day 31, +30H penalty)

---

### Day 45: "Election Day"
**Trigger**: Automatic, final day  
**Location**: N/A (Results announcement)

**Event Description:**
The votes are counted. Your fate is decided.

**Results Calculation:**
- Total Support earned
- Subtract Heat penalties (-1 Support per 1H above 50)
- Add Utang na Loob bonuses (+50 Support per 1U above 20)
- Random events (±500 Support, represents ground game)

**Victory Threshold**: 10,000 Support

**Victory:**
- Ending based on method (see Victory Conditions in GAME_OVERVIEW.md)
- Unlock new content
- Political Capital rewards

**Defeat:**
- "Lost Election" ending
- Still earn Political Capital (less)
- Unlock "Comeback" achievement (if close)

**Special Scenarios:**

**"Contested Results" (Within 500 Support of victory):**
- Trigger Electoral Protest event
- Can spend resources to force recount
- ₱10,000 OR 50L OR 80U
- 50% chance to flip result

**"Landslide Victory" (>15,000 Support):**
- Special "Mandate" ending
- Double Political Capital
- Unlock special party for next run

**"Pyrrhic Victory" (<0 Utang na Loob but >10,000 Support):**
- Won but alone
- "Isolated Victor" ending
- Unlock special cards

---

## Project Events

### "Bridge Construction Proposal"
**Trigger**: Random, Days 10-35  
**Location**: Barangay Hall, Office Buildings

**Event Description:**
A contractor proposes building a new bridge. How much will you pocket?

**Choices:**
1. **Clean Project** (Free)
   - +200 Campaign Funds
   - +1000 Support
   - +0 Heat
   - Takes 5 days

2. **20% Kickback** (Free)
   - +800 Campaign Funds
   - +800 Support
   - +15 Heat
   - Takes 5 days

3. **50% Kickback** (Free)
   - +2000 Campaign Funds
   - +400 Support
   - +35 Heat
   - Gain "Ghost Project" leverage card
   - Takes 3 days

4. **Reject Completely** (Free)
   - +0 Campaign Funds
   - +100 Support (good governance)
   - -5 Heat
   - +5U

**Follow-up Event (If kickback taken):**
- "Audit Questions" (Random, 30% chance)
- Must defend project or face +30H and return funds

---

### "Overpriced Streetlights"
**Trigger**: Random, Days 15-40  
**Location**: Office Buildings, Barangay Hall

**Event Description:**
An "innovative" streetlight project with suspiciously high costs.

**Choices:**
1. **Approve Standard Price** (Free)
   - +₱300
   - +500 Support
   - +5H

2. **Approve Inflated Price** (Free)
   - +₱1500
   - +200 Support
   - +25H
   - 40% chance of audit

3. **Expose and Reject** (Free)
   - +0 Campaign Funds
   - +800 Support (anti-corruption)
   - -10H
   - +8U
   - Make enemy of contractor

---

### "Ayuda Distribution"
**Trigger**: Random, Days 5-40  
**Location**: Barangay Hall, Palengke

**Event Description:**
Distribute aid packages to constituents. How will you handle it?

**Choices:**
1. **Legitimate Distribution** (₱1500)
   - +2000 Support
   - +10U
   - -5H (good PR)

2. **Distribution with Branding** (₱1000)
   - +2500 Support (your name on everything)
   - +5U
   - +10H (vote buying accusations)

3. **Selective Distribution** (₱800)
   - +1800 Support (only to supporters)
   - -5U (unfair)
   - +15H

4. **Ghost Ayuda** (₱300)
   - Keep most money (+₱1200 profit)
   - +500 Support (minimal distribution)
   - +40H (fraud)
   - High scandal risk

---

### "Sports Complex Project"
**Trigger**: Random, Days 20-40  
**Location**: Basketball Court, Office Buildings

**Event Description:**
Build a sports facility for the youth. Investment in the future... or kickback opportunity?

**Choices:**
1. **Full Investment** (₱2000)
   - +2500 Support (youth)
   - +15U
   - -10H (good press)
   - Permanent +100 Support/day (youth demographic)

2. **Moderate Investment** (₱1000)
   - +1500 Support
   - +8U
   - +0H

3. **Minimal + Kickback** (₱300)
   - +₱1200 profit
   - +500 Support
   - +30H
   - Shoddy construction (may collapse later = scandal)

---

### "Dolomite Beach Project"
**Trigger**: Random, Days 15-35  
**Location**: Town Plaza (if coastal region)

**Event Description:**
A controversial "beautification" project. Critics call it wasteful, supporters call it tourism.

**Choices:**
1. **Full Support** (Free)
   - +1500 Support (certain demographics love it)
   - -1000 Support (environmentalists hate it)
   - +20H (controversy)
   - Net: +500 Support, +20H

2. **Modified Eco-Friendly Version** (₱1000)
   - +1200 Support
   - -200 Support (some disappointed)
   - +5H
   - Net: +1000 Support, +5H

3. **Reject Project** (Free)
   - +800 Support (environmentalists)
   - -500 Support (tourism advocates)
   - -8H (responsible choice)
   - +5U

**Special**: Absurdist element - regardless of choice, becomes meme

---

### "Drug War Funding"
**Trigger**: Random, Days 10-30  
**Location**: Police Station (Strongman only)

**Event Description:**
Police request funding for controversial anti-drug operations. Extreme choice.

**Choices:**
1. **Full Funding + Participation** (₱2000)
   - +3000 Support (law & order voters)
   - -2000 Support (human rights advocates)
   - +80H (international condemnation)
   - +50 Fear (Strongman)
   - Unlock "Tokhang+" upgraded card
   - High scandal risk

2. **Funding Only** (₱1000)
   - +1500 Support
   - -1000 Support
   - +40H
   - +25 Fear

3. **Alternative Approach** (₱1500)
   - Rehabilitation programs instead
   - +1000 Support (progressives)
   - -500 Support (conservatives)
   - +10H (soft on crime accusations)
   - +10U

4. **Reject Completely** (Free)
   - -1000 Support (seen as weak)
   - +500 Support (human rights advocates)
   - -10H
   - +8U

---

## Special Mechanics Events

### "The Palakasan Web"
**Trigger**: When first achieving 15U with any NPC  
**Location**: Varies

**Event Description:**
Your connection with this NPC opens doors to their entire network.

**Effects:**
- Reveal relationship web (visual UI)
- See all NPCs connected to this one
- Gain +1U with all connected NPCs
- Unlock "Network Politics" achievement

**Follow-up:**
- Can now leverage these relationships
- Helping one helps connected NPCs (+1U cascade)
- Betraying one angers entire network (-5U cascade)

---

### "Dynasty Meeting" (Nepo Baby Only)
**Trigger**: Can be called once per run, manually  
**Location**: Family Compound

**Event Description:**
Call an emergency family council meeting. They're not happy you need help.

**Requirements:**
- Must be at Family Compound
- Costs 1 day

**Benefits:**
- Restore all resources to starting values
- Gain +20 Influence
- Remove one negative status effect
- Get strategic advice (opponent intel)

**Costs:**
- -10U (family disappointed in you)
- +15H (publicized family intervention)
- Can only use once per run

---

### "The Scandal Explosion" (Heat 100+)
**Trigger**: Automatic when Heat reaches 100  
**Location**: N/A (emergency event)

**Event Description:**
Everything hits at once. Multiple scandals break simultaneously. This is the reckoning.

**Scandal Battle:**
- **Special Card Battle**: "The Truth" (Uses exposé cards, extremely difficult)
- **Multiple phases**: Must survive 3 rounds
- **All resources needed**: Uses ₱, L, U, and special resources

**Phase 1: "The Exposé"**
- Journalists reveal your corruption
- Must defend with ₱ or cards
- -5000 Support automatically

**Phase 2: "Public Outcry"**
- Protests and calls for withdrawal
- Must win Charm challenge or spend 30U
- Additional -2000 Support

**Phase 3: "The Judgment"**
- COMELEC decision
- Must win final card battle OR spend 50L

**Victory:**
- Survive scandal
- Heat reset to 60
- -7000 Support total
- Permanent penalty: -10% to all card effectiveness
- "Scarred but Standing" status
- Can continue campaign

**Defeat:**
- **Game Over**
- "Disqualified by Scandal" ending
- Earn minimal Political Capital

**Special Survivals:**
- **Religious Leader "Faith Shield"**: Auto-pass Phase 1 (if available)
- **Nepo Baby "Martyr Complex"**: Convert Heat to Support (if available)
- **Celebrity "Comeback Movie"**: -30H before scandal (if available)
- **Strongman "Loyal Fanatics"**: Base support never drops (500 minimum maintained)

---

## Opponent AI Events

### "Rival's Dirty Trick"
**Trigger**: Random, Days 20-40  
**Location**: N/A

**Event Description:**
Your opponent launches a smear campaign against you.

**Effects:**
- -1000 Support
- +15H (negative attention)
- Must respond

**Response Options:**
1. **Counter-Attack** (₱800)
   - Launch own smear campaign
   - Opponent loses 1500 Support
   - +20H (mud-slinging)

2. **Take High Road** (Free)
   - Ignore attacks
   - +5U (dignified)
   - -500 additional Support (unanswered attacks hurt)

3. **Fact-Check and Debunk** (₱500)
   - Methodical response
   - Restore 800 Support
   - -5H (good press)

4. **Dirty Counter** (10L)
   - Extreme response
   - Opponent loses 2500 Support
   - +40H (very dirty)

---

### "Opponent Scandal"
**Trigger**: Random, Days 15-40  
**Location**: N/A

**Event Description:**
Your opponent is caught in a scandal. How will you respond?

**Choices:**
1. **Pile On** (Free)
   - Aggressive attacks
   - Gain 1500 Support
   - +10H (seen as opportunistic)

2. **Stay Silent** (Free)
   - Take high road
   - Gain 500 Support (some respect restraint)
   - +5U
   - Miss opportunity for knockout blow

3. **Express Sympathy** (Free)
   - Classy move
   - Gain 300 Support
   - +10U (very dignified)
   - Opponent may remember this

4. **Secretly Fan Flames** (8L)
   - Make scandal worse covertly
   - Opponent loses additional 2000 Support
   - +25H (if discovered)
   - 40% chance of exposure

---

## Weather Events (Flavor)

### "Typhoon Warning"
**Trigger**: Random, Days 10-40  
**Location**: N/A (affects travel)

**Event Description:**
A typhoon is approaching. Campaign activities affected.

**Effects:**
- Next 2 days: Cannot travel (stay in current location)
- Random damage to locations (-500 Support from affected areas)

**Response Options:**
1. **Organize Relief** (₱1500)
   - Disaster response
   - +2500 Support (hero status)
   - +15U
   - -10H (good PR)

2. **Minimal Response** (₱500)
   - Token gesture
   - +800 Support
   - +3U

3. **Focus on Campaign** (Free)
   - Ignore disaster
   - -1500 Support (heartless)
   - +20H (bad PR)
   - -10U

4. **Photo Op Only** (₱200)
   - Fake concern
   - +600 Support initially
   - +30H (if exposed as insincere, 60% chance)

---

*Events create dynamic, unpredictable campaigns. Mastering event responses separates winners from losers.*