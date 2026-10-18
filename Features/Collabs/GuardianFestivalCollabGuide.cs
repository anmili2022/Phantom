namespace Phantom;

public static class GuardianFestivalCollabGuide
{
    public static readonly DateTime EventStart = new(2026, 10, 15, 16, 0, 0);
    public static readonly DateTime EventEnd = new(2026, 11, 2, 22, 59, 0);
    public const string EventTimeText = "2026-10-15 16:00 ~ 2026-11-02 22:59";
    public const string WikiUrl = "https://ff14.huijiwiki.com/wiki/%E5%AE%88%E6%8A%A4%E5%A4%A9%E8%8A%82_2026";
    public const string Summary = "守护天节活动期间，在各大主城与活动 NPC 互动完成任务，可获得节日限定奖励。";

    public const string FashionCategory = "外观";
    public const string FurnitureCategory = "家具";
    public const string MiscCategory = "其他";

    public static readonly IReadOnlyList<CollabNpc> Npcs =
    [
        new("guardian-quest-npc", "南瓜头女人", "格里达尼亚旧街", 10.2f, 9.4f, "接取活动任务「守护天节与奇妙的人偶们」（全职业 15 级）。"),
        new("guardian-shop-npc", "守护天节的店员", "格里达尼亚旧街", 10.3f, 9.0f, "季节商铺，可用金币兑换往期守护天节道具。"),
    ];

    public static readonly IReadOnlyList<CollabRewardDefinition> Rewards =
    [
        new("guardian-bear-head", "狰狞熊头套", FashionCategory, [52402], Note: "任务奖励（头部防具）。"),
        new("guardian-bear-body", "狰狞熊服", FashionCategory, [52403], Note: "任务奖励（身体防具）。"),
        new("guardian-mushroom-glare", "瞪眼守夜菇", FurnitureCategory, [51293], Note: "商店兑换 5000 金币。"),
        new("guardian-mushroom-laugh", "狂笑守夜菇", FurnitureCategory, [51292], Note: "商店兑换 5000 金币。"),
        new("guardian-ghost-cart", "守夜鬼魂小车", FurnitureCategory, [51294], Note: "商店兑换 8000 金币。"),
        new("guardian-poster-2017", "守护天节海报2017", FurnitureCategory, [51304], Note: "商店兑换 2000 金币。"),
        new("guardian-poster-2025", "守护天节海报2025-2", FurnitureCategory, [51302], Note: "商店兑换 2000 金币。"),
        new("guardian-poster-desk-2025", "守护天节桌面海报2025-2", FurnitureCategory, [51303], Note: "商店兑换 3000 金币。"),
    ];

    public static readonly IReadOnlyList<CollabAchievementDefinition> Achievements =
    [
        new("achievement-guardian-hide-seek", "找呀找呀找朋友 10", 3945, "完成任务「守护天节与奇妙的捉迷藏」。"),
    ];

    public static bool IsEventActive(DateTime time)
        => time >= EventStart && time <= EventEnd;
}
