namespace Phantom;

public sealed record DeepDungeonWeaponSeries(
    string Key,
    string Name,
    string EnglishName,
    string Version,
    int EntryLevel,
    string Summary,
    string SourceUrl,
    IReadOnlyList<PhantomWeaponProgressStage> Stages,
    IReadOnlyList<PhantomWeaponJob> Jobs)
{
    public string SeriesKey => $"deep-dungeon-{Key}";
}

public static class DeepDungeonWeaponGuide
{
    public static readonly IReadOnlyList<DeepDungeonWeaponSeries> Series = new[]
    {
        CreateSeries(
            "palace",
            "死者宫殿",
            "The Palace of the Dead",
            "3.35",
            1,
            "突破地下50层后，可消耗聚魔装备强化值兑换聚魔柄并带出武器。两个阶段分别按当前实际持有统计。",
            "https://ff14.huijiwiki.com/wiki/%E6%AD%BB%E8%80%85%E5%AE%AB%E6%AE%BF#%E5%B8%A6%E5%87%BA%E8%81%9A%E9%AD%94%E6%AD%A6%E5%99%A8",
            new PhantomWeaponProgressStage("palace-padjali", "角尊", 235, "角尊"),
            new PhantomWeaponProgressStage("palace-kinna", "闪熠", 255, "闪熠")),
        CreateSeries(
            "heaven-on-high",
            "天之御柱",
            "Heaven-on-High",
            "4.35",
            61,
            "使用天之魔器柄兑换带出武器。该迷宫只有一个武器阶段。",
            "https://ff14.huijiwiki.com/wiki/%E5%A4%A9%E4%B9%8B%E5%BE%A1%E6%9F%B1#%E5%B8%A6%E5%87%BA%E8%81%9A%E9%AD%94%E6%AD%A6%E5%99%A8",
            new PhantomWeaponProgressStage("heaven-on-high-empyrean", "天之", 365, "天之")),
        CreateSeries(
            "eureka-orthos",
            "正统优雷卡",
            "Eureka Orthos",
            "6.35",
            81,
            "使用正统聚魔柄兑换带出武器。正统与高正统阶段分别按当前实际持有统计。",
            "https://ff14.huijiwiki.com/wiki/%E6%AD%A3%E7%BB%9F%E4%BC%98%E9%9B%B7%E5%8D%A1#%E5%B8%A6%E5%87%BA%E8%81%9A%E9%AD%94%E6%AD%A6%E5%99%A8",
            new PhantomWeaponProgressStage("eureka-orthos-orthos", "正统", 620, "正统"),
            new PhantomWeaponProgressStage("eureka-orthos-enaretos", "高正统", 625, "高正统")),
        CreateSeries(
            "pilgrims-traverse",
            "朝圣交错路",
            "Pilgrim's Traverse",
            "7.35",
            91,
            "使用光耀聚魔柄兑换带出武器。光耀与圣礼阶段分别按当前实际持有统计。",
            "https://ff14.huijiwiki.com/wiki/%E6%9C%9D%E5%9C%A3%E4%BA%A4%E9%94%99%E8%B7%AF#%E5%B8%A6%E5%87%BA%E8%81%9A%E9%AD%94%E6%AD%A6%E5%99%A8",
            new PhantomWeaponProgressStage("pilgrims-traverse-illuminated", "光耀", 750, "光耀"),
            new PhantomWeaponProgressStage("pilgrims-traverse-ceremonial", "圣礼", 755, "圣礼")),
    };

    public static DeepDungeonWeaponSeries Get(string key)
        => Series.First(series => series.Key == key);

    private static DeepDungeonWeaponSeries CreateSeries(
        string key,
        string name,
        string englishName,
        string version,
        int entryLevel,
        string summary,
        string sourceUrl,
        params PhantomWeaponProgressStage[] stages)
        => new(key, name, englishName, version, entryLevel, summary, sourceUrl, stages, CreateJobs(stages.Length));

    private static IReadOnlyList<PhantomWeaponJob> CreateJobs(int stageCount)
        => new[]
        {
            Job("pld", "骑士", stageCount, 2),
            Job("war", "战士", stageCount),
            Job("drk", "暗黑骑士", stageCount),
            Job("gnb", "绝枪战士", stageCount),
            Job("whm", "白魔法师", stageCount),
            Job("sch", "学者", stageCount),
            Job("ast", "占星术士", stageCount),
            Job("sge", "贤者", stageCount),
            Job("mnk", "武僧", stageCount),
            Job("drg", "龙骑士", stageCount),
            Job("nin", "忍者", stageCount),
            Job("sam", "武士", stageCount),
            Job("rpr", "钐镰客", stageCount),
            Job("vpr", "蝰蛇剑士", stageCount),
            Job("brd", "吟游诗人", stageCount),
            Job("mch", "机工士", stageCount),
            Job("dnc", "舞者", stageCount),
            Job("blm", "黑魔法师", stageCount),
            Job("smn", "召唤师", stageCount),
            Job("rdm", "赤魔法师", stageCount),
            Job("pct", "绘灵法师", stageCount),
        };

    private static PhantomWeaponJob Job(string key, string name, int stageCount, int itemCount = 1)
        => new(
            key,
            name,
            Enumerable.Range(0, stageCount)
                .Select(_ => (IReadOnlyList<string>)Enumerable.Range(0, itemCount).Select(_ => "待采集 Item ID").ToArray())
                .ToArray());
}
