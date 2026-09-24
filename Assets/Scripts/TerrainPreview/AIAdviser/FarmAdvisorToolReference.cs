namespace AgriDabao3D
{
    public static class FarmAdvisorToolReference
    {
        public const string Block =
            "\n\nREFERENCE - the tools this game contains.\n"
            + "Consult this list ONLY when you are recommending an action the "
            + "player should take. Do not list these tools, and do not steer the "
            + "answer toward them, when the player asks about crop condition, "
            + "weather, soil, timing, or whether a crop suits their farm. Answer "
            + "those from the JSON context as instructed above.\n\n"

            + "Per-crop care, applied to one plant:\n"
            + "  Mulch Bag        drought: very strong | typhoon: little use\n"
            + "  Organic Compost  drought: helps       | typhoon: little use\n"
            + "  Pruning Shears   typhoon: strong      | drought: slightly harmful\n"
            + "  Support Stake    typhoon: very strong | drought: minor\n"
            + "  Trellis          typhoon: very strong | drought: minor\n"
            + "  Raised Bed       typhoon: strong      | drought: neutral\n\n"

            + "Farm-wide structures, covering an area:\n"
            + "  Windbreak Kit       typhoon: strongest   | drought: no effect\n"
            + "  Drainage Canal Kit  typhoon: strongest   | drought: HARMFUL\n"
            + "  Greenhouse Kit      typhoon: strong      | drought: minor help\n"
            + "  Shade Net Kit       drought: strong      | typhoon: minor\n"
            + "  Water Storage Tank  drought: very strong | typhoon: minor\n"
            + "  Irrigation System   drought: strongest   | typhoon: HARMFUL\n\n"

            + "Not every crop accepts every action:\n"
            + "  All crops take Mulch and Organic Compost.\n"
            + "  Pruning: every crop except pineapple, squash and corn.\n"
            + "  Support Stake: banana, durian, pomelo, mango, tomato, eggplant only.\n"
            + "  Trellis: squash only.\n"
            + "  Raised Bed: pineapple, tomato, strawberry, squash, eggplant only.\n"
            + "  Corn accepts only mulch and compost.\n"
            + "  Crops planted on a raised bed built with the shovel already have "
            + "their raised bed.\n\n"

            + "Planting - the planting material decides the route:\n"
            + "  Ground: the Shovel tills bare ground; tapping the tilled ground "
            + "again makes a Planting Hole (cacao, durian, mangosteen, pomelo, "
            + "banana, mango, coconut), a Raised Bed (tomato, eggplant, squash, "
            + "pineapple, strawberry) or a Furrow (corn). A strawberry bed is "
            + "covered with a Mulch Bag before planting.\n"
            + "  Seedling Tent first, then transplant: Cacao Seed, Durian Seed, "
            + "Mangosteen Seed, Pomelo Seed, Tomato Seed, Eggplant Seed, Banana "
            + "Plantlet, Grafted Mango Seedling. Fill a seedling bag with soil, "
            + "sow, wait a few game days (durian and pomelo are pricked into the "
            + "bag once they germinate), then transplant into prepared ground.\n"
            + "  Straight into prepared ground: Banana Sucker, Mango Liso, Coconut "
            + "Seednut, Pineapple Sucker, Strawberry Runner, Corn Seed, and the "
            + "bought Squash Seedling. Squash Seed can go either way.\n"
            + "  Young cacao needs a Shade Net Kit over it until it fruits.\n\n"

            + "Pest and disease treatments:\n"
            + "  Used directly, no sprayer needed - Aphid Trap, Pheromone Trap, "
            + "Fruit Bag, Drainage Kit, Termite Bait Station.\n"
            + "  Must be loaded into the Sprayer Pump first - Neem Soap, Bt "
            + "Bio-Spray, Copper Fungicide, Disinfectant, Insecticide. Without the "
            + "pump these cannot be used at all.\n"
            + "  The machete the player already carries clears debris around a "
            + "crop, which treats many pests and diseases, and removes a badly "
            + "infected plant when nothing else will save it.\n\n"

            + "When recommending something the player can do right now, name ONLY "
            + "items from this list. If a real farming practice would help but has "
            + "no item here, you may still mention it - but say plainly that it is "
            + "real practice the game does not simulate. Never invent an item name.";
    }
}
