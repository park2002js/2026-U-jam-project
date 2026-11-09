Sniper Eyebeast lightning is composed from independently rendered geometry.
One bright main trunk and a soft halo are joined by three wrapping currents
and four short offshoots. SniperCurrent.shader generates each ribbon's
cross-section glow; the beam does not sample an image texture.

The endpoint combines a white/purple glow, a small polygon core and sparks.
All full-length currents meet the exact shot endpoints. Independent geometry
updates and intensity pulses make the thinner currents flicker separately.

Visual reference: the user's lightning screenshot supplied on 2026-11-08.
Its main-trunk/side-current composition informed the effect; no screenshot
pixels or third-party artwork are included in the game assets.

The shot holds full intensity for 0.08 seconds and fades by 0.18 seconds.
Endpoint energy fades by 0.4 seconds; the entire effect cleans up at 0.42.
Owner death, attack cancellation or disable removes the visual. The effect
does not apply damage or change the existing instantaneous attack.

SniperEyebeastVfxSetup.ValidateGeometry checks finite paths, exact endpoints,
forward progression, independent wrapping currents, attached short offshoots
and timing, including vertical and zero-length cases.

Earlier LightningBeamV3.png, its generation prompt and textured shader are
retained as historical assets. The current shot does not reference them.
