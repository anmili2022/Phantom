namespace Phantom;

public static class ZodiacGuide
{
    public static readonly IReadOnlyList<ZodiacFateObjective> AtmaTerritories = new[]
    {
        Atma("atma-aries", "白羊之魂晶", "中拉诺西亚"),
        Atma("atma-pisces", "双鱼之魂晶", "拉诺西亚低地"),
        Atma("atma-cancer", "巨蟹之魂晶", "西拉诺西亚"),
        Atma("atma-aquarius", "宝瓶之魂晶", "拉诺西亚高地"),
        Atma("atma-leo", "狮子之魂晶", "拉诺西亚外地"),
        Atma("atma-virgo", "室女之魂晶", "黑衣森林中央林区"),
        Atma("atma-capricorn", "摩羯之魂晶", "黑衣森林东部林区"),
        Atma("atma-sagittarius", "人马之魂晶", "黑衣森林北部林区"),
        Atma("atma-gemini", "双子之魂晶", "西萨纳兰"),
        Atma("atma-libra", "天秤之魂晶", "中萨纳兰"),
        Atma("atma-taurus", "金牛之魂晶", "东萨纳兰"),
        Atma("atma-scorpio", "天蝎之魂晶", "南萨纳兰"),
    };

    public static readonly IReadOnlyList<ZodiacBookGuide> AnimusBooks = new[]
    {
        FireOne(), WaterOne(), WindOne(), FireTwo(), WaterTwo(), WindTwo(),
        FirePrisonOne(), WaterPrisonOne(), EarthOne(),
    };

    private static ZodiacFateObjective Atma(string key, string name, string zone)
        => new(key, name, zone, 0, 0f, 0f, BookKey: null, AnyFateInTerritory: true);

    private static ZodiacBookGuide Book(
        string key,
        string name,
        string[] monsters,
        string[] monsterZones,
        string[] duties,
        string[] fates,
        string[] fateZones,
        string[] leves,
        string[] leveZones,
        IReadOnlyList<ZodiacCoordinate>[]? monsterCoordinates = null)
    {
        var coordinates = monsterCoordinates ?? GetMonsterCoordinates(key);
        var monsterObjectives = monsters.Select((monster, index) =>
            new ZodiacMonsterObjective(
                $"{key}-monster-{index + 1}",
                monster,
                monsterZones[index],
                0,
                coordinates?[index].FirstOrDefault()?.MapX ?? 0f,
                coordinates?[index].FirstOrDefault()?.MapY ?? 0f,
                LocationNotes: coordinates == null || coordinates[index].Count == 0
                    ? "Wiki 已核对；部分目标为巡逻路线或区域范围。"
                    : string.Join("、", coordinates[index].Select(coordinate => coordinate.ToString())),
                Coordinates: coordinates?[index]))
            .ToArray();
        var dutyObjectives = duties.Select((duty, index) =>
            new ZodiacDutyObjective($"{key}-duty-{index + 1}", duty, "副本", 0, BookKey: key, GroupKey: key)).ToArray();
        var fateObjectives = fates.Select((fate, index) =>
            CreateFateObjective(key, index, fate, fateZones[index]))
            .ToArray();
        var leveObjectives = leves.Select((leve, index) =>
            CreateLeveObjective(key, index, leve, leveZones[index]))
            .ToArray();
        return new ZodiacBookGuide(key, name, monsterObjectives, dutyObjectives, fateObjectives, leveObjectives);
    }

    private static ZodiacFateObjective CreateFateObjective(string bookKey, int index, string name, string zone)
    {
        var (mapX, mapY, note, npcName, npcZone, npcX, npcY) = name switch
        {
            "骑兵天敌——妖蛇飞蜥" => (8.6f, 12f, (string?)null, null, null, 0f, 0f),
            "雷雨呼唤者——布朗格" => (25.1f, 17.9f, null, null, null, 0f, 0f),
            "百灵啼持久战" => (27.4f, 21.6f, "需先与千里眼 米安娜对话触发前置危命", "千里眼 米安娜", "黑衣森林东部林区", 28.2f, 20.4f),
            "大口食人魔——加尔加梅勒" => (34.3f, 13.7f, null, null, null, 0f, 0f),
            "异国魔虫——螳螂帝王" => (14.2f, 34.6f, null, null, null, 0f, 0f),
            "矿脉虫巢" => (25.9f, 24.6f, null, null, null, 0f, 0f),
            "守护运输部队" => (26.8f, 18.9f, null, null, null, 0f, 0f),
            "阴险的魔物——憎恨恶石" => (11.5f, 18.2f, null, null, null, 0f, 0f),
            "魔界花盛开的世界" => (13.5f, 12.1f, null, null, null, 0f, 0f),
            "南防波堤之战 全力打击" => (18.9f, 22f, "需先完成侦察行动", null, null, 0f, 0f),
            "第二战斗大队" => (21.2f, 16.7f, null, null, null, 0f, 0f),
            "鸟人军采伐所强攻战" => (19.5f, 19.2f, null, null, null, 0f, 0f),
            "北防波堤之战 全力打击" => (20.6f, 19.1f, "需先完成侦察行动", null, null, 0f, 0f),
            "沼泽林遗恨——恩布鲁" => (15.7f, 14.2f, null, null, null, 0f, 0f),
            "幽谷药师——黎明药达奇希奥" => (32.4f, 14.4f, null, null, null, 0f, 0f),
            "友人与家人" => (18.4f, 19.7f, null, null, null, 0f, 0f),
            "力量之塔" => (10.4f, 28.6f, "需与艾因哈特家的卫兵对话触发", null, null, 0f, 0f),
            "最后的斗猪王——派亚" => (32.1f, 25.4f, null, null, null, 0f, 0f),
            "残忍食人魔——波罗斯" => (31f, 5.1f, null, null, null, 0f, 0f),
            "以毒制毒" => (23.7f, 14.3f, null, null, null, 0f, 0f),
            "命运代言人——灰煤烟佩古基·春" => (24.3f, 26.1f, null, null, null, 0f, 0f),
            "遗迹的亡灵骑士——代达罗斯" => (21.8f, 19.8f, null, null, null, 0f, 0f),
            "烈风勇士——怀罗克四姐妹" => (34.7f, 21.3f, null, null, null, 0f, 0f),
            "雪山袭击者——索贝克" => (4.8f, 21.9f, null, null, null, 0f, 0f),
            "丑恶合成兽——巴杜枭" => (30f, 25.5f, null, null, null, 0f, 0f),
            _ => (0f, 0f, (string?)"地点未核对", null, null, 0f, 0f)
        };

        var locationNotes = note ?? "Wiki 已核对；部分 FATE 需要前置触发。";
        if (npcName != null)
        {
            locationNotes += $" 前置 NPC：{npcName}，{npcZone} ({npcX:0.0}, {npcY:0.0})。";
        }

        return new ZodiacFateObjective($"{bookKey}-fate-{index + 1}", name, zone, 0, mapX, mapY, BookKey: bookKey, LocationNotes: locationNotes, PrerequisiteNpcName: npcName, PrerequisiteNpcZone: npcZone, PrerequisiteNpcMapX: npcX, PrerequisiteNpcMapY: npcY);
    }

    private static ZodiacLeveObjective CreateLeveObjective(string bookKey, int index, string name, string zone)
    {
        var (mapX, mapY, npc) = GetLeveNpc(name, zone);
        var note = npc == null ? "NPC 坐标未核对。" : $"NPC：{npc}。";
        return new ZodiacLeveObjective($"{bookKey}-leve-{index + 1}", name, zone, mapX, mapY, "理符", 40, bookKey, note);
    }

    private static (float MapX, float MapY, string? Npc) GetLeveNpc(string name, string zone)
    {
        if (zone == "北萨纳兰") return (22.1f, 29.4f, "鲁鲁巴纳");
        if (zone == "库尔札斯中央高地")
        {
            return name.Contains("徘徊", StringComparison.Ordinal) || name.Contains("冰霜", StringComparison.Ordinal)
                ? (12.6f, 16.7f, "瓦力诺")
                : (11.9f, 16.8f, "洛蒂耶");
        }

        if (zone == "摩杜纳")
        {
            return name.Contains("验证", StringComparison.Ordinal) || name.Contains("饰品", StringComparison.Ordinal) || name.Contains("尖牙", StringComparison.Ordinal)
                ? (29.8f, 12.5f, "克·蕾塔伊")
                : (30.7f, 12.1f, "艾伊德哈特");
        }

        return (0f, 0f, null);
    }

    private static ZodiacBookGuide FireOne() => Book("fire-1", "火天文书·第一卷",
        ["第二大队剑斗士", "滩棘鱼人", "合成矿妖虫", "乳根花簇", "第四大队剑斗士", "赞拉克格斗家", "石蜥蜴", "伐木巨人", "鲁莽异端者", "第五大队魔导先锋"],
        ["东拉诺西亚", "西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "西萨纳兰", "南萨纳兰", "北萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["地下灵殿塔姆·塔拉墓园", "对龙城塞石卫塔", "腐坏遗迹无限城市街古迹"],
        ["骑兵天敌——妖蛇飞蜥", "雷雨呼唤者——布朗格", "百灵啼持久战"],
        ["库尔札斯中央高地", "拉诺西亚外地", "黑衣森林东部林区"],
        ["焚书任务：回收禁书«异界火焰的怪物»", "索敌指令：潜伏在库尔札斯的通缉犯", "防卫指令：阿格里俄斯号的研究任务"],
        ["北萨纳兰", "库尔札斯中央高地", "摩杜纳"],
        new[]
        {
            Coordinates((25.1f, 21.3f), (27.2f, 21.3f), (28.9f, 21.1f), (31f, 20.1f)),
            Coordinates((16.6f, 17.8f), (16.7f, 16.8f), (17.4f, 16.5f), (16.7f, 15.3f), (17.5f, 15.1f)),
            Coordinates((22.6f, 9f), (21.8f, 9.3f), (23.1f, 7.2f), (22f, 5.5f), (24.9f, 7.7f), (25.6f, 6.6f)),
            Coordinates((24.1f, 16.8f, null), (23.3f, 18.2f, "附近")),
            Coordinates((12.6f, 7.3f), (12.3f, 6.4f), (10.7f, 5.9f), (9f, 5.5f)),
            Coordinates((18.1f, 24.5f), (19.5f, 25.3f), (21f, 25.6f), (23f, 25.6f), (24.7f, 26.2f)),
            Coordinates((22.4f, 26.7f), (20.9f, 26.5f), (21.2f, 24.9f), (23.1f, 23.7f), (24f, 22.6f)),
            Coordinates((13.5f, 25.3f), (14.2f, 26.6f), (15.8f, 28.1f)),
            Coordinates((16.7f, 15.5f), (17.2f, 15.6f), (16.7f, 17.2f)),
            Coordinates((10f, 14f, "帝国中央堡内"))
        });

    private static IReadOnlyList<ZodiacCoordinate> Coordinates(params (float X, float Y, string? Note)[] values)
        => values.Select(value => new ZodiacCoordinate(value.X, value.Y, value.Note)).ToArray();

    private static IReadOnlyList<ZodiacCoordinate> Coordinates(params (float X, float Y)[] values)
        => values.Select(value => new ZodiacCoordinate(value.X, value.Y)).ToArray();

    private static IReadOnlyList<ZodiacCoordinate>[]? GetMonsterCoordinates(string key)
        => key switch
        {
            "water-1" =>
            [
                Coordinates((26.2f, 21.2f), (27.9f, 20.9f), (29.8f, 21.3f), (29.6f, 20.1f), (28.9f, 19.7f), (30.5f, 19.4f)),
                Coordinates((17.2f, 19.8f), (18.2f, 19.7f), (18.3f, 20.8f), (18.4f, 21.9f)),
                Coordinates((14.3f, 16.9f, "附近 7 个单位")),
                Coordinates((22.1f, 8.9f), (23.1f, 7.6f), (22.4f, 7.9f), (22f, 5.9f), (23.7f, 6.4f)),
                Coordinates((24.4f, 11.1f, "附近 3 个单位")),
                Coordinates((19.9f, 19.9f), (18.9f, 19.5f), (21.1f, 20.6f), (18.6f, 23.8f), (16.6f, 25.3f), (21.9f, 21.4f)),
                Coordinates((17.1f, 16.9f), (16.1f, 16.4f), (17.3f, 15.1f), (15.7f, 14.6f)),
                Coordinates((13.3f, 27.1f), (12.5f, 27.8f), (15.7f, 26.6f)),
                Coordinates((14.4f, 10.6f), (12.6f, 10.8f), (17f, 30.8f), (11.2f, 29.7f)),
                Coordinates((24.5f, 12.6f, "沿河畔向东至 27.8, 13.3"))
            ],
            "wind-1" =>
            [
                Coordinates((25.6f, 20.8f), (27.5f, 21.1f), (29.6f, 21.5f)),
                Coordinates((17.6f, 15.8f), (16.2f, 14.8f)),
                Coordinates((20f, 19.8f, "附近 3 个单位")),
                Coordinates((25f, 8.3f), (25f, 5.6f), (27.1f, 5.1f)),
                Coordinates((28.8f, 17.7f), (30.3f, 16.8f), (28.4f, 16.2f), (32.1f, 14.6f), (28.8f, 13.3f)),
                Coordinates((19.1f, 19.6f), (20.2f, 20.1f), (21.1f, 20.6f), (19.3f, 22.4f), (17.4f, 22.2f), (15.9f, 23.7f), (16.7f, 25.4f)),
                Coordinates((24.6f, 21f, "附近")),
                Coordinates((31.7f, 17.5f), (34.5f, 22.3f), (33.8f, 24.7f)),
                Coordinates((11.9f, 12.6f), (10.2f, 14f), (10.5f, 15.6f), (11.7f, 15.8f), (12.8f, 15.6f), (12.7f, 16f), (12.5f, 17f)),
                Coordinates((26.8f, 8.3f, "路线至 28.8, 7.4"), (32.6f, 8.6f, "路线至 32.9, 11.8"))
            ],
            "fire-2" =>
            [
                Coordinates((17.7f, 17.1f), (16.6f, 17f), (16.3f, 16f), (17.4f, 15.8f), (18.5f, 15.6f)),
                Coordinates((13.4f, 16.6f), (12.9f, 16.9f)),
                Coordinates((26.5f, 4.8f), (27.3f, 7.1f)),
                Coordinates((25.6f, 17.7f), (28.8f, 17.7f), (28.8f, 16.7f), (29.6f, 13.3f), (29.8f, 12.4f), (29.5f, 11.4f), (28.8f, 12.2f), (28.1f, 13f), (30.5f, 15.8f), (32.6f, 14.1f)),
                Coordinates((25.1f, 21.3f, "沿小径至 22.1, 20.7")),
                Coordinates((21.3f, 19.5f), (22.3f, 19.4f), (22.3f, 18.7f)),
                Coordinates((23f, 21.7f), (26f, 23f), (26.9f, 21.2f), (29.8f, 20.1f), (31.1f, 18.3f), (32.5f, 19.6f)),
                Coordinates((18.8f, 30.1f, "沿河岸至 7.2, 27.5")),
                Coordinates((32.3f, 18.4f), (33.2f, 19.1f), (34.7f, 21f), (34.6f, 22.2f), (33.5f, 21.4f), (34.7f, 22.9f), (34.3f, 24.1f), (33.5f, 24f), (32.6f, 22.6f)),
                Coordinates((17.2f, 15.7f), (17f, 16.2f), (16.7f, 17.2f))
            ],
            "water-2" =>
            [
                Coordinates((13f, 17.1f), (13.6f, 16.5f)),
                Coordinates((14.5f, 14.3f), (13f, 14.6f)),
                Coordinates((23f, 9.9f), (21.7f, 8.3f), (22.6f, 7f), (22f, 5.9f)),
                Coordinates((26.1f, 13.3f, "附近")),
                Coordinates((32.3f, 24.6f, "向北至 33, 23.7")),
                Coordinates((11.6f, 6.6f), (10.1f, 6.5f), (9f, 6.6f)),
                Coordinates((16.1f, 25.2f), (18.8f, 23.2f), (20.5f, 23.8f)),
                Coordinates((31.7f, 16.9f), (31.5f, 18.1f), (34f, 20.1f), (34.2f, 22.1f), (33f, 20.6f), (34.1f, 23.5f), (32.6f, 23.5f)),
                Coordinates((12.9f, 12.9f), (12f, 12.1f), (10.1f, 12.5f), (10.9f, 15.7f), (12.2f, 16.4f)),
                Coordinates((32.8f, 14.8f), (33.2f, 16f))
            ],
            "wind-2" =>
            [
                Coordinates((15.5f, 14.2f), (15.1f, 15.5f), (13.9f, 14.5f), (14.3f, 13.3f)),
                Coordinates((24.5f, 7.5f), (26.5f, 5.4f), (24.2f, 8.3f)),
                Coordinates((27.7f, 18.4f, "附近 3 个单位")),
                Coordinates((21.4f, 20.7f), (19.5f, 19.9f), (20.2f, 18.7f)),
                Coordinates((11.7f, 7.3f), (10.2f, 6.1f), (9.3f, 6.2f)),
                Coordinates((19.2f, 26f), (20.8f, 23.8f), (20.6f, 22.5f)),
                Coordinates((29.8f, 19.3f), (31.7f, 18.8f)),
                Coordinates((31.6f, 17.3f), (32.5f, 18.2f), (34.3f, 20.5f), (34.5f, 22.1f), (35.6f, 22.3f), (34.1f, 23.1f), (33.2f, 24.9f)),
                Coordinates((28.8f, 12.9f), (30.4f, 14.4f), (31.9f, 12.3f)),
                Coordinates((12.3f, 12.6f), (9.4f, 14.2f), (10.9f, 14.9f), (11.7f, 16.5f))
            ],
            "fire-prison-1" =>
            [
                Coordinates((25.5f, 20.9f), (27.5f, 21f), (30.2f, 21.2f), (30.1f, 19.4f)),
                Coordinates((13.1f, 16.9f, "附近 3 个单位")),
                Coordinates((23.8f, 9.3f), (21.1f, 8.1f), (22.4f, 9.8f), (23.8f, 7.1f), (23f, 5f)),
                Coordinates((27.3f, 18.8f), (31.3f, 15.2f), (32.5f, 14.2f), (28.8f, 13.1f)),
                Coordinates((29.2f, 23.5f, "路线至 32.5, 25.7")),
                Coordinates((21.2f, 19.6f), (22.4f, 18.8f)),
                Coordinates((25.7f, 23.3f), (28.7f, 20.5f), (30.2f, 19.8f), (31.3f, 18.2f), (32.6f, 19.7f)),
                Coordinates((12.3f, 26.7f), (14.3f, 27.2f), (14.9f, 25.8f)),
                Coordinates((16.3f, 15.2f), (17.3f, 17.1f), (16.7f, 17.2f)),
                Coordinates((27.1f, 10f), (29.1f, 14.5f), (31.6f, 13.8f), (28.4f, 12.1f))
            ],
            "water-prison-1" =>
            [
                Coordinates((15.1f, 15.5f), (14.3f, 13.4f), (12.9f, 14.3f)),
                Coordinates((21.7f, 9.3f), (21.9f, 6f), (23.8f, 5.7f)),
                Coordinates((24.2f, 12.1f), (24.8f, 10.6f), (20.8f, 10.6f), (23.8f, 14.7f), (25.3f, 17f)),
                Coordinates((29.2f, 23.5f, "南部林区路线"), (25.1f, 21.3f, "北部林区路线"), (22.1f, 20.7f, "路线终点")),
                Coordinates((20.7f, 20.3f), (18.9f, 19.9f), (20.7f, 18.5f)),
                Coordinates((11.8f, 6.1f), (9.9f, 5.7f), (9.4f, 6.3f)),
                Coordinates((20.5f, 21.6f), (21.8f, 21.7f), (18.8f, 20.1f)),
                Coordinates((16.5f, 31.8f, "沿河岸至 11.6, 30.6")),
                Coordinates((12.2f, 12.4f), (8.6f, 14.1f), (10.6f, 14.8f), (12f, 16f)),
                Coordinates((28.4f, 13.3f), (29.7f, 15.1f), (31f, 13.9f))
            ],
            "earth-1" =>
            [
                Coordinates((25.8f, 21.2f), (27.1f, 20.9f), (28.1f, 20.8f), (28.1f, 21.4f), (29f, 20.3f), (29.5f, 19.8f), (30.4f, 20.5f), (31f, 19.6f)),
                Coordinates((13.9f, 15.5f, "附近")),
                Coordinates((24.7f, 7.1f), (24.4f, 6.4f), (27.1f, 5.5f), (25.9f, 8.4f)),
                Coordinates((24.7f, 10.6f), (24f, 10.2f), (20.8f, 10.4f), (24.1f, 13.5f), (24.2f, 16.6f)),
                Coordinates((21.2f, 20.6f), (20.7f, 19.9f), (19f, 19.7f), (20.8f, 18.6f)),
                Coordinates((21.7f, 21.8f), (20.4f, 21.7f), (18.6f, 20f)),
                Coordinates((23.9f, 21.6f), (25.6f, 23.6f), (26.3f, 20.9f), (28.5f, 20.7f), (31.1f, 18.3f), (32.1f, 19.9f)),
                Coordinates((31.4f, 17.4f), (34.5f, 20.6f), (34.3f, 22.1f), (34.3f, 24.1f), (32.7f, 23.5f)),
                Coordinates((12f, 12.4f), (10.1f, 13f), (10.2f, 14.8f), (12.9f, 16.5f)),
                Coordinates((30.2f, 6.3f, "范围至 32.5, 6.8"))
            ],
            _ => null
        };

    private static ZodiacBookGuide WaterOne() => Book("water-1", "水天文书·第一卷",
        ["第二大队骑士", "礁鳞鱼人", "海蜂水母", "武伽玛罗采石员", "妖精领哨兵", "蜥蜴人枪兵", "魔导先锋", "朗咒巨人", "泥沼蝾螈", "湖畔眼镜蛇"],
        ["东拉诺西亚", "西拉诺西亚", "西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "南萨纳兰", "北萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["封锁坑道铜铃铜山", "山中战线泽梅尔要塞", "纷争要地布雷福洛克斯野营地"],
        ["大口食人魔——加尔加梅勒", "异国魔虫——螳螂帝王", "矿脉虫巢"],
        ["库尔札斯中央高地", "西拉诺西亚", "东萨纳兰"],
        ["巡逻任务：确保补给通道的安全", "焚书任务：回收禁书«黑暗羽翼的怪物»", "防卫指令：魔导机器的残骸"],
        ["北萨纳兰", "库尔札斯中央高地", "摩杜纳"]);

    private static ZodiacBookGuide WindOne() => Book("wind-1", "风天文书·第一卷",
        ["第二大队拳斗士", "萨普沙水龙蜥", "壕齿鱼人", "精英巡逻员", "咆哮之森精", "蜥蜴人咒术法师", "冥鬼之眼", "风扬风爪剑士", "第五大队骑士", "骏鹰"],
        ["东拉诺西亚", "西拉诺西亚", "西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "南萨纳兰", "北萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["天然要害沙斯塔夏溶洞", "毒雾洞窟黄金谷", "剑斗领域日影地修炼所"],
        ["守护运输部队", "阴险的魔物——憎恨恶石", "魔界花盛开的世界"],
        ["拉诺西亚高地", "黑衣森林中央林区", "摩杜纳"],
        ["讨伐任务：犯罪组织巴特商会", "迎击任务：验证哈帕利特奴隶末裔说的真伪", "歼敌指令：怒号的米玛斯"],
        ["北萨纳兰", "摩杜纳", "库尔札斯中央高地"]);

    private static ZodiacBookGuide FireTwo() => Book("fire-2", "火天文书·第二卷",
        ["滩齿鱼人", "赤鳞海盗", "武伽玛罗巨像", "叹息之森精", "无头骑士", "精炼剑手", "不悔弓手", "雷蛟", "风扬豪放猛士", "发狂异端者"],
        ["西拉诺西亚", "西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "黑衣森林北部林区", "南萨纳兰", "南萨纳兰", "库尔札斯中央高地", "库尔札斯中央高地", "摩杜纳"],
        ["休养胜地布雷福洛克斯野营地", "神灵圣域放浪神古神殿", "骚乱坑道铜铃铜山"],
        ["南防波堤之战 全力打击", "第二战斗大队", "鸟人军采伐所强攻战"],
        ["西拉诺西亚", "南萨纳兰", "黑衣森林北部林区"],
        ["迎击任务：恶神之眼巴罗尔", "迎击指令：威胁前哨的魔物", "防卫指令：古代亚拉戈的遗物"],
        ["北萨纳兰", "库尔札斯中央高地", "摩杜纳"]);

    private static ZodiacBookGuide WaterTwo() => Book("water-2", "水天文书·第二卷",
        ["赤眼海盗", "萨普沙礁鳞鱼人", "武伽玛罗受施者", "妖精菌帽", "巨虻", "第四大队绳斗士", "精铁龟", "风扬守卫黑狼", "第五大队剑斗士", "基迦巨人比丘"],
        ["西拉诺西亚", "西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "黑衣森林南部林区", "西萨纳兰", "南萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["监狱废墟托托·拉克千狱", "邪教驻地无限城古堡", "恶灵府邸静语庄园"],
        ["北防波堤之战 全力打击", "沼泽林遗恨——恩布鲁", "幽谷药师——黎明药达奇希奥"],
        ["西拉诺西亚", "摩杜纳", "黑衣森林东部林区"],
        ["引导任务：失踪的警犬", "巡逻任务：巡逻白云崖的街道", "迎击指令：第五步兵大队所属部队"],
        ["北萨纳兰", "库尔札斯中央高地", "摩杜纳"]);

    private static ZodiacBookGuide WindTwo() => Book("wind-2", "风天文书·第二卷",
        ["萨普沙礁齿鱼人", "精英祭司", "梦蟾蜍", "守卫军狼", "第四大队旗手", "蜥蜴人弓手", "不悔战斗火蛟", "风扬迷雾术士", "基迦巨人沙门", "第五大队旗手"],
        ["西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "黑衣森林北部林区", "西萨纳兰", "南萨纳兰", "南萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["名门府邸静语庄园", "骚乱坑道铜铃铜山", "纷争要地布雷福洛克斯野营地"],
        ["友人与家人", "力量之塔", "最后的斗猪王——派亚"],
        ["南萨纳兰", "库尔札斯中央高地", "黑衣森林南部林区"],
        ["焚书任务：回收禁书«异界火焰的怪物»", "巡逻任务：抢夺宝石饰品的基迦巨人族", "防卫指令：追击者的遗物"],
        ["北萨纳兰", "摩杜纳", "库尔札斯中央高地"]);

    private static ZodiacBookGuide FirePrisonOne() => Book("fire-prison-1", "火狱文书·第一卷",
        ["第二大队绳斗士", "赤爪海盗", "武伽玛罗巡逻员", "嚎叫之森精", "狂野野猪", "精炼辩士", "赞拉克预言师", "撑船巨人", "诅咒异端者", "基迦巨人僧侣"],
        ["东拉诺西亚", "西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "黑衣森林南部林区", "南萨纳兰", "南萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["古代遗迹喀恩埋没圣堂", "恶灵府邸静语庄园", "剑斗领域日影地修炼所"],
        ["残忍食人魔——波罗斯", "以毒制毒", "命运代言人——灰煤烟佩古基·春"],
        ["摩杜纳", "黑衣森林东部林区", "南萨纳兰"],
        ["巡逻任务：确保补给通道的安全", "歼敌指令：冰霜龙鸟", "迎击指令：威胁调查地安全的基迦巨人族"],
        ["北萨纳兰", "库尔札斯中央高地", "摩杜纳"]);

    private static ZodiacBookGuide WaterPrisonOne() => Book("water-prison-1", "水狱文书·第一卷",
        ["萨普沙礁爪鱼人", "武伽玛罗祭司", "嚎叫紫妖精", "狐蝠", "鸟人风爪剑士", "第四大队拳斗士", "蜥蜴人劫道兵", "雪地白狼", "第五大队绳斗士", "基迦巨人僧都"],
        ["西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "黑衣森林北部林区", "黑衣森林北部林区", "西萨纳兰", "南萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["流沙迷宫樵鸣洞", "领航明灯天狼星灯塔", "腐坏遗迹无限城市街古迹"],
        ["遗迹的亡灵骑士——代达罗斯", "青磷大路", "烈风勇士——怀罗克四姐妹"],
        ["黑衣森林北部林区", "", "库尔札斯中央高地"],
        ["迎击任务：徘徊的利剑牛头魔", "讨伐任务：犯罪组织巴特商会", "歼敌指令：红发的俄刻阿诺斯"],
        ["库尔札斯中央高地", "北萨纳兰", "摩杜纳"]);

    private static ZodiacBookGuide EarthOne() => Book("earth-1", "土天文书·第一卷",
        ["第二大队旗手", "钝口螈", "精英采石员", "叹息紫妖精", "鸟人豪放猛士", "蜥蜴人清道夫", "不悔拳手", "风扬敏捷战士", "第五大队拳斗士", "哈帕利特"],
        ["东拉诺西亚", "西拉诺西亚", "拉诺西亚外地", "黑衣森林东部林区", "黑衣森林北部林区", "南萨纳兰", "南萨纳兰", "库尔札斯中央高地", "摩杜纳", "摩杜纳"],
        ["魔兽领域日影地修炼所", "邪教驻地无限城古堡", "领航明灯天狼星灯塔"],
        ["雪山袭击者——索贝克", "试掘地强攻", "丑恶合成兽——巴杜枭"],
        ["库尔札斯中央高地", "", "东萨纳兰"],
        ["迎击任务：恶神之眼巴罗尔", "焚书任务：回收禁书«尖牙利齿的怪物»", "索敌指令：惊吓调查员的恶灵"],
        ["北萨纳兰", "摩杜纳", "库尔札斯中央高地"]);
}
