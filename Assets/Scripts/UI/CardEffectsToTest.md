# Card MMFeedback Effects Reference

## Card Draw Effects
- [ ] **MMF_Scale** - Card pops in from small to normal size
- [ ] **MMF_Position** - Card slides from deck to hand position
- [ ] **MMF_Rotation** - Card rotates into upright position
- [ ] **MMF_Sound** - Whoosh/card flip sound
- [ ] **MMF_FloatingText** - Optional: show card name briefly
- [ ] **MMF_Particles** - Dust/sparkle particles on draw
- [ ] **MMF_Events** - Trigger custom draw logic

## Card Hover Effects
- [ ] **MMF_Scale** - Slight scale up (1.0 -> 1.1)
- [ ] **MMF_Position** - Lift card up on Y axis
- [ ] **MMF_Rotation** - Slight tilt toward camera
- [ ] **MMF_Light** - Add rim light/glow
- [ ] **MMF_MaterialProperty** - Brighten card material
- [ ] **MMF_Sound** - Subtle hover sound
- [ ] **MMF_Haptics** - Controller rumble (if applicable)
- [ ] **MMF_Springs** - Springy movement using MMSpringPosition

## Card Select/Click Effects
- [ ] **MMF_CameraShake** - Tiny camera shake on select
- [ ] **MMF_Scale** - Quick punch scale (1.1 -> 1.05)
- [ ] **MMF_Flash** - Flash renderer white briefly
- [ ] **MMF_Sound** - Click/select sound
- [ ] **MMF_Particles** - Energy burst particles
- [ ] **MMF_ChromaticAberration** - Quick PP effect
- [ ] **MMF_Events** - Trigger card selection logic
- [ ] **MMF_Wiggle** - Card wiggle animation

## Card Discard Effects
- [ ] **MMF_Scale** - Shrink to zero
- [ ] **MMF_Position** - Move to discard pile position
- [ ] **MMF_Rotation** - Spin while discarding
- [ ] **MMF_Fade** - Fade out renderer
- [ ] **MMF_Sound** - Discard sound (different from draw)
- [ ] **MMF_Particles** - Dissolve/smoke particles
- [ ] **MMF_DestroySelf** - Remove card object after animation
- [ ] **MMF_MaterialProperty** - Desaturate/darken

## Card Drag Effects (Optional)
- [ ] **MMF_Position** - Follow mouse/touch position
- [ ] **MMF_Rotation** - Slight rotation while dragging
- [ ] **MMF_Springs** - Springy follow movement
- [ ] **MMF_TrailRenderer** - Motion trail
- [ ] **MMF_MaterialProperty** - Transparency while dragging

## Card Return to Hand Effects
- [ ] **MMF_Position** - Spring back to hand position
- [ ] **MMF_Rotation** - Rotate to hand angle
- [ ] **MMF_Springs** - Bouncy return animation
- [ ] **MMF_Sound** - Snap back sound

## Rare Card Specific Effects
- [ ] **MMF_Bloom** - Post-processing bloom
- [ ] **MMF_Light** - Floating point light
- [ ] **MMF_Particles** - Constant particle aura
- [ ] **MMF_MaterialProperty** - Animated shader properties
- [ ] **MMF_VFXGraph** - Custom VFX Graph particles

## Hand Fan Layout Effects
- [ ] **MMF_Position** - Smooth repositioning when hand changes
- [ ] **MMF_Rotation** - Fan rotation per card
- [ ] **MMF_Springs** - Springy hand reorganization

---

## Recommended Testing Order:
1. Start with **Card Draw**: Scale + Position + Sound
2. Add **Card Hover**: Scale + Position + Light
3. Add **Card Select**: Flash + Sound + Events
4. Add **Card Discard**: Scale + Fade + Position + Sound
5. Polish with **Springs** and **Particles**
6. Advanced: VFX Graph and Material animations

## Notes:
- Each MMFeedbacks component should be on the card prefab
- Use `MMFeedbacks.PlayFeedbacks()` to trigger
- Chain effects using Timing/Delay settings
- Test one effect at a time before combining
- Feel demos have great examples to reference
