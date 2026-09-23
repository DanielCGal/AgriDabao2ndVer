AREA SELECTION - DISTRICT MAP ART
=================================

Three pictures per district, all exported on the same canvas size as the
satellite map they sit on top of (the one in the Davao Map Sprite slot of
AreaSelectionBuilder). Different sizes will not line up.

Put them here:

  Districts/                 the two pictures that are DRAWN
    <District>Highlight.png       only this district tinted, whole city visible.
                                  Shown while the player is stepping through
                                  districts with << and >>.
    <District>Barangays.png       this district's barangays drawn in, green where
                                  farming is allowed and red where it is not.
                                  Shown after the player presses Next.

  Districts/AreaSpots/       the picture that is READ, never drawn
    <District>Spot.png            the same green and red barangays as flat shapes
                                  on plain white, no satellite behind them. This
                                  is what Generate checks the player's box
                                  against: all green passes, any red is refused
                                  as non-agricultural, and white counts as being
                                  outside the district.

Import settings are applied automatically by
Assets/Editor/DistrictMapArtPostprocessor.cs, so nothing has to be ticked by
hand. The AreaSpots files are forced to Read/Write Enabled, Point filter and no
compression, because the game reads their pixel colours and compression smears
green into red along every boundary. If you add files while that script is
missing, or change the settings by hand, run AgriDabao -> Reimport District Map
Art from the menu bar.

Colours are recognised by which channel leads, not by an exact swatch, so any
clear green and any clear red will work. The thin blended ring where green meets
red is ignored.

Finally, drop each picture into its district's row on the Area Selection Builder
component in the AreaSelection scene. A district with no art can still be browsed
but says so instead of generating a farm.
