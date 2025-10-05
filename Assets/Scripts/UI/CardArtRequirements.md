# Card Art Requirements

## File Specifications

### Individual Card Files (For Testing/Prototyping)
- **Resolution:** 1024 x 1536 px or 2048 x 3072 px
- **Aspect Ratio:** 2:3 (standard playing card)
- **Format:** PNG (with transparency support)
- **Color Space:** sRGB
- **DPI:** 300 (for high quality)

### Texture Atlas (For Production)
- **Atlas Size:** 4096 x 4096 px (recommended) or 8192 x 8192 px (high quality)
- **Grid Layout:** 6 columns x 7 rows = 42 cards per atlas
- **Individual Card Slot Size:** ~682 x 585 px (with padding between cards)
- **Format:** PNG
- **Color Space:** sRGB

## Card Layout & Safe Zones

When designing card artwork, reserve space for UI overlays:

```
Total Height: 2048px (example for 2048x3072 card)

┌─────────────────────────┐
│   TOP SAFE ZONE: 300px  │ ← Cost circle overlay
│─────────────────────────│
│                         │
│   MAIN ART AREA         │ ← Primary artwork (1372px height)
│   (Center focus here)   │
│                         │
│─────────────────────────│
│ BOTTOM SAFE ZONE: 400px │ ← Description text box
└─────────────────────────┘

Side Margins: 100px each side
```

### Safe Zone Guidelines:
- **Border/Margin:** 100px on all sides
- **Top Reserved Area:** 300px (cost indicator, card name)
- **Bottom Reserved Area:** 400px (description, effects text)
- **Center Art Focus:** 1372px middle section (main artwork should be centered here)

## Layer Structure (Recommended)

### Option 1: Single Baked Texture
- Combine all layers (art + frame + effects) into one PNG
- **Pros:** Simplest workflow, best performance
- **Cons:** Can't animate layers separately

### Option 2: Separate Layers (Recommended for flexibility)

**Card Art Layer (Primary):**
- Pure artwork without borders/frames
- 2048 x 3072 px
- Export as: `CardName_Art.png`

**Frame Layer (Per Rarity):**
- Border/frame design with transparent center
- 2048 x 3072 px
- Reusable across all cards of same rarity
- Export as: `Frame_Basic.png`, `Frame_Enhanced.png`, `Frame_Rare.png`

**Optional Foil/Holo Layer (Rare cards only):**
- Overlay effects (shine, holographic patterns)
- 2048 x 3072 px
- Can be animated/shader-driven in Unity
- Export as: `Holo_Overlay.png`

## Art Style Guidelines

### Card Types (Color Coding)
Following the Griftlands-inspired system:

- **Diplomacy (Green):** Peaceful, persuasive imagery
  - Color palette: Greens, warm earth tones, soft lighting
  - Themes: Conversation, handshakes, religious symbols, community

- **Hostility (Red):** Aggressive, threatening imagery
  - Color palette: Reds, dark tones, harsh shadows
  - Themes: Confrontation, weapons, fire, intensity

- **Manipulate (Purple):** Utility, cunning, resource-focused
  - Color palette: Purples, blues, mysterious tones
  - Themes: Cards, money, schemes, abstract concepts

### Rarity Visual Distinctions & Color Palettes

#### **Basic Rarity**
- **Frame Style:** Simple border, clean lines, matte finish
- **Frame Color:** Silver/Grey (#B8B8B8, #E0E0E0)
- **Accent Color:** White highlights (#FFFFFF)
- **Glow:** None
- **Effects:** No particles or special effects

#### **Basic+ (Upgraded)**
- **Frame Style:** Same as Basic, but with subtle reinforced corners
- **Frame Color:** Brighter silver (#D4D4D4, #F0F0F0)
- **Accent Color:** Light blue tint (#C0D8E8)
- **Indicator:** Small "+" symbol in top-right corner
- **Glow:** Very subtle white outline (optional)

#### **Enhanced Rarity**
- **Frame Style:** Ornate border with decorative corners, slight embossing
- **Frame Color:** Gold (#D4AF37, #FFD700)
- **Accent Color:** Warm yellow highlights (#FFF4A3)
- **Glow:** Soft golden glow around edges
- **Effects:** Occasional sparkle particles, subtle shimmer on frame

#### **Enhanced+ (Upgraded)**
- **Frame Style:** Same as Enhanced with additional filigree details
- **Frame Color:** Brighter gold with rose gold accents (#FFD700, #E8B4A8)
- **Accent Color:** Bright yellow-gold (#FFEB3B)
- **Indicator:** Glowing "+" symbol in top-right corner
- **Glow:** More pronounced golden glow, pulsing effect
- **Effects:** More frequent sparkles

#### **Rare Rarity**
- **Frame Style:** Elaborate frame with complex patterns, 3D embossed look
- **Frame Color:** Deep purple to blue gradient (#6A0DAD, #4B0082, #1E3A8A)
- **Accent Color:** Cyan/electric blue highlights (#00FFFF, #66D9EF)
- **Glow:** Bright blue-purple glow, animated shimmer
- **Effects:** Holographic rainbow shift, constant particle aura, animated shader

#### **Rare+ (Upgraded)**
- **Frame Style:** Same as Rare with additional crystalline/gem elements
- **Frame Color:** Vibrant purple-blue with prismatic edges (#8B00FF, #4169E1)
- **Accent Color:** Bright cyan with rainbow shift (#00FFFF, rainbow gradient)
- **Indicator:** Animated/pulsing "+" symbol with particle trail
- **Glow:** Intense animated glow, color-shifting between purple/blue/cyan
- **Effects:** Intense holographic effects, VFX Graph particles, full rainbow spectrum shift

## Color Reference (Hex Codes)

### Basic Rarity Palette
```
Frame Primary: #B8B8B8 (Silver Grey)
Frame Secondary: #E0E0E0 (Light Silver)
Accent: #FFFFFF (White)
Upgraded Tint: #C0D8E8 (Pale Blue)
```

### Enhanced Rarity Palette
```
Frame Primary: #D4AF37 (Metallic Gold)
Frame Secondary: #FFD700 (Gold)
Accent: #FFF4A3 (Light Yellow)
Glow: #FFD700 with 30% opacity
Upgraded Accent: #E8B4A8 (Rose Gold)
```

### Rare Rarity Palette
```
Frame Primary: #6A0DAD (Purple)
Frame Secondary: #4B0082 (Indigo)
Frame Tertiary: #1E3A8A (Deep Blue)
Accent: #00FFFF (Cyan)
Highlight: #66D9EF (Sky Blue)
Glow: #8B00FF with 50% opacity
Upgraded Rainbow: Full spectrum (HSV shift animation)
```

## Card Type Color Overlays (Subtle Tints)

### Diplomacy Cards (Green)
- **Tint Color:** #4CAF50 (10% opacity overlay on art)
- **Border Accent:** Green undertone in frame shadows

### Hostility Cards (Red)
- **Tint Color:** #F44336 (10% opacity overlay on art)
- **Border Accent:** Red undertone in frame shadows

### Manipulate Cards (Purple)
- **Tint Color:** #9C27B0 (10% opacity overlay on art)
- **Border Accent:** Purple undertone in frame shadows

## Unity Import Settings (Reference)

Once imported into Unity:
- **Texture Type:** Sprite (2D and UI)
- **Sprite Mode:** Multiple (for atlases) or Single (for individual files)
- **Pixels Per Unit:** 100
- **Max Size:** 2048 or 4096
- **Compression:** ASTC (mobile) or BC7 (PC) - High Quality
- **Generate Mip Maps:** Yes
- **Filter Mode:** Bilinear or Trilinear

## Procreate Setup (Recommended Settings)

### Individual Card Canvas:
```
Width: 2048 px
Height: 3072 px
DPI: 300
Color Profile: sRGB
```

### Atlas Canvas:
```
Width: 4096 px (or 8192 px)
Height: 4096 px (or 8192 px)
DPI: 300
Color Profile: sRGB
Grid: 6 columns x 7 rows
```

## Workflow Recommendations

1. **Start with Individual Cards**
   - Design 5-10 cards as individual files
   - Test in Unity to verify they look good
   - Iterate on style/composition

2. **Create Frame Templates**
   - Design one frame per rarity
   - Make them reusable across all cards

3. **Batch Production**
   - Once style is locked, create all card art
   - Keep layers organized (art separate from frame)

4. **Combine into Atlas**
   - When you have 20+ cards, combine into atlas
   - Use Unity's Sprite Slicer or manual grid layout
   - Or use external tools like TexturePacker

## File Naming Convention

### Individual Files:
```
CardName_Art.png          (e.g., "ThreatenedStrike_Art.png")
Frame_Basic.png
Frame_Enhanced.png
Frame_Rare.png
Holo_Overlay.png
```

### Atlas Files:
```
CardAtlas_Basic.png       (Contains all Basic rarity cards)
CardAtlas_Enhanced.png    (Contains all Enhanced rarity cards)
CardAtlas_Rare.png        (Contains all Rare rarity cards)
```

## Memory Budget (Target)

- **Individual Card (2048x3072):** ~2-4 MB compressed
- **Atlas (4096x4096, 42 cards):** ~8-16 MB compressed
- **Target for 100 cards:** ~150-300 MB total (across multiple atlases)
- **Active memory (10 cards on screen):** ~20-40 MB

## Notes

- Start with lower resolution (1024x1536) for rapid prototyping
- Upgrade to 2048x3072 for final production
- Consider 4096x6144 only if targeting high-end PC with zoom features
- Always export with transparency (PNG) for layering flexibility
- Keep source files (.procreate, .psd) organized with layers intact
