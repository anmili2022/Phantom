namespace Phantom;

public static class YokaiWatchGuide
{
    public const string WatchCategory = "手表";
    public const string MountCategory = "坐骑";
    public const string PortraitCategory = "肖像教材";
    public const string MinionCategory = "宠物";
    public const string WeaponCategory = "武器";

    public static readonly IReadOnlyList<YokaiRewardDefinition> Rewards =
    [
        new("watch", "妖怪手表", WatchCategory, "妖怪手表"),
        new("mount-vespa", "维斯帕号", MountCategory, "维斯帕号钥匙"),
        new("mount-vespa-advance", "维斯帕前进号", MountCategory, "维斯帕前进号钥匙"),
        new("portrait", "肖像教材：妖怪手表", PortraitCategory, "肖像教材：妖怪手表"),

        new("minion-jibanyan", "地缚猫", MinionCategory, "地缚猫"),
        new("minion-komajiro", "狛次郎", MinionCategory, "狛次郎"),
        new("minion-komasan", "小狛", MinionCategory, "小狛"),
        new("minion-whisper", "维斯帕", MinionCategory, "维斯帕"),
        new("minion-blizzaria", "吹雪公主", MinionCategory, "吹雪公主"),
        new("minion-kyubi", "九尾", MinionCategory, "九尾"),
        new("minion-manjimutt", "人面犬", MinionCategory, "人面犬"),
        new("minion-nokojima", "野槌蛇", MinionCategory, "野槌蛇"),
        new("minion-orochi", "大蛇", MinionCategory, "大蛇"),
        new("minion-nurarihyon", "滑头鬼", MinionCategory, "滑头鬼"),
        new("minion-usa-pyon", "USA蹦", MinionCategory, "USA蹦"),
        new("minion-shogunyan", "武士猫", MinionCategory, "武士猫"),
        new("minion-hovernyan", "浮游猫", MinionCategory, "浮游猫"),
        new("minion-enma", "阎魔", MinionCategory, "阎魔"),

        new("weapon-shogunyan", "妖刀·猫丸 / 圆阵猫盾", WeaponCategory, "妖刀·猫丸", ["圆阵猫盾"]),
        new("weapon-jibanyan", "百斩斧·赤猫", WeaponCategory, "百斩斧·赤猫"),
        new("weapon-hovernyan", "同田贯·冬猫", WeaponCategory, "同田贯·冬猫"),
        new("weapon-enma", "阎魔枪刃", WeaponCategory, "阎魔枪刃"),
        new("weapon-komasan", "白犬杖", WeaponCategory, "白犬杖"),
        new("weapon-komajiro", "朱犬之书", WeaponCategory, "朱犬之书"),
        new("weapon-nokojima", "幸运天球仪", WeaponCategory, "幸运天球仪"),
        new("weapon-usa-pyon", "宇宙拳套", WeaponCategory, "宇宙拳套"),
        new("weapon-orochi", "蛇枪·鸦丸", WeaponCategory, "蛇枪·鸦丸"),
        new("weapon-kyubi", "九尾双剑", WeaponCategory, "九尾双剑"),
        new("weapon-nurarihyon", "怪刀·浮世丸", WeaponCategory, "怪刀·浮世丸"),
        new("weapon-whisper", "智者大弓", WeaponCategory, "智者大弓"),
        new("weapon-robot-cat", "F型波动炮", WeaponCategory, "F型波动炮"),
        new("weapon-oni-princess", "百鬼圆阵", WeaponCategory, "百鬼圆阵"),
        new("weapon-blizzaria", "雪姬杖", WeaponCategory, "雪姬杖"),
        new("weapon-manji", "凭依教典", WeaponCategory, "凭依教典"),
        new("weapon-kaira", "蛇王刺剑", WeaponCategory, "蛇王刺剑"),
    ];

    public static string? GetWeaponMinionName(string weaponKey)
        => weaponKey switch
        {
            "weapon-shogunyan" => "武士猫",
            "weapon-jibanyan" => "地缚猫",
            "weapon-hovernyan" => "浮游猫",
            "weapon-enma" => "阎魔",
            "weapon-komasan" => "小狛",
            "weapon-komajiro" => "狛次郎",
            "weapon-nokojima" => "野槌蛇",
            "weapon-usa-pyon" => "USA蹦",
            "weapon-orochi" => "大蛇",
            "weapon-kyubi" => "九尾",
            "weapon-nurarihyon" => "滑头鬼",
            "weapon-whisper" => "维斯帕",
            "weapon-blizzaria" => "吹雪公主",
            "weapon-manji" => "人面犬",
            _ => null,
        };
}
