namespace AgriDabao3D
{
    public static class CropFieldDescriptions
    {
        public const string Title = "Crop Information";

        public const string Body =
            "Every line on the crop board, and what it actually does.\n\n"

            + "CROP NAME\n"
            + "The specific plant you tapped, such as mangosteen_1. The number "
            + "tells one plant apart from another of the same kind, so you can be "
            + "sure which one a reading belongs to when several are growing "
            + "together.\n\n"

            + "PLANTED FROM\n"
            + "The planting material this crop grew from, and whether it went "
            + "straight into the ground or was raised in the Seedling Tent first. "
            + "Days in the tent count toward the crop's age, so a transplanted "
            + "seedling is further along than one just sown.\n\n"

            + "TREE STAGE\n"
            + "How far along this plant is: Seedling, Vegetative, PreFruiting, "
            + "Fruiting, then Old. Nothing can be harvested before PreFruiting, "
            + "the harvest is largest at Fruiting, and an Old plant produces less "
            + "than it used to.\n\n"

            + "HEALTH\n"
            + "Condition right now, out of 100. It moves quickly - a storm or a "
            + "pest drops it within days, and treatment brings it back up. Use it "
            + "to see whether what you just did worked.\n\n"

            + "AVG HEALTH\n"
            + "Health averaged over time, and the one that decides your harvest. "
            + "A plant that has been sick for weeks yields poorly even after "
            + "today's reading recovers, which is why the two numbers differ. "
            + "Treat problems early and this stays high.\n\n"

            + "STRESS\n"
            + "Accumulated strain, out of 100, where higher is worse. Typhoons, "
            + "drought, pests, thirst and unsuitable soil all add to it. Along "
            + "with Avg Health it directly reduces how much you harvest, so a "
            + "healthy but stressed plant still disappoints. Mulch, compost and "
            + "pruning bring it down.\n\n"

            + "WATER\n"
            + "How wet the soil is around this plant. Each crop has its own "
            + "comfortable band - coconut tolerates drier ground than banana - and "
            + "both ends hurt: too dry starves it, too wet drowns the roots and "
            + "invites rot. More water is not always better.\n\n"

            + "DRAINAGE\n"
            + "How quickly water leaves this soil. Low drainage means rain sits "
            + "around the roots, which is what starts phytophthora, bacterial wilt "
            + "and the pineapple rots. A drainage kit or a raised bed raises it.\n\n"

            + "FERTILITY\n"
            + "The nutrients in the soil. It falls as the plant feeds and as "
            + "harvests are taken. Organic compost is what raises it, and it can "
            + "be applied again about every twelve days.\n\n"

            + "SOIL SUITABILITY\n"
            + "How well this particular soil suits this particular crop - not soil "
            + "quality in general. The same ground can read high for one crop and "
            + "low for another. A low figure is not a fault you can fix; it means "
            + "this crop is in the wrong place, and something else would do better "
            + "there.\n\n"

            + "SHADE\n"
            + "Young cacao only. Cacao grows up under shade, so until it starts to "
            + "fruit it carries extra stress in open sun. A Shade Net Kit or a "
            + "greenhouse nearby removes it.\n\n"

            + "IN REAL FARMS\n"
            + "How long this step takes on a real farm, from the crop notes the game "
            + "is built on. The game shortens nursery and growing times so a farm "
            + "moves at a pace you can play; this line keeps the real figure in "
            + "view.\n\n"

            + "PEST / DISEASE STATUS\n"
            + "Anything currently attacking the plant, with how far it has "
            + "spread. None means the plant is clear. Severity climbs every day it "
            + "goes untreated, and each problem has its own treatment - check the "
            + "shop description of a treatment to see what it cures.";
    }
}
