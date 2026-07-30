namespace Phantom;

public sealed record PhantomWeaponStage(
    string Key,
    string Name,
    string ItemLevel,
    string Quest,
    string Summary,
    IReadOnlyList<PhantomWeaponRequirement> Requirements,
    IReadOnlyList<PhantomWeaponTask> Tasks,
    IReadOnlyList<PhantomWeaponReward> RepeatableRewards,
    IReadOnlyList<string> Notes);

public sealed record PhantomWeaponRequirement(
    string Key,
    string Name,
    int Needed,
    string Source);

public sealed record PhantomWeaponTask(
    string Key,
    string Name,
    string Detail);

public sealed record PhantomWeaponReward(
    string Activity,
    string Reward);

public sealed record PhantomWeaponTarget(
    string Key,
    string Zone,
    string Name,
    uint TerritoryType,
    float MapX,
    float MapY);

public static class PhantomWeaponGuide
{
    public static readonly IReadOnlyList<PhantomWeaponTarget> SecretTargets = new[]
    {
        new PhantomWeaponTarget("secret-okp-badger", "奥阔帕恰山", "图拉尔蜜獾", 1187, 30.34f, 15.73f),
        new PhantomWeaponTarget("secret-okp-agave", "奥阔帕恰山", "巨龙舌兰", 1187, 20.7f, 14.3f),
        new PhantomWeaponTarget("secret-okp-lizard", "奥阔帕恰山", "巨颚蜥", 1187, 25.42f, 22.05f),
        new PhantomWeaponTarget("secret-okp-carver", "奥阔帕恰山", "其瓦固雕工", 1187, 21.42f, 35.09f),

        new PhantomWeaponTarget("secret-koz-uruq", "克扎玛乌卡湿地", "呜噜怪", 1188, 16.46f, 5.97f),
        new PhantomWeaponTarget("secret-koz-ocelot", "克扎玛乌卡湿地", "豹猫", 1188, 35.54f, 13.66f),
        new PhantomWeaponTarget("secret-koz-wasp", "克扎玛乌卡湿地", "纸巢胡蜂", 1188, 35.74f, 35.73f),
        new PhantomWeaponTarget("secret-koz-apollyon", "克扎玛乌卡湿地", "小亚波伦", 1188, 9.34f, 21.93f),

        new PhantomWeaponTarget("secret-yak-panther", "亚克特尔树海", "长牙狞豹", 1189, 13.06f, 8.97f),
        new PhantomWeaponTarget("secret-yak-wing", "亚克特尔树海", "土石之翼", 1189, 34.74f, 13.77f),
        new PhantomWeaponTarget("secret-yak-branch", "亚克特尔树海", "拟鸟枝", 1189, 15.42f, 24.10f),
        new PhantomWeaponTarget("secret-yak-leaf", "亚克特尔树海", "蓝叶灵", 1189, 8.22f, 26.65f),

        new PhantomWeaponTarget("secret-sha-crab", "夏劳尼荒野", "风滚蟹", 1190, 33.22f, 29.01f),
        new PhantomWeaponTarget("secret-sha-raptor", "夏劳尼荒野", "角盗龙", 1190, 27.38f, 13.33f),
        new PhantomWeaponTarget("secret-sha-bison", "夏劳尼荒野", "犎牛", 1190, 22.34f, 10.97f),
        new PhantomWeaponTarget("secret-sha-cactus", "夏劳尼荒野", "圆扇刺", 1190, 16.42f, 16.89f),

        new PhantomWeaponTarget("secret-her-fang", "遗产之地", "引导之牙", 1191, 27.66f, 26.17f),
        new PhantomWeaponTarget("secret-her-catoblepas", "遗产之地", "卡托布莱普塔", 1191, 35.42f, 12.01f),
        new PhantomWeaponTarget("secret-her-mastodon", "遗产之地", "嵌齿象", 1191, 13.94f, 16.13f),
        new PhantomWeaponTarget("secret-her-beast", "遗产之地", "鬃背兽", 1191, 17.58f, 33.89f),

        new PhantomWeaponTarget("secret-liv-cat", "活着的记忆", "飞天猫", 1192, 33.22f, 35.33f),
        new PhantomWeaponTarget("secret-liv-scorpion", "活着的记忆", "火绳蝎", 1192, 26.5f, 6.61f),
        new PhantomWeaponTarget("secret-liv-tree", "活着的记忆", "永恒杉树精", 1192, 17.35f, 21.65f),
        new PhantomWeaponTarget("secret-liv-soul", "活着的记忆", "液态灵魂", 1192, 10.38f, 36.57f),
    };

    public static readonly IReadOnlyList<PhantomWeaponStage> Stages = new[]
    {
        new PhantomWeaponStage(
            "penumbra",
            "幻境武器·半影",
            "iLvl 745",
            "幻境中的武器 / 制作幻境武器",
            "首把武器需要先完成半魂晶流程，随后每把消耗 3 个兑换素材。",
            new[]
            {
                new PhantomWeaponRequirement("penumbra-token", "未知物品", 3, "尔密娜 幻境村 (X: 6.9, Y: 7.3)，每个 500 亚拉戈数理神典石。"),
                new PhantomWeaponRequirement("penumbra-shards", "6 种半魂晶", 18, "每种 3 个。南征之章 CE/FATE 或 7.0 地区 FATE 概率获得。"),
            },
            new[]
            {
                new PhantomWeaponTask("penumbra-unlock", "完成新月岛任务线", "完成金曦之遗辉后，在图莱尤拉 (X: 17.1, Y: 11.8) 接取柯坦拉姆最后的冒险。"),
                new PhantomWeaponTask("penumbra-shards-once", "交付 6 种半魂晶", "交给盖罗尔特 幻境村 (X: 6.6, Y: 7.1)。该流程仅需完成一次。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[]
            {
                "南征之章 CE 半魂晶掉率约 20%，FATE 约 5%。",
                "7.0 地区对应：奥阔帕恰山、克扎玛乌卡湿地、亚克特尔树海、夏劳尼荒野、遗产之地、活着的记忆。",
            }),
        new PhantomWeaponStage(
            "umbra",
            "幻境武器·本影",
            "iLvl 760",
            "古代工匠的技术 / 蕴藏属性之力的魔法球 / 强化幻境武器",
            "先交 5 种素材，再为 4 个魔法球各积蓄 10000 以太。",
            new[]
            {
                new PhantomWeaponRequirement("umbra-gil", "金币素材", 1, "戈皮露 图莱尤拉 (X: 13.1, Y: 12.7)，300000 金币。"),
                new PhantomWeaponRequirement("umbra-gem", "双色宝石素材", 1, "拉尔·乌鲁可 亚克特尔树海 (X: 13.8, Y: 12.7)，600 双色宝石。"),
                new PhantomWeaponRequirement("umbra-crafted", "能工巧匠制作素材", 3, "通过制作或市场获取。"),
                new PhantomWeaponRequirement("umbra-token", "未知物品", 3, "每把武器 3 个，每个 500 亚拉戈数理神典石。"),
                new PhantomWeaponRequirement("umbra-aether-dungeon", "拾级迷宫以太", 10000, "每日额外奖励 376，以副本理论通关时间结算。"),
                new PhantomWeaponRequirement("umbra-aether-alliance", "团队任务以太", 10000, "每日额外奖励 101，以副本理论通关时间结算。"),
                new PhantomWeaponRequirement("umbra-aether-trial", "讨伐歼灭战以太", 10000, "每日额外奖励 614，以副本理论通关时间结算。"),
                new PhantomWeaponRequirement("umbra-aether-raid", "大型任务以太", 10000, "每日额外奖励 614，以副本理论通关时间结算。"),
            },
            new[]
            {
                new PhantomWeaponTask("umbra-materials-once", "交付五大材料", "交给盖罗尔特 幻境村 (X: 6.6, Y: 7.1)。该流程仅需完成一次。"),
                new PhantomWeaponTask("umbra-aether-once", "充满四个魔法球", "每个魔法球 10000 以太。放弃任务会清空已积蓄以太。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[] { "魔球盘从上至下对应：拾级迷宫、团队任务、讨伐歼灭战、大型任务。" }),
        new PhantomWeaponStage(
            "darkness",
            "幻境武器·黯影",
            "iLvl 775",
            "寻找最适合的技术 / 寻找最适合的身体 / 新生幻境武器",
            "先交 4 种素材，再用幻境透镜收集 1200 个身体重塑素材。",
            new[]
            {
                new PhantomWeaponRequirement("darkness-gil", "金币素材", 1, "戈皮露 图莱尤拉 (X: 13.1, Y: 12.7)，500000 金币。"),
                new PhantomWeaponRequirement("darkness-crafted", "能工巧匠制作素材", 3, "通过制作或市场获取。"),
                new PhantomWeaponRequirement("darkness-token", "未知物品", 3, "每把武器 3 个，每个 500 亚拉戈数理神典石。"),
                new PhantomWeaponRequirement("darkness-body-100", "百目阶段", 100, "身体重塑素材。"),
                new PhantomWeaponRequirement("darkness-body-200", "格雷姆林宝宝阶段", 200, "身体重塑素材。"),
                new PhantomWeaponRequirement("darkness-body-300", "夺心小魔阶段", 300, "身体重塑素材。"),
                new PhantomWeaponRequirement("darkness-body-600", "梦魔娃娃阶段", 600, "身体重塑素材。"),
            },
            new[]
            {
                new PhantomWeaponTask("darkness-materials-once", "交付四大材料", "交给盖罗尔特 幻境村 (X: 6.6, Y: 7.1)。该流程仅需完成一次。"),
                new PhantomWeaponTask("darkness-body-once", "完成妖异身体重塑", "最终形态为卡洛菲斯提莉玩偶。放弃任务会重置身体进度。"),
            },
            new[]
            {
                new PhantomWeaponReward("随机任务：顶级迷宫 / 满级迷宫", "15"),
                new PhantomWeaponReward("随机任务：练级迷宫", "20"),
                new PhantomWeaponReward("南征之章紧急遭遇战", "5"),
                new PhantomWeaponReward("朝圣交错路 1-10 / 11-20 / 21-30 / 31-40", "10 / 11 / 12 / 13"),
                new PhantomWeaponReward("朝圣交错路 41-50 / 51-60 / 61-70 / 71-100", "15 / 17 / 21 / 26"),
                new PhantomWeaponReward("多变 / 深读 / 异闻 商客奇谭", "14 / 16 / 17"),
                new PhantomWeaponReward("阿卡狄亚登天斗技场 重量级普通", "8"),
                new PhantomWeaponReward("阿卡狄亚零式登天斗技场 重量级 1/2/3/4", "10 / 11 / 11 / 18"),
                new PhantomWeaponReward("格莱杨拉波尔歼殛战", "9"),
                new PhantomWeaponReward("7.0 地区 FATE", "3"),
            },
            Array.Empty<string>()),
        new PhantomWeaponStage(
            "eclipse",
            "幻境武器·蚀影",
            "iLvl 790",
            "切断幻影的方法 / 创造幻境菜刀 / 幻境武器的真容",
            "先交 4 种素材，再通过随机任务或北征之章收集三类驱幻晶。",
            new[]
            {
                new PhantomWeaponRequirement("eclipse-gil", "金币素材", 1, "戈皮露 图莱尤拉 (X: 13.1, Y: 12.7)，500000 金币。"),
                new PhantomWeaponRequirement("eclipse-crafted", "能工巧匠制作素材", 3, "通过制作或市场获取。"),
                new PhantomWeaponRequirement("eclipse-token", "未知物品", 3, "每把武器 3 个，每个 500 亚拉戈数理神典石。"),
                new PhantomWeaponRequirement("eclipse-crystal-dungeon", "拾级迷宫驱幻晶", 13, "随机任务：拾级迷宫。"),
                new PhantomWeaponRequirement("eclipse-crystal-trial", "讨伐歼灭战驱幻晶", 5, "随机任务：讨伐歼灭战。"),
                new PhantomWeaponRequirement("eclipse-crystal-raid", "大型任务驱幻晶", 5, "随机任务：大型任务。"),
            },
            new[]
            {
                new PhantomWeaponTask("eclipse-materials-once", "交付四大材料", "交给盖罗尔特 幻境村 (X: 6.6, Y: 7.1)。该流程仅需完成一次。"),
                new PhantomWeaponTask("eclipse-knife-once", "完成幻境菜刀强化", "北征之章 FATE 给 3 个驱幻晶，CE 给 10 个驱幻晶。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[] { "完成第一把蚀影后，开放幻境武器·黯影哑光复制品领取。" }),
        new PhantomWeaponStage(
            "secret",
            "幻境武器·秘影",
            "iLvl 795",
            "幻境武器的真容 / 幻境武器的最终形态",
            "使用知见水晶积累战斗记忆。每个指定目标击倒 1 体，每张地图还需金牌完成 5 个 FATE。",
            new[]
            {
                new PhantomWeaponRequirement("secret-okp", "奥阔帕恰山记忆", 9, "4 个指定目标 + 5 个金牌 FATE。"),
                new PhantomWeaponRequirement("secret-koz", "克扎玛乌卡湿地记忆", 9, "4 个指定目标 + 5 个金牌 FATE。"),
                new PhantomWeaponRequirement("secret-yak", "亚克特尔树海记忆", 9, "4 个指定目标 + 5 个金牌 FATE。"),
                new PhantomWeaponRequirement("secret-sha", "夏劳尼荒野记忆", 9, "4 个指定目标 + 5 个金牌 FATE。"),
                new PhantomWeaponRequirement("secret-her", "遗产之地记忆", 9, "4 个指定目标 + 5 个金牌 FATE。"),
                new PhantomWeaponRequirement("secret-liv", "活着的记忆记忆", 9, "4 个指定目标 + 5 个金牌 FATE。"),
            },
            new[]
            {
                new PhantomWeaponTask("secret-start", "完成一把蚀影并接取最终阶段", "与盖罗尔特 幻境村 (X: 6.6, Y: 7.1) 对话。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[] { "无需使用接取任务的职业，也无需装备对应幻境武器。放弃任务会重置战斗记忆。" }),
    };
}
