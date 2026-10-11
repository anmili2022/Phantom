namespace Phantom;

public static class FfxvCollabGuide
{
    public const string FashionCategory = "时装";
    public const string MountCategory = "坐骑";
    public const string HairstyleCategory = "发型";
    public const string TripleTriadCategory = "九宫幻卡";
    public const string OrchestrionCategory = "乐谱";

    public static readonly DateTime EventStart = new(2026, 9, 24, 16, 0, 0);
    public static readonly DateTime EventEnd = new(2026, 10, 13, 22, 59, 0);
    public const string EventTimeText = "2026-09-24 16:00 ~ 2026-10-13 22:59";
    public const string WikiUrl = "https://ff14.huijiwiki.com/wiki/%E7%8C%AE%E7%BB%99%E8%8B%B1%E9%9B%84%E7%9A%84%E5%A4%9C%E6%9B%B2";

    public static readonly IReadOnlyList<CollabNpc> Npcs =
    [
        new("noctis-quest", "琪琵·嘉奇亚", "乌尔达哈现世回廊", 8.5f, 9.7f, "任务起始（黑衣青年）"),
        new("garland-investigator", "加隆德炼铁厂调查员", "中萨纳兰", 21.5f, 17.9f, "用未知的魔导部件兑换乐谱"),
        new("garland-clerk", "加隆德炼铁厂销售员", "金碟游乐场", 5.2f, 6.3f, "用金碟币兑换坐骑/发型/幻卡"),
    ];

    public static readonly IReadOnlyList<CollabRewardDefinition> Rewards =
    [
        new("outfit-jacket", "路西斯王子的外套", FashionCategory, [24608], Note: "任务奖励：黑衣青年线"),
        new("outfit-gloves", "路西斯王子的半指手套", FashionCategory, [24609], Note: "任务奖励：黑衣青年线"),
        new("outfit-pants", "路西斯王子的打底裤", FashionCategory, [24610], Note: "任务奖励：黑衣青年线"),
        new("outfit-boots", "路西斯王子的皮靴", FashionCategory, [24611], Note: "任务奖励：黑衣青年线"),
        new("mount-regalia", "雷迦利亚G型", MountCategory, [24624], MountName: "雷迦利亚G型", Note: "金碟币×200000（加隆德炼铁厂销售员）"),
        new("hairstyle-noctis", "诺克提斯", HairstyleCategory, [24802], Note: "金碟币×20000（加隆德炼铁厂销售员）"),
        new("card-noctis", "诺克提斯·路西斯·切拉姆", TripleTriadCategory, [24880], Note: "金碟币×10000（加隆德炼铁厂销售员）"),
        new("orch-hamhead", "锤头鲨", OrchestrionCategory, [25078], Note: "未知的魔导部件×1（加隆德炼铁厂调查员）"),
        new("orch-waltz", "幻想圆舞曲", OrchestrionCategory, [25079], Note: "未知的魔导部件×1（加隆德炼铁厂调查员）"),
        new("orch-rest", "休息与反省", OrchestrionCategory, [25080], Note: "未知的魔导部件×1（加隆德炼铁厂调查员）"),
        new("orch-darkness", "黑暗遮蔽", OrchestrionCategory, [25081], Note: "未知的魔导部件×1（加隆德炼铁厂调查员）"),
        new("orch-revelation", "夜之启示", OrchestrionCategory, [25082], Note: "未知的魔导部件×1（加隆德炼铁厂调查员）"),
        new("orch-pitstop", "快速加油", OrchestrionCategory, [25083], Note: "未知的魔导部件×1（加隆德炼铁厂调查员）"),
    ];

    public static readonly IReadOnlyList<CollabAchievementDefinition> Achievements =
    [
        new("achievement-comrades", "战友（称号：王者之剑）", 2241, "活动任务线完成后解锁"),
    ];

    public static bool IsEventActive(DateTime time)
        => time >= EventStart && time <= EventEnd;
}

public sealed record CollabRewardDefinition(
    string Key,
    string Name,
    string Category,
    IReadOnlyList<uint> ItemIds,
    string? MountName = null,
    string? Note = null);

public sealed record CollabAchievementDefinition(
    string Key,
    string Name,
    uint AchievementId,
    string? Note = null);
