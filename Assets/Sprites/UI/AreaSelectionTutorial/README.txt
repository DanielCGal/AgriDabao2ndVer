AREA SELECTION TUTORIAL - PICTURES
==================================

Put the tutorial screenshots in this folder. Anything dropped in here imports
as a Sprite automatically (Assets/Editor/TutorialPictureImporter.cs), so it can
go straight into a page's Picture slot.

Then open the AreaSelection scene, select AreaSelectionBuilder, and under
"Area Selection Tutorial" drag each picture onto its page:

  District Tutorial Slides   3 pages, shown after the player says YES,
                             before they choose a district.

  Area Tutorial Slides       2 pages, shown the first time they press ENTER
                             on a district, before they drag the box.

The text of every page is editable in the same place.

Pages without a picture show a dark panel where the picture goes, so the
tutorial still works while the screenshots are being made.

If a picture was moved in from another folder rather than dropped in fresh,
run AgriDabao -> Reimport Tutorial Pictures so it picks up the Sprite settings.
