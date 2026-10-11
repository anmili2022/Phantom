namespace Phantom;

public static class MogMogCollabGuide
{
    public const string MountCategory = "坐骑";
    public const string FashionCategory = "时装";
    public const string BodyCategory = "身体防具";
    public const string HeadCategory = "头部防具";
    public const string OrchestrionCategory = "管弦乐琴乐谱";
    public const string TripleTriadCategory = "九宫幻卡";
    public const string MinionCategory = "宠物";
    public const string MiscCategory = "杂货";
    public const string OtherCategory = "其他";

    public const string Group7X = "7.x";
    public const string Group6X = "6.x";
    public const string Group5X = "5.x";

    public static readonly DateTime EventStart = new(2026, 9, 9, 16, 0, 0);
    public static readonly DateTime EventEnd = new(2026, 10, 19, 22, 59, 0);
    public const string EventTimeText = "2026-09-09 16:00 ~ 2026-10-19 22:59（天文的行路 第1阶段）";
    public const string WikiUrl = "https://ff14.huijiwiki.com/wiki/%E8%8E%AB%E5%8F%A4%E8%8E%AB%E5%8F%A4%E2%98%85%E5%A4%A7%E6%94%B6%E9%9B%86";

    public static readonly IReadOnlyList<CollabNpc> Npcs =
    [
        new("moogle-lominsa", "旅行莫古力", "利姆萨·罗敏萨下层甲板", 9.4f, 11.6f, "兑换奖励"),
        new("moogle-gridania", "旅行莫古力", "格里达尼亚新街", 12.4f, 12.1f, "兑换奖励"),
        new("moogle-uldah", "旅行莫古力", "乌尔达哈现世回廊", 9.6f, 9.1f, "兑换奖励"),
    ];

    public static readonly IReadOnlyList<CollabRewardDefinition> RareRewards =
    [
        new("mount-woolback", "呜噜怪角笛", MountCategory, [50440], MountName: "呜噜怪", Note: "用「呜噜怪角笛的交换票据」兑换（旅行莫古力）"),
        new("fashion-dogscarf", "豆柴围巾", FashionCategory, [24615], Note: "用「稀少神典石：天文1章」兑换（旅行莫古力）"),
    ];

    public static readonly IReadOnlyList<CollabRewardDefinition> ExchangeRewards =
    [
        new("ex-ram-crystal", "拉姆水晶", OtherCategory, [30107], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-orch-shadow", "管弦乐琴乐谱：传说之兽　～暗影～", OrchestrionCategory, [33117], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-skyworker-vest", "天穹工作背心", BodyCategory, [32794], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-craft-apron", "手艺围裙装", BodyCategory, [30052], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-toad-head", "蟾蜍头套", HeadCategory, [27936], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-toad-body", "蟾蜍服", BodyCategory, [27937], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-emote-explain", "演技教材·说明", MiscCategory, [23367], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-minion-limited", "有限的美德", MinionCategory, [24635], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-chocobo-barding", "崇山陆行鸟半装甲", OtherCategory, [12077], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-auk-whistle", "青君神威魔笛", OtherCategory, [24623], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-orch-kupo", "管弦乐琴乐谱：爱是库啵？", OrchestrionCategory, [33847], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-card-ananta", "九宫幻卡：阿难陀族", TripleTriadCategory, [20242], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-card-kotokaze", "九宫幻卡：琴风", TripleTriadCategory, [20244], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-card-fat-chocobo", "九宫幻卡：胖陆行鸟", TripleTriadCategory, [12273], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-hairstyle-long", "发型样式：长发", MiscCategory, [16703], Note: "旅行莫古力兑换（稀少神典石）"),
        new("ex-minion-doma-goldfish", "多玛金鱼儿", MinionCategory, [28624], Note: "旅行莫古力兑换（稀少神典石）"),
    ];

    public static readonly IReadOnlyList<CollabRewardDefinition> Rewards =
        [.. RareRewards, .. ExchangeRewards];

    public static readonly IReadOnlyList<string> RareCategories = [MountCategory, FashionCategory];

    public static readonly IReadOnlyList<string> ExchangeCategories =
        [BodyCategory, HeadCategory, OrchestrionCategory, TripleTriadCategory, MinionCategory, MiscCategory, OtherCategory];

    public static readonly IReadOnlyList<CollabAchievementDefinition> Achievements = [];

    public static readonly IReadOnlyList<MogMogRareItem> RareItems =
    [
        // 7.x
        new("rare-7x-donut-earring", "甜甜圈耳坠", Group7X, 41503, "黄金的魔典 · 主要兑换内容"),
        new("rare-7x-water-ring", "水蛇戒指", Group7X, 33650, "黄金的魔典 · 其他稀有内容"),
        new("rare-7x-koala-earring", "猴面雀耳坠", Group7X, 17495, "与幻想邂逅 / 奇谭探求者 · 主要兑换内容"),
        new("rare-7x-ifrit-jacket", "火神夹克衫", Group7X, 17470, "与幻想邂逅 · 其他稀有内容"),
        new("rare-7x-falcon-key", "飞隼号启动钥匙", Group7X, 20548, "与幻想邂逅 / 奇谭探求者 · 其他稀有内容", MountName: "飞隼号"),
        new("rare-7x-mini-hraesvelgr", "迷你赫拉斯瓦尔格", Group7X, 17528, "奇谭探求者 · 其他稀有内容"),
        new("rare-7x-coffee-earring", "咖啡杯耳坠", Group7X, 41506, "默示的路标 · 主要兑换内容"),
        new("rare-7x-demon-brick-earring", "恶魔砖块耳坠", Group7X, 13329, "默示的路标 / 雾中经典 · 其他稀有内容"),
        new("rare-7x-koala-bird-earring", "巧儿鸟耳坠", Group7X, 15936, "默示的路标 / 雾中经典 · 其他稀有内容"),
        new("rare-7x-daoist-moogle-doll", "道士莫古力娃娃", Group7X, 15449, "默示的路标 · 其他稀有内容"),
        new("rare-7x-bun-earring", "包子耳坠", Group7X, 41505, "雾中经典 · 主要兑换内容"),
        new("rare-7x-miner-earring", "采矿工耳坠", Group7X, 47938, "天文的行路 第1阶段 · 主要兑换内容"),
        new("rare-7x-fat-cat-parasol", "猫小胖阳伞", Group7X, 33712, "天文的行路 第1阶段 · 其他稀有内容"),
        // 6.x
        new("rare-6x-dog-earring", "豆柴耳饰", Group6X, 24614, "圣典的继承者 · 主要兑换内容"),
        new("rare-6x-catfish-necklace", "鲶鱼领巾", Group6X, 24613, "回归的真理 / 万物的记忆 · 主要兑换内容"),
        new("rare-6x-fantasy-earring", "仙子猪耳坠", Group6X, 39328, "虚构之刻 · 主要兑换内容"),
        new("rare-6x-10th-portrait", "肖像教材：10周年纪念", Group6X, 40501, "外典 / 创世新纪元 第1·2阶段 · 主要/其他"),
        new("rare-6x-emerald-earring", "绿宝石兽耳坠", Group6X, 21033, "创世新纪元 第2阶段 · 主要兑换内容"),
        // 5.x
        new("rare-5x-catfish-earring", "鲶鱼耳饰", Group5X, 24612, "另一种禁书 · 主要兑换内容"),
        new("rare-5x-daoist-moogle-doll", "道士莫古力娃娃", Group5X, 15449, "FANFEST2021 · 其他稀有内容"),
        new("rare-5x-oriental-warrior-hat", "东方武人帽", Group5X, 21036, "国服末段 · 其他稀有内容"),
        new("rare-5x-oriental-warrior-top", "东方武人服", Group5X, 21037, "国服末段 · 其他稀有内容"),
        new("rare-5x-oriental-warrior-boots", "东方武人靴", Group5X, 21038, "国服末段 · 其他稀有内容"),
        new("rare-5x-oriental-lady-hair", "东方佳人发饰", Group5X, 21039, "国服末段 · 其他稀有内容"),
        new("rare-5x-oriental-lady-top", "东方佳人服", Group5X, 21040, "国服末段 · 其他稀有内容"),
        new("rare-5x-oriental-lady-boots", "东方佳人靴", Group5X, 21041, "国服末段 · 其他稀有内容"),
    ];

    public static readonly IReadOnlyList<string> RareGroups = [Group7X, Group6X, Group5X];

    public static readonly IReadOnlyList<MogMogGuidePhase> GuidePhases =
    [
        new(
            "天文的行路 第1阶段",
            "2026-09-09 ~ 2026-10-19",
            [
                "每周小莫古挑战（完成前4周：稀少神典石：天文1章 ×10 + 呜噜怪角笛的交换票据 ×1；完成后2周：×30 + 票据 ×1）：",
                "  第1周：天佑女王 71级 完成5次南方博兹雅战线的冲突战 / 钓鱼 50级 钓上高声鲶鱼",
                "  第2周：天佑女王 完成南方博兹雅战线的紧急遭遇战1次 / 寻宝 36级 解读任意陈旧的地图并开启宝箱",
                "  第3周：天佑女王 完成5次南方博兹雅战线的冲突战 / 多玛方城战 15级 进行1次玩家对战（非人机）",
                "  第4周：天佑女王 完成南方博兹雅战线的紧急遭遇战1次 / 九宫幻卡 15级 以九宫幻卡战胜无所事事的帝国兵（魔大陆阿济兹拉 28.1,31.2）",
                "  第5周：天佑女王 完成南方博兹雅战线「帝国湖岸堡攻城战」1次 / 深层迷宫 1级 突破死者宫殿任意层4次",
                "  第6周：天佑女王 完成1次女王古殿（零式不计） / 特殊迷宫探索 90级 完成4次多变迷宫 六根山",
                "莫古莫古挑战：特殊迷宫探索 90级 完成6次多变迷宫 阿罗阿罗岛 → 稀少神典石：天文1章 ×70 + 呜噜怪角笛的交换票据 ×5",
            ]),
        new(
            "天文的行路 第2阶段",
            "（后续阶段）",
            [
                "每周小莫古挑战（完成前4周：稀少神典石：天文2章 ×10 + 呜噜怪角笛的交换票据 ×1；完成后2周：×30 + 票据 ×1）：",
                "  第1周：天佑女王 71级 完成5次扎杜诺尔高原的冲突战 / 大国防联军 20级 筹备任务递交5次物品",
                "  第2周：天佑女王 完成扎杜诺尔高原的紧急遭遇战1次 / 陆行鸟竞赛 15级 参加3次陆行鸟竞赛",
                "  第3周：天佑女王 完成5次扎杜诺尔高原的冲突战 / 天书奇谈 60级 收集3个印花",
                "  第4周：天佑女王 完成扎杜诺尔高原的紧急遭遇战1次 / 老主顾交易 60级 完成任意6次老主顾交易",
                "  第5周：天佑女王 完成1次女王古殿（零式不计） / 深层迷宫 91级 突破朝圣交错路任意层4次",
                "  第6周：天佑女王 完成扎杜诺尔高原「旗舰达尔里阿达号攻略战」1次 / 特殊迷宫探索 90级 完成4次多变迷宫 商客奇谭",
                "莫古莫古挑战：寻宝 1级 挑战5次通过寻宝传送魔纹进入的宝物库 → 稀少神典石：天文2章 ×50 + 呜噜怪角笛的交换票据 ×5",
            ]),
    ];

    public static bool IsEventActive(DateTime time)
        => time >= EventStart && time <= EventEnd;
}

public sealed record MogMogRareItem(
    string Key,
    string Name,
    string Group,
    uint ItemId,
    string? Note = null,
    string? MountName = null);

public sealed record MogMogGuidePhase(
    string Title,
    string TimeText,
    IReadOnlyList<string> Lines);
