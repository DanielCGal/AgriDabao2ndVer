using System;

namespace AgriDabao3D
{
    public static class DistrictDescriptions
    {
        private const string SeedRule =
            "You begin with planting material for three of these crops, three of "
            + "each - always one that goes straight into the ground and one that "
            + "starts in the Seedling Tent. These crops are also the only ones a farm "
            + "here can grow: the shop sells only their seeds, suckers and seedlings, "
            + "and no other planting material can be bought on the marketplace or "
            + "taken in a trade. The rest still appear in the shop, greyed out, so "
            + "you can read about them.";

        public static string For(string districtName)
        {
            if (string.IsNullOrWhiteSpace(districtName))
                return string.Empty;

            string name = districtName.Trim();

            if (name.Equals("Calinan", StringComparison.OrdinalIgnoreCase))
                return
                    "An inland district northwest of the city centre, and one of "
                    + "Davao's main farming belts. The Malagos area inside it is "
                    + "known for cacao and chocolate, and it is also where the "
                    + "Philippine Eagle Center sits. Long-established fruit farms "
                    + "here grow durian, pomelo and mango.\n\n"
                    + "TERRAIN\n"
                    + "Rolling upland rather than flat ground, a few hundred metres "
                    + "above sea level, with gentle to moderate slopes. It drains "
                    + "well, so waterlogging is less of a worry here than on the "
                    + "coast.\n\n"
                    + "STARTER CROPS\n"
                    + "Pineapple, pomelo, mango, durian, banana, coconut, cacao and "
                    + "mangosteen. This is the widest pool of any district, because "
                    + "Calinan is the fruit belt and grows nearly everything the "
                    + "uplands allow. " + SeedRule;

            if (name.Equals("Toril", StringComparison.OrdinalIgnoreCase))
                return
                    "A large southwestern district running from the shoreline "
                    + "inland toward the foothills of Mount Apo. Farming here is "
                    + "split between the coastal flats and the slopes behind them, "
                    + "and it has long been associated with coconut and banana "
                    + "plantations.\n\n"
                    + "TERRAIN\n"
                    + "Flat near the sea, rising to moderate slopes further inland. "
                    + "The low ground holds water after heavy rain, so drainage "
                    + "matters more here than in the higher districts.\n\n"
                    + "STARTER CROPS\n"
                    + "Coconut, banana, cacao, mango, pomelo, durian and pineapple "
                    + "- all tree and plantation crops, which is what this mix of "
                    + "coast and slope supports best. " + SeedRule;

            if (name.Equals("Baguio", StringComparison.OrdinalIgnoreCase))
                return
                    "An upland district in the northwest of Davao City - not to be "
                    + "confused with Baguio City far away in Luzon. It sits toward "
                    + "the Mount Apo range and is cooler and wetter than the "
                    + "lowlands, which is why cacao and mangosteen do well here.\n\n"
                    + "TERRAIN\n"
                    + "Hilly and noticeably higher than the coastal districts, with "
                    + "moderate to steep slopes. Cooler air and steady moisture suit "
                    + "the shade-loving crops.\n\n"
                    + "STARTER CROPS\n"
                    + "Coconut, mangosteen, banana, cacao, durian and corn. This is "
                    + "the first district where a short-cycle crop appears alongside "
                    + "the trees, so it is a good place to learn both rhythms. "
                    + SeedRule;

            if (name.Equals("Paquibato", StringComparison.OrdinalIgnoreCase))
                return
                    "The most remote of Davao City's districts, in the far north "
                    + "and reached by long upland roads. It is almost entirely "
                    + "rural, and farming is what the district lives on rather than "
                    + "one industry among several.\n\n"
                    + "TERRAIN\n"
                    + "Rolling to steep hills - the most broken ground of any "
                    + "district in the game. "
                    + "Slopes shed water quickly, which helps in heavy rain and "
                    + "hurts in a dry spell.\n\n"
                    + "STARTER CROPS\n"
                    + "Corn, banana, coconut and cacao. This is the smallest pool "
                    + "in the game, and deliberately so: the district genuinely "
                    + "grows less variety than the fruit belt. " + SeedRule;

            if (name.Equals("Marilog", StringComparison.OrdinalIgnoreCase))
                return
                    "The highland district in the far northwest, part of the "
                    + "Marilog Forest Reserve and the coolest part of Davao City - "
                    + "well over a thousand metres above sea level in places. It is "
                    + "the ancestral home of the Matigsalug people, and the one part "
                    + "of the city where cool-weather vegetables are grown "
                    + "seriously.\n\n"
                    + "TERRAIN\n"
                    + "High plateau and mountain slope, the steepest and highest of "
                    + "the districts in the game. The altitude is what makes the climate "
                    + "here different from everywhere else on the map.\n\n"
                    + "STARTER CROPS\n"
                    + "Tomato, squash, eggplant, strawberry and mangosteen. Marilog "
                    + "is the only district that grows vegetables and strawberry, "
                    + "and the only one with no coconut, banana or cacao at all - "
                    + "because it is cool enough for crops the lowlands cannot "
                    + "carry. Expect faster harvests and a very different farm. "
                    + SeedRule;

            if (name.Equals("Buhangin", StringComparison.OrdinalIgnoreCase))
                return
                    "A northern district close to the city proper, and the most "
                    + "built-up of the districts in the game. Its name is the Cebuano "
                    + "word for sand. "
                    + "Farming here shares the ground with housing and industry "
                    + "rather than having it to itself.\n\n"
                    + "TERRAIN\n"
                    + "Flat coastal lowland near sea level, with only gentle "
                    + "slopes. Easy ground to work, but low and flat land is slow "
                    + "to drain after a storm.\n\n"
                    + "STARTER CROPS\n"
                    + "Coconut, cacao, banana and corn - hardy lowland staples that "
                    + "tolerate heat and do not need a slope. A small pool, but a "
                    + "forgiving one to start on. " + SeedRule;

            if (name.Equals("Tugbok", StringComparison.OrdinalIgnoreCase))
                return
                    "An inland district between the lowlands of Talomo and Toril "
                    + "and the Calinan uplands. It is a farming district with a "
                    + "research side: the Department of Agriculture's regional field "
                    + "office is here, and a research station in Barangay Manambulan "
                    + "works on mangosteen. Farms in Biao Guianga and Tugbok Proper "
                    + "grow cacao, coconut and banana, and Los Amigos and Tugbok "
                    + "Proper also grow durian, mango and corn.\n\n"
                    + "TERRAIN\n"
                    + "Rolling land that rises gradually toward the uplands, with "
                    + "mostly gentle to moderate slopes. The district gives its name "
                    + "to Tugbok clay, one of the most widespread soils in Davao "
                    + "City.\n\n"
                    + "STARTER CROPS\n"
                    + "Banana, cacao, coconut, mangosteen, corn, durian and mango. "
                    + "One of the widest pools in the game: slow-growing fruit and "
                    + "plantation trees, with corn as the one quick, short-cycle "
                    + "crop. " + SeedRule;

            return string.Empty;
        }
    }
}
