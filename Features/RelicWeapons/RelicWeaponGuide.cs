namespace Phantom;

public sealed record RelicWeaponSeries(
    string Key,
    string Name,
    string EnglishName,
    string Summary,
    string SourceUrl,
    IReadOnlyList<PhantomWeaponStage> Stages,
    string? SecondarySourceUrl = null);

public static class RelicWeaponGuide
{
    // Names are used only by the temporary Chinese-client ID export command.
    public static readonly IReadOnlyList<PhantomWeaponProgressStage> ElegantProgressStages = new[]
    {
        new PhantomWeaponProgressStage("elegant-base", "基础武器", 665, "改良型"),
        new PhantomWeaponProgressStage("elegant", "优雅", 665, "优雅"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> ElegantWeaponJobs = new[]
    {
        new PhantomWeaponJob("pld", "骑士", new[] { new[] { "改良型北极星刺", "改良型舰盾" }, new[] { "北极星刺·优雅", "舰盾·优雅" } }),
        new PhantomWeaponJob("mnk", "武僧", StageItems("改良型龙须拳", "龙须拳·优雅")),
        new PhantomWeaponJob("war", "战士", StageItems("改良型碎石怒环", "碎石怒环·优雅")),
        new PhantomWeaponJob("drg", "龙骑士", StageItems("改良型黑狼", "黑狼·优雅")),
        new PhantomWeaponJob("nin", "忍者", StageItems("改良型忍者菜刀", "忍者菜刀·优雅")),
        new PhantomWeaponJob("sam", "武士", StageItems("改良型彼岸此岸", "彼岸此岸·优雅")),
        new PhantomWeaponJob("rpr", "钐镰客", StageItems("改良型化身之镰", "化身之镰·优雅")),
        new PhantomWeaponJob("vpr", "蝰蛇剑士", StageItems("改良型索林之刃", "索林之刃·优雅")),
        new PhantomWeaponJob("drk", "暗黑骑士", StageItems("改良型浴血被提", "浴血被提·优雅")),
        new PhantomWeaponJob("gnb", "绝枪战士", StageItems("改良型刻耳柏洛斯之牙", "刻耳柏洛斯之牙·优雅")),
        new PhantomWeaponJob("whm", "白魔法师", StageItems("改良型弦月玉兔", "弦月玉兔·优雅")),
        new PhantomWeaponJob("sch", "学者", StageItems("改良型仙女迷恋", "仙女迷恋·优雅")),
        new PhantomWeaponJob("ast", "占星术士", StageItems("改良型观星者", "观星者·优雅")),
        new PhantomWeaponJob("sge", "贤者", StageItems("改良型美丽的树枝", "美丽的树枝·优雅")),
        new PhantomWeaponJob("brd", "吟游诗人", StageItems("改良型青鸟之巢", "青鸟之巢·优雅")),
        new PhantomWeaponJob("mch", "机工士", StageItems("改良型胜利属于人民", "胜利属于人民·优雅")),
        new PhantomWeaponJob("dnc", "舞者", StageItems("改良型双月", "双月·优雅")),
        new PhantomWeaponJob("blm", "黑魔法师", StageItems("改良型邪焰冥灯", "邪焰冥灯·优雅")),
        new PhantomWeaponJob("smn", "召唤师", StageItems("改良型百合中的相遇", "百合中的相遇·优雅")),
        new PhantomWeaponJob("rdm", "赤魔法师", StageItems("改良型灯火手杖", "灯火手杖·优雅")),
        new PhantomWeaponJob("pct", "绘灵法师", StageItems("改良型文艺复兴之笔", "文艺复兴之笔·优雅")),
        new PhantomWeaponJob("blu", "青魔法师", StageItems("绅魔法师的伞", "绅魔法师的伞·优雅")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> CosmicProgressStages = new[]
    {
        new PhantomWeaponProgressStage("cosmic-base", "宇宙", 720, "宇宙"),
        new PhantomWeaponProgressStage("cosmic-spacious", "太空", 750, "太空"),
        new PhantomWeaponProgressStage("cosmic-hyperspatial", "超空间", 765, "超空间"),
        new PhantomWeaponProgressStage("cosmic-stellar", "群星", 780, "群星"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> CosmicToolJobs = new[]
    {
        new PhantomWeaponJob("crp", "刻木匠", StageItems("宇宙手锯", "太空手锯", "超空间手锯", "群星手锯")),
        new PhantomWeaponJob("bsm", "锻铁匠", StageItems("宇宙横头锤", "太空横头锤", "超空间横头锤", "群星横头锤")),
        new PhantomWeaponJob("arm", "铸甲匠", StageItems("宇宙圆头锤", "太空圆头锤", "超空间圆头锤", "群星圆头锤")),
        new PhantomWeaponJob("gsm", "雕金匠", StageItems("宇宙工艺锤", "太空工艺锤", "超空间工艺锤", "群星工艺锤")),
        new PhantomWeaponJob("ltw", "制革匠", StageItems("宇宙圆革刀", "太空圆革刀", "超空间圆革刀", "群星圆革刀")),
        new PhantomWeaponJob("wvr", "裁衣匠", StageItems("宇宙缝针", "太空缝针", "超空间缝针", "群星缝针")),
        new PhantomWeaponJob("alc", "炼金术士", StageItems("宇宙蒸馏器", "太空蒸馏器", "超空间蒸馏器", "群星蒸馏器")),
        new PhantomWeaponJob("cul", "烹调师", StageItems("宇宙煎锅", "太空煎锅", "超空间煎锅", "群星煎锅")),
        new PhantomWeaponJob("min", "采矿工", StageItems("宇宙鹤嘴锄", "太空鹤嘴锄", "超空间鹤嘴锄", "群星鹤嘴锄")),
        new PhantomWeaponJob("btn", "园艺工", StageItems("宇宙手斧", "太空手斧", "超空间手斧", "群星手斧")),
        new PhantomWeaponJob("fsh", "捕鱼人", StageItems("宇宙钓竿", "太空钓竿", "超空间钓竿", "群星钓竿")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> ZodiacProgressStages = new[]
    {
        new PhantomWeaponProgressStage("zodiac-relic", "古武", 80, string.Empty),
        new PhantomWeaponProgressStage("zodiac-zenith", "天极", 90, "天极"),
        new PhantomWeaponProgressStage("zodiac-atma", "魂晶", 100, "魂晶"),
        new PhantomWeaponProgressStage("zodiac-animus", "魂灵", 100, "魂灵"),
        new PhantomWeaponProgressStage("zodiac-novus", "新星", 110, "新星"),
        new PhantomWeaponProgressStage("zodiac-nexus", "镇魂", 115, "镇魂"),
        new PhantomWeaponProgressStage("zodiac-zodiac", "黄道", 125, string.Empty),
        new PhantomWeaponProgressStage("zodiac-zeta", "本我", 135, "本我"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> ZodiacWeaponJobs = new[]
    {
        new PhantomWeaponJob("pld", "骑士", new[] { new[] { "无锋剑柯塔纳", "神圣盾" }, new[] { "无锋剑柯塔纳·天极", "神圣盾·天极" }, new[] { "无锋剑柯塔纳·魂晶", "神圣盾·魂晶" }, new[] { "无锋剑柯塔纳·魂灵", "神圣盾·魂灵" }, new[] { "无锋剑柯塔纳·新星", "神圣盾·新星" }, new[] { "无锋剑柯塔纳·镇魂", "神圣盾·镇魂" }, new[] { "王者之剑", "圣盾埃癸斯" }, new[] { "王者之剑·本我", "王者之剑·本我（复制品）", "圣盾埃癸斯·本我", "圣盾埃癸斯·本我（复制品）" } }),
        new PhantomWeaponJob("mnk", "武僧", StageItemsWithReplica("释法来", "释法来·天极", "释法来·魂晶", "释法来·魂灵", "释法来·新星", "释法来·镇魂", "凯撒裂爪", "凯撒裂爪·本我", "凯撒裂爪·本我（复制品）")),
        new PhantomWeaponJob("war", "战士", StageItemsWithReplica("勇悍斧", "勇悍斧·天极", "勇悍斧·魂晶", "勇悍斧·魂灵", "勇悍斧·新星", "勇悍斧·镇魂", "诸神黄昏斧", "诸神黄昏斧·本我", "诸神黄昏斧·本我（复制品）")),
        new PhantomWeaponJob("drg", "龙骑士", StageItemsWithReplica("穿心枪盖博尔格", "穿心枪盖博尔格·天极", "穿心枪盖博尔格·魂晶", "穿心枪盖博尔格·魂灵", "穿心枪盖博尔格·新星", "穿心枪盖博尔格·镇魂", "圣枪朗基努斯", "圣枪朗基努斯·本我", "圣枪朗基努斯·本我（复制品）")),
        new PhantomWeaponJob("brd", "吟游诗人", StageItemsWithReplica("月神之弓", "月神之弓·天极", "月神之弓·魂晶", "月神之弓·魂灵", "月神之弓·新星", "月神之弓·镇魂", "与一之弓", "与一之弓·本我", "与一之弓·本我（复制品）")),
        new PhantomWeaponJob("whm", "白魔法师", StageItemsWithReplica("酒神杖", "酒神杖·天极", "酒神杖·魂晶", "酒神杖·魂灵", "酒神杖·新星", "酒神杖·镇魂", "涅槃杖", "涅槃杖·本我", "涅槃杖·本我（复制品）")),
        new PhantomWeaponJob("blm", "黑魔法师", StageItemsWithReplica("星尘杖", "星尘杖·天极", "星尘杖·魂晶", "星尘杖·魂灵", "星尘杖·新星", "星尘杖·镇魂", "莉莉丝魔杖", "莉莉丝魔杖·本我", "莉莉丝魔杖·本我（复制品）")),
        new PhantomWeaponJob("smn", "召唤师", StageItemsWithReplica("绿瞳列传", "绿瞳列传·天极", "绿瞳列传·魂晶", "绿瞳列传·魂灵", "绿瞳列传·新星", "绿瞳列传·镇魂", "启示录", "启示录·本我", "启示录·本我（复制品）")),
        new PhantomWeaponJob("sch", "学者", StageItemsWithReplica("万辞全书", "万辞全书·天极", "万辞全书·魂晶", "万辞全书·魂灵", "万辞全书·新星", "万辞全书·镇魂", "最终宝典", "最终宝典·本我", "最终宝典·本我（复制品）")),
        new PhantomWeaponJob("nin", "忍者", StageItemsWithReplica("吉光", "吉光·天极", "吉光·魂晶", "吉光·魂灵", "吉光·新星", "吉光·镇魂", "佐助之刀", "佐助之刀·本我", "佐助之刀·本我（复制品）")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> AnimaProgressStages = new[]
    {
        new PhantomWeaponProgressStage("anima-animated", "元灵", 170, "元灵"),
        new PhantomWeaponProgressStage("anima-awoken", "觉醒", 200, "觉醒"),
        new PhantomWeaponProgressStage("anima-anima", "新元灵", 210, string.Empty),
        new PhantomWeaponProgressStage("anima-hyperconductive", "超导", 230, "超导"),
        new PhantomWeaponProgressStage("anima-reconditioned", "百炼成钢", 240, string.Empty),
        new PhantomWeaponProgressStage("anima-sharp", "灵慧", 260, "灵慧"),
        new PhantomWeaponProgressStage("anima-complete", "真元灵", 270, string.Empty),
        new PhantomWeaponProgressStage("anima-lux", "灵光", 275, "灵光"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> AnimaWeaponJobs = new[]
    {
        new PhantomWeaponJob("pld", "骑士", new[] { new[] { "高洁之剑·元灵", "圣女盾·元灵" }, new[] { "高洁之剑·觉醒", "圣女盾·觉醒" }, new[] { "全能剑阿尔玛斯", "安喀勒盾" }, new[] { "全能剑阿尔玛斯·超导", "安喀勒盾·超导" }, new[] { "双子领主之利剑", "双子领主之坚盾" }, new[] { "双子领主之利剑·灵慧", "双子领主之坚盾·灵慧" }, new[] { "血族剑", "圣母盾" }, new[] { "血族剑·灵光", "圣母盾·灵光" } }),
        new PhantomWeaponJob("war", "战士", StageItems("罗摩斧·元灵", "罗摩斧·觉醒", "天雷神斧", "天雷神斧·超导", "鲜血皇帝之巨斧", "鲜血皇帝之巨斧·灵慧", "米诺斯斧", "米诺斯斧·灵光")),
        new PhantomWeaponJob("drk", "暗黑骑士", StageItems("死亡使者·元灵", "死亡使者·觉醒", "诺统", "诺统·超导", "无道暴君之斩首", "无道暴君之斩首·灵慧", "克洛诺斯巨剑", "克洛诺斯巨剑·灵光")),
        new PhantomWeaponJob("whm", "白魔法师", StageItems("炽天使杖·元灵", "炽天使杖·觉醒", "天威杖", "天威杖·超导", "纯白沙皇之法杖", "纯白沙皇之法杖·灵慧", "辛德利幻杖", "辛德利幻杖·灵光")),
        new PhantomWeaponJob("sch", "学者", StageItems("几何原本·元灵", "几何原本·觉醒", "星象四书", "星象四书·超导", "绝世巨擘之诺言", "绝世巨擘之诺言·灵慧", "远征记", "远征记·灵光")),
        new PhantomWeaponJob("ast", "占星术士", StageItems("天宇星象·元灵", "天宇星象·觉醒", "天津四天仪", "天津四天仪·超导", "末代王储之天球", "末代王储之天球·灵慧", "寿星仪", "寿星仪·灵光")),
        new PhantomWeaponJob("mnk", "武僧", StageItems("旭日·元灵", "旭日·觉醒", "军神护拳", "军神护拳·超导", "强权苏丹之对拳", "强权苏丹之对拳·灵慧", "尼耶佩尔斗拳", "尼耶佩尔斗拳·灵光")),
        new PhantomWeaponJob("drg", "龙骑士", StageItems("贯雷枪布里欧纳克·元灵", "贯雷枪布里欧纳克·觉醒", "先锋枪隆格米安特", "先锋枪隆格米安特·超导", "盖世霸王之尖枪", "盖世霸王之尖枪·灵慧", "屠龙戟", "屠龙戟·灵光")),
        new PhantomWeaponJob("nin", "忍者", StageItems("不动行光·元灵", "不动行光·觉醒", "神无", "神无·超导", "荆棘亲王之锐刺", "荆棘亲王之锐刺·灵慧", "藏骨短刀", "藏骨短刀·灵光")),
        new PhantomWeaponJob("brd", "吟游诗人", StageItems("拨铃波琴弓·元灵", "拨铃波琴弓·觉醒", "甘狄拔神弓", "甘狄拔神弓·超导", "独裁帝王之长弓", "独裁帝王之长弓·灵慧", "特尔潘德弓", "特尔潘德弓·灵光")),
        new PhantomWeaponJob("mch", "机工士", StageItems("费迪南德·元灵", "费迪南德·觉醒", "末日", "末日·超导", "王朝君主之烈火", "王朝君主之烈火·灵慧", "死锁", "死锁·灵光")),
        new PhantomWeaponJob("blm", "黑魔法师", StageItems("弯月杖·元灵", "弯月杖·觉醒", "无尽魔源杖", "无尽魔源杖·超导", "漆黑可汗之魔杖", "漆黑可汗之魔杖·灵慧", "死亡命运", "死亡命运·灵光")),
        new PhantomWeaponJob("smn", "召唤师", StageItems("四方天使录·元灵", "四方天使录·觉醒", "巨龙之书", "巨龙之书·超导", "疯狂女王之书卷", "疯狂女王之书卷·灵慧", "摹仿论", "摹仿论·灵光")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> EurekaProgressStages = new[]
    {
        new PhantomWeaponProgressStage("eureka-anemos", "常风", 355, "常风"),
        new PhantomWeaponProgressStage("eureka-pagos", "恒冰", 370, "元素"),
        new PhantomWeaponProgressStage("eureka-pyros", "涌火", 385, "涌火"),
        new PhantomWeaponProgressStage("eureka-hydatos", "丰水", 405, "优雷卡"),
        new PhantomWeaponProgressStage("eureka-physeos", "补正", 405, "改"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> EurekaWeaponJobs = new[]
    {
        new PhantomWeaponJob("pld", "骑士", new[] { new[] { "嘉拉汀·常风", "艾瓦拉克血十字盾·常风" }, new[] { "元素长剑", "元素之盾" }, new[] { "涌火长剑", "涌火之盾" }, new[] { "安忒亚·优雷卡", "柏勒洛丰·优雷卡" }, new[] { "安忒亚·改", "柏勒洛丰·改" } }),
        new PhantomWeaponJob("war", "战士", StageItems("伐煞斧·常风", "元素战斧", "涌火战斧", "沙玛什·优雷卡", "沙玛什·改")),
        new PhantomWeaponJob("drk", "暗黑骑士", StageItems("裂斩剑卡拉德博尔格·常风", "元素断头剑", "涌火断头剑", "剑鱼·优雷卡", "剑鱼·改")),
        new PhantomWeaponJob("whm", "白魔法师", StageItems("驱除之杖·常风", "元素牧杖", "涌火牧杖", "夜蔷薇·优雷卡", "夜蔷薇·改")),
        new PhantomWeaponJob("sch", "学者", StageItems("工具论·常风", "元素魔导典", "涌火魔导典", "杰巴特·优雷卡", "杰巴特·改")),
        new PhantomWeaponJob("ast", "占星术士", StageItems("昴星团天仪·常风", "元素天仪", "涌火天仪", "辇道增七·优雷卡", "辇道增七·改")),
        new PhantomWeaponJob("mnk", "武僧", StageItems("善见神轮·常风", "元素指虎", "涌火指虎", "杜穆齐德·优雷卡", "杜穆齐德·改")),
        new PhantomWeaponJob("drg", "龙骑士", StageItems("龙须·常风", "元素龙枪", "涌火龙枪", "璎珞蛇·优雷卡", "璎珞蛇·改")),
        new PhantomWeaponJob("nin", "忍者", StageItems("息风·常风", "元素匕首", "涌火匕首", "鹊·优雷卡", "鹊·改")),
        new PhantomWeaponJob("sam", "武士", StageItems("菊一文字·常风", "元素武士刀", "涌火武士刀", "鸟头太刀·优雷卡", "鸟头太刀·改")),
        new PhantomWeaponJob("brd", "吟游诗人", StageItems("必中琴弓·常风", "元素竖琴弓", "涌火竖琴弓", "泽鹰·优雷卡", "泽鹰·改")),
        new PhantomWeaponJob("mch", "机工士", StageItems("外来者·常风", "元素手炮", "涌火手炮", "玛丽弗里斯·优雷卡", "玛丽弗里斯·改")),
        new PhantomWeaponJob("blm", "黑魔法师", StageItems("破坏之杖·常风", "元素法杖", "涌火法杖", "座头鲸·优雷卡", "座头鲸·改")),
        new PhantomWeaponJob("smn", "召唤师", StageItems("雷蒙盖顿·常风", "元素魔导书", "涌火魔导书", "图亚·优雷卡", "图亚·改")),
        new PhantomWeaponJob("rdm", "赤魔法师", StageItems("死印剑·常风", "元素刺剑", "涌火刺剑", "布鲁奈罗·优雷卡", "布鲁奈罗·改")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> ResistanceProgressStages = new[]
    {
        new PhantomWeaponProgressStage("resistance-base", "义军", 485, string.Empty),
        new PhantomWeaponProgressStage("resistance-augmented", "+1", 500, "改良型"),
        new PhantomWeaponProgressStage("resistance-recollection", "回忆", 500, "回忆"),
        new PhantomWeaponProgressStage("resistance-law-order", "裁决", 510, "裁决"),
        new PhantomWeaponProgressStage("resistance-augmented-law-order", "裁决+1", 515, "改良型裁决"),
        new PhantomWeaponProgressStage("resistance-blades", "女王", 535, "女王"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> ResistanceWeaponJobs = new[]
    {
        new PhantomWeaponJob("pld", "骑士", new[] { new[] { "身负荣耀", "坚毅" }, new[] { "改良型身负荣耀", "改良型坚毅" }, new[] { "身负荣耀·回忆", "坚毅·回忆" }, new[] { "裁决手半剑", "裁决鸢盾" }, new[] { "改良型裁决手半剑", "改良型裁决鸢盾" }, new[] { "女王之荣耀", "女王之坚毅" } }),
        new PhantomWeaponJob("war", "战士", StageItems("开天碎颅", "改良型开天碎颅", "开天碎颅·回忆", "裁决双刃斧", "改良型裁决双刃斧", "女王之英勇")),
        new PhantomWeaponJob("drk", "暗黑骑士", StageItems("生于哀伤", "改良型生于哀伤", "生于哀伤·回忆", "裁决双手剑", "改良型裁决双手剑", "女王之正义")),
        new PhantomWeaponJob("gnb", "绝枪战士", StageItems("冠绝之刃", "改良型冠绝之刃", "冠绝之刃·回忆", "裁决魔机刃", "改良型裁决魔机刃", "女王之决意")),
        new PhantomWeaponJob("whm", "白魔法师", StageItems("怒心", "改良型怒心", "怒心·回忆", "裁决牧杖", "改良型裁决牧杖", "女王之慈悲")),
        new PhantomWeaponJob("sch", "学者", StageItems("群贤毕至", "改良型群贤毕至", "群贤毕至·回忆", "裁决魔导典", "改良型裁决魔导典", "女王之睿智")),
        new PhantomWeaponJob("ast", "占星术士", StageItems("天阳至宁", "改良型天阳至宁", "天阳至宁·回忆", "裁决量天仪", "改良型裁决量天仪", "女王之天命")),
        new PhantomWeaponJob("mnk", "武僧", StageItems("轮回", "改良型轮回", "轮回·回忆", "裁决指虎", "改良型裁决指虎", "女王之安宁")),
        new PhantomWeaponJob("drg", "龙骑士", StageItems("三锋锐", "改良型三锋锐", "三锋锐·回忆", "裁决长枪", "改良型裁决长枪", "女王之荣光")),
        new PhantomWeaponJob("nin", "忍者", StageItems("骨不知", "改良型骨不知", "骨不知·回忆", "裁决匕首", "改良型裁决匕首", "女王之精巧")),
        new PhantomWeaponJob("sam", "武士", StageItems("星切", "改良型星切", "星切·回忆", "裁决武士刀", "改良型裁决武士刀", "女王之忠诚")),
        new PhantomWeaponJob("brd", "吟游诗人", StageItems("才气焕发", "改良型才气焕发", "才气焕发·回忆", "裁决复合弓", "改良型裁决复合弓", "女王之沉思")),
        new PhantomWeaponJob("mch", "机工士", StageItems("执法", "改良型执法", "执法·回忆", "裁决左轮枪", "改良型裁决左轮枪", "女王之才智")),
        new PhantomWeaponJob("dnc", "舞者", StageItems("交转重环", "改良型交转重环", "交转重环·回忆", "裁决圆月轮", "改良型裁决圆月轮", "女王之愉悦")),
        new PhantomWeaponJob("blm", "黑魔法师", StageItems("魂罚", "改良型魂罚", "魂罚·回忆", "裁决咒杖", "改良型裁决咒杖", "女王之盛怒")),
        new PhantomWeaponJob("smn", "召唤师", StageItems("一灵真性", "改良型一灵真性", "一灵真性·回忆", "裁决魔导书", "改良型裁决魔导书", "女王之敏锐")),
        new PhantomWeaponJob("rdm", "赤魔法师", StageItems("述说者", "改良型述说者", "述说者·回忆", "裁决刺剑", "改良型裁决刺剑", "女王之克己")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> SkysteelProgressStages = new[]
    {
        new PhantomWeaponProgressStage("skysteel-base", "天钢", 440, "天钢"),
        new PhantomWeaponProgressStage("skysteel-plus-one", "+1", 455, "+1"),
        new PhantomWeaponProgressStage("skysteel-dragonsung", "龙诗", 475, "龙诗"),
        new PhantomWeaponProgressStage("skysteel-augmented-dragonsung", "改良龙诗", 485, "改良型龙诗"),
        new PhantomWeaponProgressStage("skysteel-skysung", "天诗", 500, "天诗"),
        new PhantomWeaponProgressStage("skysteel-skybuilders", "天工", 510, "天工"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> SkysteelToolJobs = new[]
    {
        new PhantomWeaponJob("crp", "刻木匠", StageItems("天钢手锯", "天钢手锯+1", "龙诗手锯", "改良型龙诗手锯", "天诗手锯", "天工手锯")),
        new PhantomWeaponJob("bsm", "锻铁匠", StageItems("天钢横头锤", "天钢横头锤+1", "龙诗横头锤", "改良型龙诗横头锤", "天诗横头锤", "天工横头锤")),
        new PhantomWeaponJob("arm", "铸甲匠", StageItems("天钢圆头锤", "天钢圆头锤+1", "龙诗圆头锤", "改良型龙诗圆头锤", "天诗圆头锤", "天工圆头锤")),
        new PhantomWeaponJob("gsm", "雕金匠", StageItems("天钢工艺锤", "天钢工艺锤+1", "龙诗工艺锤", "改良型龙诗工艺锤", "天诗工艺锤", "天工工艺锤")),
        new PhantomWeaponJob("ltw", "制革匠", StageItems("天钢圆革刀", "天钢圆革刀+1", "龙诗圆革刀", "改良型龙诗圆革刀", "天诗圆革刀", "天工圆革刀")),
        new PhantomWeaponJob("wvr", "裁衣匠", StageItems("天钢缝针", "天钢缝针+1", "龙诗缝针", "改良型龙诗缝针", "天诗缝针", "天工缝针")),
        new PhantomWeaponJob("alc", "炼金术士", StageItems("天钢蒸馏器", "天钢蒸馏器+1", "龙诗蒸馏器", "改良型龙诗蒸馏器", "天诗蒸馏器", "天工蒸馏器")),
        new PhantomWeaponJob("cul", "烹调师", StageItems("天钢煎锅", "天钢煎锅+1", "龙诗煎锅", "改良型龙诗煎锅", "天诗煎锅", "天工煎锅")),
        new PhantomWeaponJob("min", "采矿工", StageItems("天钢鹤嘴锄", "天钢鹤嘴锄+1", "龙诗鹤嘴锄", "改良型龙诗鹤嘴锄", "天诗鹤嘴锄", "天工鹤嘴锄")),
        new PhantomWeaponJob("btn", "园艺工", StageItems("天钢手斧", "天钢手斧+1", "龙诗手斧", "改良型龙诗手斧", "天诗手斧", "天工手斧")),
        new PhantomWeaponJob("fsh", "捕鱼人", StageItems("天钢钓竿", "天钢钓竿+1", "龙诗钓竿", "改良型龙诗钓竿", "天诗钓竿", "天工钓竿")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> SplendorousProgressStages = new[]
    {
        new PhantomWeaponProgressStage("splendorous-base", "卓越", 570, "卓越"),
        new PhantomWeaponProgressStage("splendorous-augmented", "改良卓越", 590, "改良型卓越"),
        new PhantomWeaponProgressStage("splendorous-crystalline", "水晶", 620, "水晶"),
        new PhantomWeaponProgressStage("splendorous-chora-zoi", "乔菈水晶", 625, "乔菈水晶"),
        new PhantomWeaponProgressStage("splendorous-brilliant", "乔菈卓绝", 630, "乔菈卓绝"),
        new PhantomWeaponProgressStage("splendorous-vrandtic", "诺弗兰特远见", 635, "诺弗兰特远见"),
        new PhantomWeaponProgressStage("splendorous-lodestar", "领航星", 640, "领航星"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> SplendorousToolJobs = new[]
    {
        new PhantomWeaponJob("crp", "刻木匠", StageItems("卓越手锯", "改良型卓越手锯", "水晶手锯", "乔菈水晶手锯", "乔菈卓绝手锯", "诺弗兰特远见手锯", "领航星手锯")),
        new PhantomWeaponJob("bsm", "锻铁匠", StageItems("卓越横头锤", "改良型卓越横头锤", "水晶横头锤", "乔菈水晶横头锤", "乔菈卓绝横头锤", "诺弗兰特远见横头锤", "领航星横头锤")),
        new PhantomWeaponJob("arm", "铸甲匠", StageItems("卓越圆头锤", "改良型卓越圆头锤", "水晶圆头锤", "乔菈水晶圆头锤", "乔菈卓绝圆头锤", "诺弗兰特远见圆头锤", "领航星圆头锤")),
        new PhantomWeaponJob("gsm", "雕金匠", StageItems("卓越工艺锤", "改良型卓越工艺锤", "水晶工艺锤", "乔菈水晶工艺锤", "乔菈卓绝工艺锤", "诺弗兰特远见工艺锤", "领航星工艺锤")),
        new PhantomWeaponJob("ltw", "制革匠", StageItems("卓越圆革刀", "改良型卓越圆革刀", "水晶圆革刀", "乔菈水晶圆革刀", "乔菈卓绝圆革刀", "诺弗兰特远见圆革刀", "领航星圆革刀")),
        new PhantomWeaponJob("wvr", "裁衣匠", StageItems("卓越缝针", "改良型卓越缝针", "水晶缝针", "乔菈水晶缝针", "乔菈卓绝缝针", "诺弗兰特远见缝针", "领航星缝针")),
        new PhantomWeaponJob("alc", "炼金术士", StageItems("卓越蒸馏器", "改良型卓越蒸馏器", "水晶蒸馏器", "乔菈水晶蒸馏器", "乔菈卓绝蒸馏器", "诺弗兰特远见蒸馏器", "领航星蒸馏器")),
        new PhantomWeaponJob("cul", "烹调师", StageItems("卓越煎锅", "改良型卓越煎锅", "水晶煎锅", "乔菈水晶煎锅", "乔菈卓绝煎锅", "诺弗兰特远见平底锅", "领航星平底锅")),
        new PhantomWeaponJob("min", "采矿工", StageItems("卓越鹤嘴锄", "改良型卓越鹤嘴锄", "水晶鹤嘴锄", "乔菈水晶鹤嘴锄", "乔菈卓绝鹤嘴锄", "诺弗兰特远见鹤嘴锄", "领航星鹤嘴锄")),
        new PhantomWeaponJob("btn", "园艺工", StageItems("卓越手斧", "改良型卓越手斧", "水晶手斧", "乔菈水晶手斧", "乔菈卓绝手斧", "诺弗兰特远见手斧", "领航星手斧")),
        new PhantomWeaponJob("fsh", "捕鱼人", StageItems("卓越钓竿", "改良型卓越钓竿", "水晶钓竿", "乔菈水晶钓竿", "乔菈卓绝钓竿", "诺弗兰特远见钓竿", "领航星钓竿")),
    };

    public static readonly IReadOnlyList<PhantomWeaponProgressStage> UltimateProgressStages = new[]
    {
        new PhantomWeaponProgressStage("ultimate-ucob", "绝巴哈", 345, "绝境龙神"),
        new PhantomWeaponProgressStage("ultimate-uwu", "绝神兵", 375, "究极"),
        new PhantomWeaponProgressStage("ultimate-tea", "绝亚", 475, "绝境"),
        new PhantomWeaponProgressStage("ultimate-dsr", "绝龙诗", 605, "绝境苍穹"),
        new PhantomWeaponProgressStage("ultimate-top", "绝欧米茄", 635, "绝境欧米茄"),
        new PhantomWeaponProgressStage("ultimate-fru", "绝伊甸", 735, "绝境伊甸之晨"),
        new PhantomWeaponProgressStage("ultimate-chaos", "绝妖星", 0, "帕拉佐钻石"),
    };

    public static readonly IReadOnlyList<PhantomWeaponJob> UltimateWeaponJobs = new[]
    {
        new PhantomWeaponJob("pld", "骑士", new[] { new[] { "绝境龙神剑", "绝境龙神盾" }, new[] { "无锋剑柯塔纳·究极", "神圣盾·究极" }, new[] { "绝境全能剑阿尔玛斯", "绝境安喀勒盾" }, new[] { "绝境苍穹之剑", "绝境苍穹之盾" }, new[] { "绝境欧米茄长剑", "绝境欧米茄鸢盾" }, new[] { "绝境伊甸之晨手半剑", "绝境伊甸之晨步兵盾" }, new[] { "帕拉佐钻石剑", "帕拉佐钻石盾" } }),
        new PhantomWeaponJob("mnk", "武僧", StageItems("绝境龙神爪", "释法来·究极", "绝境军神护拳", "绝境苍穹之拳", "绝境欧米茄拳套", "绝境伊甸之晨圣徒", "帕拉佐钻石拳套")),
        new PhantomWeaponJob("war", "战士", StageItems("绝境龙神斧", "勇悍斧·究极", "绝境天雷神斧", "绝境苍穹之斧", "绝境欧米茄战斧", "绝境伊甸之晨战斧", "帕拉佐钻石大斧")),
        new PhantomWeaponJob("drg", "龙骑士", StageItems("绝境龙神枪", "穿心枪盖博尔格·究极", "绝境先锋枪隆格米安特", "绝境苍穹之戟", "绝境欧米茄三尖枪", "绝境伊甸之晨战戟", "帕拉佐钻石战戟")),
        new PhantomWeaponJob("brd", "吟游诗人", StageItems("绝境龙神弓", "月神之弓·究极", "绝境甘狄拔神弓", "绝境苍穹之弓", "绝境欧米茄长弓", "绝境伊甸之晨骑兵弓", "帕拉佐钻石长弓")),
        new PhantomWeaponJob("nin", "忍者", StageItems("绝境龙神匕首", "吉光·究极", "绝境神无", "绝境苍穹之刀", "绝境欧米茄夺命镰", "绝境伊甸之晨左手剑", "帕拉佐钻石小刀")),
        new PhantomWeaponJob("drk", "暗黑骑士", StageItems("绝境龙神大剑", "死亡使者·究极", "绝境诺统", "绝境苍穹之大剑", "绝境欧米茄大剑", "绝境伊甸之晨双手剑", "帕拉佐钻石巨剑")),
        new PhantomWeaponJob("mch", "机工士", StageItems("绝境龙神手炮", "费迪南德·究极", "绝境末日", "绝境苍穹之火", "绝境欧米茄短铳", "绝境伊甸之晨手枪", "帕拉佐钻石火枪")),
        new PhantomWeaponJob("whm", "白魔法师", StageItems("绝境龙神牧杖", "酒神杖·究极", "绝境天威杖", "绝境苍穹之幻杖", "绝境欧米茄牧杖", "绝境伊甸之晨牧杖", "帕拉佐钻石幻杖")),
        new PhantomWeaponJob("blm", "黑魔法师", StageItems("绝境龙神长杖", "星尘杖·究极", "绝境无尽魔源杖", "绝境苍穹之咒杖", "绝境欧米茄咒杖", "绝境伊甸之晨法杖", "帕拉佐钻石咒杖")),
        new PhantomWeaponJob("smn", "召唤师", StageItems("绝境龙神书", "绿瞳列传·究极", "绝境巨龙之书", "绝境苍穹之书", "绝境欧米茄魔导书", "绝境伊甸之晨魔导书", "帕拉佐钻石魔导书")),
        new PhantomWeaponJob("sch", "学者", StageItems("绝境龙神典", "万辞全书·究极", "绝境星象四书", "绝境苍穹之典", "绝境欧米茄魔导典", "绝境伊甸之晨魔导典", "帕拉佐钻石魔导典")),
        new PhantomWeaponJob("ast", "占星术士", StageItems("绝境龙神黄道仪", "天宇星象·究极", "绝境天津四天仪", "绝境苍穹之仪", "绝境欧米茄黄道仪", "绝境伊甸之晨黄道仪", "帕拉佐钻石六分仪")),
        new PhantomWeaponJob("sam", "武士", StageItems("绝境龙神刀", "天座·究极", "绝境噬骨", "绝境苍穹之太刀", "绝境欧米茄武士刀", "绝境伊甸之晨武士刀", "帕拉佐钻石刀")),
        new PhantomWeaponJob("rdm", "赤魔法师", StageItems("绝境龙神刺剑", "刺针·究极", "绝境灵慧刺剑", "绝境苍穹之刺剑", "绝境欧米茄小剑", "绝境伊甸之晨刺剑", "帕拉佐钻石细剑")),
        new PhantomWeaponJob("gnb", "绝枪战士", StageItems(string.Empty, string.Empty, "绝境无序", "绝境苍穹之枪刃", "绝境欧米茄刺刀", "绝境伊甸之晨枪刃", "帕拉佐钻石刺刀")),
        new PhantomWeaponJob("dnc", "舞者", StageItems(string.Empty, string.Empty, "绝境锻造神环刃", "绝境苍穹之战轮", "绝境欧米茄圆月轮", "绝境伊甸之晨圆月轮", "帕拉佐钻石圆月轮")),
        new PhantomWeaponJob("sge", "贤者", StageItems(string.Empty, string.Empty, string.Empty, "绝境苍穹之蛇石针", "绝境欧米茄飞翼", "绝境伊甸之晨振空摆", "帕拉佐钻石飞翼")),
        new PhantomWeaponJob("rpr", "钐镰客", StageItems(string.Empty, string.Empty, string.Empty, "绝境苍穹之夺命镰", "绝境欧米茄扎戈斧镰", "绝境伊甸之晨夺命镰", "帕拉佐钻石战镰")),
        new PhantomWeaponJob("vpr", "蝰蛇剑士", StageItems(string.Empty, string.Empty, string.Empty, string.Empty, "绝境欧米茄双牙", "绝境伊甸之晨双牙", "帕拉佐钻石双军刀")),
        new PhantomWeaponJob("pct", "绘灵法师", StageItems(string.Empty, string.Empty, string.Empty, string.Empty, "绝境欧米茄圆笔", "绝境伊甸之晨圆笔", "帕拉佐钻石扇形笔")),
    };

    public static readonly IReadOnlyDictionary<string, RelicWeaponSeries> Series = new[]
    {
        CreateZodiac(),
        CreateAnima(),
        CreateEureka(),
        CreateResistance(),
        CreateSkysteel(),
        CreateSplendorous(),
        CreateCosmic(),
        CreateUltimate(),
    }.ToDictionary(series => series.Key, StringComparer.Ordinal);

    private static RelicWeaponSeries CreateZodiac() => new(
        "zodiac",
        "古武",
        "Zodiac Weapons",
        "2.x 古武由上古武器与黄道武器两部分组成：上古武器为第一、二阶段，黄道武器为第三至第八阶段。",
        "https://ff14.huijiwiki.com/wiki/%E4%B8%8A%E5%8F%A4%E6%AD%A6%E5%99%A8",
        new[]
        {
            Stage("zodiac-relic", "上古武器", "iLvl 80", "复苏的上古武器", "从黄昏湾开启上古武器任务线，完成原型武器、职业武器、讨伐战和拉札罕淬火油流程。",
                Requirements(("zodiac-relic-poetics", "亚拉戈诗学神典石", 15, "兑换拉札罕淬火油。"), ("zodiac-relic-materia", "叁型魔晶石", 2, "按职业武器要求镶嵌 2 颗指定叁型魔晶石。")),
                Tasks(("zodiac-relic-unlock", "开启复苏的上古武器", "黄昏湾奈德里克·艾恩哈特处接取“传说中的武器工匠”。"), ("zodiac-relic-duties", "完成奇美拉、海德拉和三蛮神", "按任务要求完成死化奇美拉、海德拉、伊弗利特、迦楼罗、泰坦。"))),
            Stage("zodiac-zenith", "上古武器·天极", "iLvl 90", "天极强化", "在黑衣森林北部林区冶炼炉用萨维奈灵药强化。",
                Requirements(("zodiac-zenith-thavnairian-mist", "萨维奈灵药", 3, "共需 60 亚拉戈诗学神典石。"), ("zodiac-zenith-poetics", "亚拉戈诗学神典石", 60, "用于兑换 3 个萨维奈灵药。"))),
            Stage("zodiac-atma", "黄道武器·魂晶", "iLvl 100", "黄道十二文书前置", "收集 12 个魂晶开启黄道武器后续强化。",
                Requirements(("zodiac-atma", "十二魂晶", 12, "在指定 2.x 地区 FATE 获得。"))),
            Stage("zodiac-animus", "黄道武器·魂灵", "iLvl 100", "黄道十二文书", "完成 9 本黄道十二文书，每本包含指定敌人、迷宫、FATE 和理符目标。",
                Requirements(("zodiac-animus-books", "黄道十二文书", 9, "每本完成后推进一次魂灵进度。"))),
            Stage("zodiac-novus", "黄道武器·新星", "iLvl 110", "天球书卷", "通过亚历山大石和魔晶石为天球书卷注入属性。",
                Requirements(("zodiac-novus-alexandrite", "亚历山大石", 75, "用于天球书卷属性注入。"))),
            Stage("zodiac-nexus", "黄道武器·镇魂", "iLvl 115", "灵魂共鸣", "装备武器完成指定内容积累灵魂共鸣。",
                Requirements(("zodiac-nexus-light", "灵魂共鸣", 2000, "通过副本、讨伐、FATE 等内容积累光。"))),
            Stage("zodiac-zodiac", "黄道武器", "iLvl 125", "黄道武器四任务", "完成四个材料任务并交付指定物品。",
                Requirements(("zodiac-zodiac-quests", "黄道武器材料任务", 4, "完成四条任务线并交付材料。"))),
            Stage("zodiac-zeta", "黄道武器·本我", "iLvl 135", "灵魂凝聚最终阶段", "使用十二文书型灵魂凝聚器完成最终光阶段。",
                Requirements(("zodiac-zeta-mahatma", "黄道本我", 12, "逐个完成 12 个本我光阶段。"))),
        },
        "https://ff14.huijiwiki.com/wiki/%E9%BB%84%E9%81%93%E6%AD%A6%E5%99%A8");

    private static RelicWeaponSeries CreateAnima() => new(
        "anima",
        "魂武",
        "Anima Weapons",
        "3.x 元灵武器系列，主要消耗发光水晶、诗学兑换材料、水晶砂和后续光阶段。",
        "https://ff14.huijiwiki.com/wiki/%E5%85%83%E7%81%B5%E6%AD%A6%E5%99%A8",
        new[]
        {
            Stage("anima-animated", "元灵武器·元灵", "iLvl 170", "生命、元灵和战争", "完成开启任务并交付六种发光水晶。",
                Requirements(("anima-luminous-crystals", "发光水晶", 18, "六个苍天区域各 3 个。"))),
            Stage("anima-awoken", "元灵武器·觉醒", "iLvl 200", "英雄的轨迹", "装备元灵武器完成指定 10 个苍天迷宫。",
                Requirements(("anima-awoken-dungeons", "指定迷宫", 10, "按任务列表完成苍天地区迷宫。"))),
            Stage("anima-anima", "新元灵武器", "iLvl 210", "人造精灵的未来", "交付多类诗学兑换材料完成新元灵武器。",
                Requirements(("anima-enchanted-rubber", "魔法橡胶", 10, "诗学或其他货币兑换。"), ("anima-fast-drying-carboncoat", "快速硬化碳化油", 10, "诗学或其他货币兑换。"), ("anima-divine-water", "神圣水", 10, "诗学或其他货币兑换。"), ("anima-fast-acting-allagan-catalyst", "高效古代附魔剂", 10, "诗学或其他货币兑换。"))),
            Stage("anima-hyperconductive", "元灵武器·超导", "iLvl 230", "人造精灵的声音", "交付 5 个亚拉戈绝灵油。",
                Requirements(("anima-aether-oil", "亚拉戈绝灵油", 5, "诗学兑换或相关任务奖励。"))),
            Stage("anima-reconditioned", "百炼成钢的元灵武器", "iLvl 240", "人造精灵的未来", "使用水晶砂和硬灵性岩完成 240 点属性成长。",
                Requirements(("anima-crystal-sand", "水晶砂", 80, "和硬灵性岩一起用于属性成长。"), ("anima-umbrite", "硬灵性岩", 80, "和水晶砂一起用于属性成长。"))),
            Stage("anima-sharp", "元灵武器·灵慧", "iLvl 260", "人造精灵的磨砺", "装备武器积累灵魂凝聚度。",
                Requirements(("anima-light", "灵魂凝聚度", 2000, "通过副本、讨伐、团队任务等内容积累。"))),
            Stage("anima-complete", "真元灵武器", "iLvl 270", "人造精灵的完结", "交付古代附魔墨水等诗学材料完成真元灵武器。",
                Requirements(("anima-archaic-enchanted-ink", "古代附魔墨水", 1, "诗学兑换。"))),
            Stage("anima-lux", "真元灵武器·灵光", "iLvl 275", "人造精灵的灵光", "完成最终任务与指定讨伐歼灭战。",
                tasks: Tasks(("anima-lux-trials", "完成最终连续讨伐", "按任务要求完成最终阶段的连续讨伐内容。"))),
        });

    private static RelicWeaponSeries CreateEureka() => new(
        "eureka",
        "优武",
        "Eureka Weapons",
        "4.x 禁地兵装系列，按常风、恒冰、涌火、丰水推进，最终可追加优雷卡专用效果。",
        "https://ff14.huijiwiki.com/wiki/%E7%A6%81%E5%9C%B0%E5%85%B5%E8%A3%85",
        new[]
        {
            Stage("eureka-anemos", "常风武器", "iLvl 335-355", "优雷卡常风之地", "从旧化特职武器强化到禁地兵装·常风。",
                Requirements(("eureka-anemos-crystals", "乱属性水晶", 1300, "常风阶段武器合计。"), ("eureka-pazuzu-feathers", "帕祖祖的羽毛", 3, "禁地兵装·常风最终强化。"))),
            Stage("eureka-pagos", "恒冰武器", "iLvl 360-370", "优雷卡恒冰之地", "强化至禁地兵装·元素。",
                Requirements(("eureka-frosted-crystals", "结冰乱属性水晶", 31, "恒冰武器合计。"), ("eureka-pagos-crystals", "恒冰水晶", 500, "恒冰+1 阶段。"), ("eureka-louhi-ice", "娄希的冰片", 5, "元素阶段。"))),
            Stage("eureka-pyros", "涌火武器", "iLvl 375-385", "优雷卡涌火之地", "强化至禁地兵装·涌火，并解锁副属性调整。",
                Requirements(("eureka-pyros-crystals", "涌火水晶", 650, "涌火武器合计。"), ("eureka-penthesilea-flames", "彭忒西勒亚的火种", 5, "禁地兵装·涌火最终强化。"), ("eureka-logograms", "文理技能图鉴", 30, "涌火武器阶段要求。"))),
            Stage("eureka-hydatos", "丰水武器", "iLvl 390-405", "优雷卡丰水之地", "强化至禁地兵装最终形态。",
                Requirements(("eureka-hydatos-crystals", "丰水水晶", 350, "丰水武器合计。"), ("eureka-crystalline-scales", "水晶龙之鳞", 5, "禁地兵装最终形态。"))),
            Stage("eureka-physeos", "补正", "iLvl 405", "优雷卡专用效果强化", "使用优雷卡断片追加优雷卡专用效果。",
                Requirements(("eureka-fragments", "优雷卡的断片", 100, "武器改装阶段。"))),
        });

    private static RelicWeaponSeries CreateResistance() => new(
        "resistance",
        "义武",
        "Resistance Weapons",
        "5.x 义军武器系列，可通过博兹雅、扎杜诺尔、副本、FATE 等多路线收集材料。",
        "https://ff14.huijiwiki.com/wiki/%E4%B9%89%E5%86%9B%E6%AD%A6%E5%99%A8",
        new[]
        {
            Stage("resistance-base", "义军武器", "iLvl 485", "重现“女王之刃”", "第一把免费，后续武器交付萨维奈灵鳞粉。",
                Requirements(("resistance-thavnairian-scalepowder", "萨维奈灵鳞粉", 4, "第二把及以后每把 4 个，共 1000 诗学。"), ("resistance-base-poetics", "亚拉戈诗学神典石", 1000, "用于兑换萨维奈灵鳞粉。"))),
            Stage("resistance-augmented", "义军武器+1", "iLvl 500", "将记忆固定在义军武器之上", "交付三色记忆晶块。",
                Requirements(("resistance-tortured-memory", "烦恼的记忆晶块", 20, "博兹雅南部或指定 3.0 FATE。"), ("resistance-sorrowful-memory", "悲伤的记忆晶块", 20, "博兹雅中部或指定 3.0 FATE。"), ("resistance-harrowing-memory", "恐惧的记忆晶块", 20, "博兹雅北部或指定 3.0 FATE。"))),
            Stage("resistance-recollection", "义军武器·回忆", "iLvl 500", "将勇猛的记忆固定在义军武器之上", "交付勇猛的记忆晶块。",
                Requirements(("resistance-bitter-memory", "勇猛的记忆晶块", 6, "南方博兹雅战线、60 级迷宫或每日练级随机。"))),
            Stage("resistance-law-order", "义军武器·裁决", "iLvl 510", "义军武器，变形", "交付厌恶的记忆晶块。",
                Requirements(("resistance-loathsome-memory", "厌恶的记忆晶块", 15, "帝国湖岸堡攻城战、博兹雅紧急遭遇战或水晶塔。"))),
            Stage("resistance-augmented-law-order-prep", "义军武器·裁决+1（过渡）", "过渡", "球状物体，前来救急", "全职业一次性过渡任务，交付不祥与忌讳的记忆晶块。",
                Requirements(("resistance-haunting-memory", "不祥的记忆晶块", 18, "4.0 FATE 或玛哈之影。"), ("resistance-vexatious-memory", "忌讳的记忆晶块", 18, "4.0 FATE 或重返伊瓦利斯。"))),
            Stage("resistance-augmented-law-order", "义军武器·裁决+1", "iLvl 515", "义军武器的崭新未来", "交付被丢掉的遗物并开放属性分配。",
                Requirements(("resistance-timeworn-artifact", "被丢掉的遗物", 15, "女王古殿或死者宫殿。"))),
            Stage("resistance-blades-prep", "义军武器·女王（过渡）", "过渡", "古代博兹雅之梦", "全职业一次性三组任务，交付机械零件、战斗记录和记忆晶块。",
                Requirements(("resistance-compact-axle", "超小型传动轴", 30, "扎杜诺尔南部冲突战或亚历山大 1/2 层。"), ("resistance-compact-spring", "超小型弹簧", 30, "扎杜诺尔南部紧急遭遇战或亚历山大 3/4 层。"), ("resistance-battle-record-1", "激战的战斗记录：第一集", 30, "扎杜诺尔西部冲突战或欧米茄 1/2 层。"), ("resistance-battle-record-2", "激战的战斗记录：第二集", 30, "扎杜诺尔西部紧急遭遇战或欧米茄 3/4 层。"), ("resistance-bleak-memory", "沉重的记忆晶块", 30, "扎杜诺尔北部冲突战或伊甸 1/2 层。"), ("resistance-lurid-memory", "粗暴的记忆晶块", 30, "扎杜诺尔北部紧急遭遇战或伊甸 3/4 层。"))),
            Stage("resistance-blades", "义军武器·女王", "iLvl 535", "真正的义军武器", "交付光辉的激情晶块完成最终阶段。",
                Requirements(("resistance-raw-emotion", "光辉的激情晶块", 15, "女王古殿、旗舰达尔里阿达号、70 级迷宫或天之御柱。"))),
        });

    private static RelicWeaponSeries CreateSkysteel() => new(
        "skysteel",
        "天钢",
        "Skysteel Tools",
        "5.x 生产采集特殊工具，通过天穹街相关任务、收藏品、采集与钓鱼素材强化。",
        "https://ff14.huijiwiki.com/wiki/%E5%A4%A9%E9%92%A2%E5%B7%A5%E5%85%B7",
        new[]
        {
            Stage("skysteel-base", "天钢工具", "iLvl 440", "沉睡于工房的陈旧工具", "完成任务获得第一把工具，后续可在伊修加德基础层购买。"),
            Stage("skysteel-plus-one", "天钢工具+1", "iLvl 455", "天钢工具强化", "交付 5.25 第一组工匠或采集强化素材。"),
            Stage("skysteel-dragonsung", "龙诗工具", "iLvl 475", "龙诗工具强化", "交付 5.25 第二组工匠或采集强化素材。"),
            Stage("skysteel-augmented-dragonsung", "改良型龙诗工具", "iLvl 485", "改良工具的建议", "开始使用收藏品价值换取强化素材。"),
            Stage("skysteel-skysung", "天诗工具", "iLvl 500", "经过打磨的匠人工具", "继续交付 5.35 收藏品或采集素材。"),
            Stage("skysteel-skybuilders", "天工工具", "iLvl 510", "日臻完善的匠人工具", "最终阶段需要高难度配方或最终采集/钓鱼素材。"),
        });

    private static RelicWeaponSeries CreateSplendorous() => new(
        "splendorous",
        "莫雯",
        "Splendorous Tools",
        "6.x 莫雯卓越工具系列，通过水晶都任务、收藏品、精选与采集/钓鱼素材强化。",
        "https://ff14.huijiwiki.com/wiki/%E8%8E%AB%E9%9B%AF%E5%8D%93%E8%B6%8A%E5%B7%A5%E5%85%B7",
        new[]
        {
            Stage("splendorous-base", "卓越工具", "iLvl 570", "贴近工匠需求的新工具", "完成前置后在水晶都开启，第一把来自卓越工具箱。"),
            Stage("splendorous-augmented", "改良型卓越工具", "iLvl 590", "诚信为本的改良计划", "交付 6.35 第一组交易资本或采集素材。"),
            Stage("splendorous-crystalline", "水晶工具", "iLvl 620", "回应思想的改良计划", "此阶段开始获得莫雯卓越工具特殊效果。"),
            Stage("splendorous-chora-zoi", "乔菈水晶工具", "iLvl 625", "开花结果的改良计划", "完成新时代新商品后开放。"),
            Stage("splendorous-brilliant", "乔菈卓绝工具", "iLvl 630", "名副其实的改良计划", "继续通过精选或交易资本获取强化素材。"),
            Stage("splendorous-vrandtic", "诺弗兰特远见工具", "iLvl 635", "晶莹剔透的改良计划", "完成决心与无与伦比的杰作后开放。"),
            Stage("splendorous-lodestar", "领航星工具", "iLvl 640", "无与伦比的改良计划", "最终阶段收藏品制作为高难度配方。"),
        });

    private static RelicWeaponSeries CreateCosmic() => new(
        "cosmic",
        "宇宙",
        "Cosmic Tools",
        "7.x 宇宙探索工具系列，通过探索任务收集研究数据推进。",
        "https://ff14.huijiwiki.com/wiki/%E5%AE%87%E5%AE%99%E5%B7%A5%E5%85%B7",
        new[]
        {
            Stage("cosmic-prototype", "原型宇宙工具v0.1", "iLvl 10", "重逢与新的计划！", "完成任务后获得原型宇宙工具箱，后续工具可在研明威处领取。"),
            Stage("cosmic-base", "宇宙工具", "iLvl 720", "初级计划研究数据", "在憧憬湾完成探索任务收集 I-IV 型研究数据。",
                Requirements(("cosmic-base-data", "初级计划研究数据", 1, "按游戏内研究任务界面推进至初级 N-9。"))),
            Stage("cosmic-spacious", "太空工具", "iLvl 750", "中级计划研究数据", "在法恩娜行星推进中级计划研究数据。",
                Requirements(("cosmic-spacious-data", "中级计划研究数据", 1, "按游戏内研究任务界面推进至中级 I-5。"))),
            Stage("cosmic-hyperspatial", "超空间工具", "iLvl 765", "高级计划研究数据", "在俄匊斯行星推进高级计划研究数据。",
                Requirements(("cosmic-hyperspatial-data", "高级计划研究数据", 1, "按游戏内研究任务界面推进至高级 A-3。"))),
            Stage("cosmic-stellar", "群星工具", "iLvl 780", "顶级计划研究数据", "在奥克塞西亚行星推进顶级计划研究数据。",
                Requirements(("cosmic-stellar-data", "顶级计划研究数据", 1, "按游戏内研究任务界面推进至顶级 E-3。"))),
        });

    private static RelicWeaponSeries CreateUltimate() => new(
        "ultimate",
        "绝武",
        "Ultimate Weapons",
        "绝境战通关奖励武器栏目，按绝境战副本记录清单与兑换物。",
        "https://ff14.huijiwiki.com/wiki/%E7%BB%9D%E5%A2%83%E6%88%98%E6%AD%A6%E5%99%A8",
        new[]
        {
            UltimateStage("ultimate-ucob", "巴哈姆特绝境战", "061832.png 巴哈姆特绝境战", "巴哈姆特绝境战图腾"),
            UltimateStage("ultimate-uwu", "究极神兵绝境战", "061832.png 究极神兵绝境战", "究极神兵绝境战图腾"),
            UltimateStage("ultimate-tea", "亚历山大绝境战", "061832.png 亚历山大绝境战", "亚历山大绝境战图腾"),
            UltimateStage("ultimate-dsr", "幻想龙诗绝境战", "061832.png 幻想龙诗绝境战", "幻想龙诗绝境战图腾"),
            Stage("ultimate-top", "欧米茄绝境验证战", "iLvl 635", "欧米茄绝境验证战", "通关欧米茄绝境验证战后，使用欧米茄图腾兑换职业武器。",
                Requirements(("ultimate-top-token", "欧米茄图腾", 1, "每把武器需要 1 个；可在拉札罕涅斯瓦孜处兑换。")),
                Tasks(("ultimate-top-clear", "完成欧米茄绝境验证战", "完成任务“晓月之终途”并通关对应绝境战。"))),
            Stage("ultimate-fru", "光暗未来绝境战", "iLvl 735", "光暗未来绝境战", "通关光暗未来绝境战后，使用巫女图腾兑换职业武器。",
                Requirements(("ultimate-fru-token", "巫女图腾", 1, "每把武器需要 1 个；可在九号解决方案瓦赫赛帕处兑换。")),
                Tasks(("ultimate-fru-clear", "完成光暗未来绝境战", "完成任务“金曦之遗辉”并通关对应绝境战。"))),
            Stage("ultimate-chaos", "妖星乱舞绝境战", "奖励武器", "妖星乱舞绝境战", "通关妖星乱舞绝境战后，使用小丑图腾兑换职业武器。",
                Requirements(("ultimate-chaos-token", "小丑图腾", 1, "每把武器需要 1 个；可在九号解决方案瓦赫赛帕处兑换。")),
                Tasks(("ultimate-chaos-clear", "完成妖星乱舞绝境战", "完成任务“金曦之遗辉”并通关对应绝境战。"))),
        });

    private static PhantomWeaponStage UltimateStage(string key, string name, string duty, string token) => Stage(
        key,
        name,
        "奖励武器",
        duty,
        "通关对应绝境战后使用图腾兑换职业武器。",
        Requirements(($"{key}-token", token, 1, "每把武器通常需要 1 个对应绝境战图腾。")),
        Tasks(($"{key}-clear", "完成对应绝境战", duty)));

    private static PhantomWeaponStage Stage(
        string key,
        string name,
        string itemLevel,
        string quest,
        string summary,
        IReadOnlyList<PhantomWeaponRequirement>? requirements = null,
        IReadOnlyList<PhantomWeaponTask>? tasks = null) => new(
            key,
            name,
            itemLevel,
            quest,
            summary,
            requirements ?? Array.Empty<PhantomWeaponRequirement>(),
            tasks ?? Array.Empty<PhantomWeaponTask>(),
            Array.Empty<PhantomWeaponReward>(),
            Array.Empty<string>());

    private static IReadOnlyList<PhantomWeaponRequirement> Requirements(params (string Key, string Name, int Needed, string Source)[] requirements) =>
        requirements.Select(requirement => new PhantomWeaponRequirement(requirement.Key, requirement.Name, requirement.Needed, requirement.Source)).ToArray();

    private static IReadOnlyList<PhantomWeaponTask> Tasks(params (string Key, string Name, string Detail)[] tasks) =>
        tasks.Select(task => new PhantomWeaponTask(task.Key, task.Name, task.Detail)).ToArray();

    private static IReadOnlyList<IReadOnlyList<string>> StageItems(params string[] names)
        => names.Select(name => (IReadOnlyList<string>)new[] { name }).ToArray();

    private static IReadOnlyList<IReadOnlyList<string>> StageItemsWithReplica(params string[] names)
    {
        var stages = StageItems(names).ToList();
        stages[^2] = stages[^2].Append(names[^1]).ToArray();
        stages.RemoveAt(stages.Count - 1);
        return stages;
    }
}
