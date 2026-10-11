namespace Phantom;

public static class FallGuysCollabGuide
{
    public const string FashionCategory = "时装";
    public const string CrownCategory = "胜者王冠";
    public const string MinionCategory = "宠物";
    public const string OrchestrionCategory = "乐谱";
    public const string EmoteCategory = "演技教材";
    public const string PortraitCategory = "肖像教材";
    public const string MiscCategory = "其他";

    public static readonly DateTime EventStart = new(2026, 10, 7, 16, 0, 0);
    public static readonly DateTime EventEnd = new(2026, 10, 27, 22, 59, 0);
    public const string EventTimeText = "2026-10-07 16:00 ~ 2026-10-27 22:59（复刻）";
    public const string WikiUrl = "https://ff14.huijiwiki.com/wiki/%E7%B3%96%E8%B1%86%E4%BA%BA";

    public static readonly IReadOnlyList<CollabNpc> Npcs =
    [
        new("levina", "莱维娜", "金碟游乐场", 4.8f, 6.1f, "任务起始（抓紧胜利的王冠！，需先完成「前往游乐场」）"),
        new("fair-clerk", "金碟声誉兑换员", "金碟巨豆中心广场", 3.8f, 3.1f, "用金碟声誉兑换奖励"),
        new("program-registrar", "节目登记员", "金碟巨豆中心广场", 3.2f, 3.1f, "报名参加金碟巨豆中心节目"),
    ];

    public static readonly IReadOnlyList<CollabRewardDefinition> Rewards =
    [
        new("outfit-cap", "糖豆人针织帽", FashionCategory, [41560], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("outfit-hoodie", "糖豆人卫衣", FashionCategory, [41561], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("outfit-leggings", "糖豆人打底裤", FashionCategory, [41562], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("outfit-shoes", "糖豆人运动鞋", FashionCategory, [41563], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("crown-matte", "哑光胜者王冠", CrownCategory, [41564], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("crown-shiny", "闪亮胜者王冠", CrownCategory, [41796], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("minion-pink", "粉红糖豆人", MinionCategory, [41479], Note: "金碟声誉×350（金碟声誉兑换员）"),
        new("minion-penguin", "糖豆企鹅", MinionCategory, [41480], Note: "金碟声誉×350（金碟声誉兑换员）"),
        new("orch-fallguys", "管弦乐琴乐谱：快快落落糖豆人", OrchestrionCategory, [41628], Note: "金碟声誉×220（金碟声誉兑换员）"),
        new("emote-victory", "演技教材·大获全胜", EmoteCategory, [41463], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("portrait-1", "肖像教材：金碟巨豆中心1", PortraitCategory, [41377], Note: "金碟声誉×200（金碟声誉兑换员）"),
        new("portrait-2", "肖像教材：金碟巨豆中心2", PortraitCategory, [41378], Note: "金碟声誉×200（金碟声誉兑换员）"),
        new("portrait-3", "肖像教材：金碟巨豆中心3", PortraitCategory, [41379], Note: "金碟声誉×200（金碟声誉兑换员）"),
        new("misc-key", "糖豆犀牛认证密钥", MiscCategory, [41491], Note: "金碟声誉×410（金碟声誉兑换员）"),
        new("misc-ticket", "金碟游乐场传送网使用券", MiscCategory, [41708], Note: "金碟声誉×200（金碟声誉兑换员）"),
        new("misc-dye", "追加染剂1", MiscCategory, [52255], Note: "金碟声誉×150（金碟声誉兑换员）"),
    ];

    public static readonly IReadOnlyList<CollabAchievementDefinition> Achievements =
    [
        new("achievement-champion-1", "勇夺桂冠1", 3404, "在金碟巨豆中心获得第1名"),
        new("achievement-champion-2", "勇夺桂冠2", 3405, "在金碟巨豆中心获得第1名（更高次数）"),
        new("achievement-champion-3", "勇夺桂冠3", 3406, "在金碟巨豆中心获得第1名（更高次数）"),
        new("achievement-champion-4", "勇夺桂冠4（称号：糖豆王）", 3407, "在金碟巨豆中心获得第1名100次"),
    ];

    public static bool IsEventActive(DateTime time)
        => time >= EventStart && time <= EventEnd;
}
