namespace Phantom;

public static class MandervilleWeaponGuide
{
    public static readonly IReadOnlyList<PhantomWeaponProgressStage> ProgressStages = new[]
    {
        new PhantomWeaponProgressStage("manderville-base", "基础", 615, "曼德维尔"),
        new PhantomWeaponProgressStage("manderville-amazing", "惊异", 630, "曼德维尔惊异"),
        new PhantomWeaponProgressStage("manderville-majestic", "威严", 645, "曼德维尔威严"),
        new PhantomWeaponProgressStage("manderville-complete", "盈满", 665, "曼德维尔盈满"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> WeaponJobs = new[]
    {
        new PhantomWeaponJob("pld", "骑士", new[] { new[] { "曼德维尔长剑", "曼德维尔鸢盾" }, new[] { "曼德维尔惊异长剑", "曼德维尔惊异鸢盾" }, new[] { "曼德维尔威严之剑", "曼德维尔威严盾" }, new[] { "曼德维尔盈满弯刃刀", "曼德维尔盈满鸢盾" } }),
        new PhantomWeaponJob("war", "战士", StageItems("曼德维尔战斧", "曼德维尔惊异战斧", "曼德维尔威严巨斧", "曼德维尔盈满战斧")),
        new PhantomWeaponJob("drk", "暗黑骑士", StageItems("曼德维尔双手剑", "曼德维尔惊异双手剑", "曼德维尔威严巨剑", "曼德维尔盈满巨剑")),
        new PhantomWeaponJob("gnb", "绝枪战士", StageItems("曼德维尔枪刃", "曼德维尔惊异枪刃", "曼德维尔威严刺刀", "曼德维尔盈满枪刃")),
        new PhantomWeaponJob("whm", "白魔法师", StageItems("曼德维尔牧杖", "曼德维尔惊异牧杖", "曼德维尔威严幻杖", "曼德维尔盈满牧杖")),
        new PhantomWeaponJob("sch", "学者", StageItems("曼德维尔魔导典", "曼德维尔惊异魔导典", "曼德维尔威严魔导典", "曼德维尔盈满魔导典")),
        new PhantomWeaponJob("ast", "占星术士", StageItems("曼德维尔黄道仪", "曼德维尔惊异黄道仪", "曼德维尔威严太阳仪", "曼德维尔盈满黄道仪")),
        new PhantomWeaponJob("sge", "贤者", StageItems("曼德维尔蛇石针", "曼德维尔惊异蛇石针", "曼德维尔威严飞翼", "曼德维尔盈满飞翼")),
        new PhantomWeaponJob("mnk", "武僧", StageItems("曼德维尔指虎", "曼德维尔惊异指虎", "曼德维尔威严之拳", "曼德维尔盈满之拳")),
        new PhantomWeaponJob("drg", "龙骑士", StageItems("曼德维尔长枪", "曼德维尔惊异长枪", "曼德维尔威严长枪", "曼德维尔盈满三尖枪")),
        new PhantomWeaponJob("nin", "忍者", StageItems("曼德维尔匕首", "曼德维尔惊异匕首", "曼德维尔威严匕首", "曼德维尔盈满匕首")),
        new PhantomWeaponJob("sam", "武士", StageItems("曼德维尔武士刀", "曼德维尔惊异武士刀", "曼德维尔威严武士刀", "曼德维尔盈满武士刀")),
        new PhantomWeaponJob("rpr", "钐镰客", StageItems("曼德维尔镰刀", "曼德维尔惊异镰刀", "曼德维尔威严战镰", "曼德维尔盈满扎戈斧镰")),
        new PhantomWeaponJob("brd", "吟游诗人", StageItems("曼德维尔琴弓", "曼德维尔惊异琴弓", "曼德维尔威严琴弓", "曼德维尔盈满复合弓")),
        new PhantomWeaponJob("mch", "机工士", StageItems("曼德维尔左轮枪", "曼德维尔惊异左轮枪", "曼德维尔威严手枪", "曼德维尔盈满左轮枪")),
        new PhantomWeaponJob("dnc", "舞者", StageItems("曼德维尔圆月轮", "曼德维尔惊异圆月轮", "曼德维尔威严圆月轮", "曼德维尔盈满圆月轮")),
        new PhantomWeaponJob("blm", "黑魔法师", StageItems("曼德维尔法杖", "曼德维尔惊异法杖", "曼德维尔威严咒杖", "曼德维尔盈满法杖")),
        new PhantomWeaponJob("smn", "召唤师", StageItems("曼德维尔魔导书", "曼德维尔惊异魔导书", "曼德维尔威严魔导书", "曼德维尔盈满魔导书")),
        new PhantomWeaponJob("rdm", "赤魔法师", StageItems("曼德维尔刺剑", "曼德维尔惊异刺剑", "曼德维尔威严重刺剑", "曼德维尔盈满刺剑")),
    };

    public static readonly IReadOnlyList<PhantomWeaponStage> Stages = new[]
    {
        new PhantomWeaponStage(
            "manderville-base",
            "曼德维尔武器",
            "iLvl 615",
            "曼德维尔家的古老武器 / 再次制作曼德维尔武器",
            "完成非著名调查员任务线后开启。每把武器需要 3 个稀少陨石，共 1500 个亚拉戈诗学神典石。",
            new[]
            {
                new PhantomWeaponRequirement("manderville-base-meteorite", "稀少陨石", 3, "在拉札罕的玖布伦娜处使用 500 个亚拉戈诗学神典石兑换。每把武器需要 3 个。"),
                new PhantomWeaponRequirement("manderville-base-poetics", "亚拉戈诗学神典石", 1500, "用于兑换 3 个稀少陨石。"),
            },
            new[]
            {
                new PhantomWeaponTask("manderville-base-unlock", "完成开启任务线", "完成任务“逃跑的复制体”后，在拉札罕曼德维尔家的佣人处接取“曼德维尔家的古老武器”。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[] { "首把与后续曼德维尔武器使用相同的 3 个稀少陨石成本。", "资料来源：灰机 Wiki 曼德维尔武器页面。" }),
        new PhantomWeaponStage(
            "manderville-amazing",
            "曼德维尔武器·惊异",
            "iLvl 630",
            "觉醒吧！斗争本能！ / 令人惊异的曼德维尔武器",
            "完成 6.35 任务线后强化。每把武器需要 3 个稀少球粒陨石，共 1500 个亚拉戈诗学神典石。",
            new[]
            {
                new PhantomWeaponRequirement("manderville-amazing-meteorite", "稀少球粒陨石", 3, "在拉札罕的玖布伦娜处使用 500 个亚拉戈诗学神典石兑换。每把武器需要 3 个。"),
                new PhantomWeaponRequirement("manderville-amazing-poetics", "亚拉戈诗学神典石", 1500, "用于兑换 3 个稀少球粒陨石。"),
            },
            new[]
            {
                new PhantomWeaponTask("manderville-amazing-unlock", "完成 6.35 任务线", "完成任务“充满活力的父子”后，在盖罗尔特处接取“觉醒吧！斗争本能！”。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[] { "每把武器单独消耗 3 个稀少球粒陨石。" }),
        new PhantomWeaponStage(
            "manderville-majestic",
            "曼德维尔武器·威严",
            "iLvl 645",
            "工匠们的酒宴 / 灵活变化的曼德维尔武器",
            "完成 6.45 任务线后强化。每把武器需要 3 个稀少无球粒陨石，共 1500 个亚拉戈诗学神典石。",
            new[]
            {
                new PhantomWeaponRequirement("manderville-majestic-meteorite", "稀少无球粒陨石", 3, "在拉札罕的玖布伦娜处使用 500 个亚拉戈诗学神典石兑换。每把武器需要 3 个。"),
                new PhantomWeaponRequirement("manderville-majestic-poetics", "亚拉戈诗学神典石", 1500, "用于兑换 3 个稀少无球粒陨石。"),
            },
            new[]
            {
                new PhantomWeaponTask("manderville-majestic-unlock", "完成 6.45 任务线", "完成任务“不拘小节的一家”后，在盖罗尔特处接取“工匠们的酒宴”。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[] { "稀少无球粒陨石可在曼德维尔家的炼金术士处用于重新调整武器能力。" }),
        new PhantomWeaponStage(
            "manderville-complete",
            "曼德维尔武器·盈满",
            "iLvl 665",
            "起舞的工匠们 / 精美绝伦的曼德维尔武器",
            "完成 6.55 任务线后强化。每把武器需要 3 个雏晶，共 1500 个亚拉戈诗学神典石。",
            new[]
            {
                new PhantomWeaponRequirement("manderville-complete-crystal", "雏晶", 3, "在拉札罕的玖布伦娜处使用 500 个亚拉戈诗学神典石兑换。每把武器需要 3 个。"),
                new PhantomWeaponRequirement("manderville-complete-poetics", "亚拉戈诗学神典石", 1500, "用于兑换 3 个雏晶。"),
            },
            new[]
            {
                new PhantomWeaponTask("manderville-complete-unlock", "完成 6.55 任务线", "完成任务“曼德维尔之谜”后，在盖罗尔特处接取“起舞的工匠们”。"),
            },
            Array.Empty<PhantomWeaponReward>(),
            new[] { "雏晶可在曼德维尔家的炼金术士处用于重新调整武器能力。", "曼德维尔武器共有 19 个战斗职业对应的武器。" }),
    };

    private static IReadOnlyList<IReadOnlyList<string>> StageItems(params string[] names)
        => names.Select(name => (IReadOnlyList<string>)new[] { name }).ToArray();
}
