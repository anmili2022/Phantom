using Lumina.Excel.Sheets;
using System.Text;

namespace Phantom;

public sealed class AchievementWeaponHistory
{
    private const string ResourceName = "Phantom.Achievements";
    private static readonly IReadOnlyDictionary<(string SeriesKey, string JobKey, int StageIndex), IReadOnlyList<uint>> AchievementIds = LoadMappings();

    public static bool IsStageOwned(string seriesKey, string jobKey, int stageIndex)
    {
        if (!AchievementIds.TryGetValue((seriesKey, jobKey, stageIndex), out var ids))
        {
            return false;
        }

        var achievements = DalamudApi.DataManager.GetExcelSheet<Achievement>();
        return ids.Any(id => achievements.TryGetRow(id, out var achievement)
            && DalamudApi.UnlockState.IsAchievementComplete(achievement));
    }

    private static IReadOnlyDictionary<(string SeriesKey, string JobKey, int StageIndex), IReadOnlyList<uint>> LoadMappings()
    {
        using var stream = typeof(AchievementWeaponHistory).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var rows = ParseCsv(reader.ReadToEnd());
        if (rows.Count == 0)
        {
            return new Dictionary<(string, string, int), IReadOnlyList<uint>>();
        }

        var header = rows[0];
        var sheetIndex = header.IndexOf("Sheet");
        var columnIndexes = header
            .Select((name, index) => (name, index))
            .Where(entry => entry.name.StartsWith("Column", StringComparison.Ordinal))
            .Select(entry => entry.index)
            .ToArray();
        var achievementsByName = DalamudApi.DataManager.GetExcelSheet<Achievement>()
            .Where(achievement => achievement.RowId != 0 && !string.IsNullOrWhiteSpace(achievement.Name.ExtractText()))
            .GroupBy(achievement => NormalizeName(achievement.Name.ExtractText()), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(achievement => achievement.RowId).ToArray(), StringComparer.Ordinal);
        var mappings = new Dictionary<(string, string, int), IReadOnlyList<uint>>();

        foreach (var sheetRows in rows.Skip(1).GroupBy(row => Cell(row, sheetIndex), StringComparer.Ordinal))
        {
            var seriesKey = GetSeriesKey(sheetRows.Key);
            if (seriesKey == null)
            {
                continue;
            }

            var titleRow = sheetRows.FirstOrDefault(row => string.IsNullOrWhiteSpace(Cell(row, columnIndexes[0])));
            if (titleRow == null)
            {
                continue;
            }

            var stageIndex = 0;
            foreach (var row in sheetRows.Where(row => !string.IsNullOrWhiteSpace(Cell(row, columnIndexes[0]))))
            {
                foreach (var columnIndex in columnIndexes.Skip(1))
                {
                    var jobKey = GetJobKey(Cell(titleRow, columnIndex));
                    var achievementName = Cell(row, columnIndex);
                    if (jobKey == null || string.IsNullOrWhiteSpace(achievementName) || achievementName == "无对应成就")
                    {
                        continue;
                    }

                    if (achievementsByName.TryGetValue(NormalizeName(achievementName), out var ids))
                    {
                        mappings[(seriesKey, jobKey, stageIndex)] = ids;
                    }
                }

                stageIndex++;
            }
        }

        return mappings;
    }

    private static string? GetSeriesKey(string sheet)
        => sheet switch
        {
            "3.0 元灵" => "anima",
            "4.0 ulk" => "eureka",
            "5.0 义军" => "resistance",
            "6.0 曼德维尔" => "manderville",
            "7.0 幻境" => "phantom",
            "5.0 天钢" => "skysteel",
            "6.0 卓越" => "splendorous",
            "7.0 宇宙" => "cosmic",
            _ => null,
        };

    private static string? GetJobKey(string name)
        => name switch
        {
            "骑士" => "pld", "战士" => "war", "黑骑" or "暗黑骑士" => "drk", "绝枪" or "绝枪战士" => "gnb",
            "白魔" or "白魔法师" => "whm", "学者" => "sch", "占星" or "占星术士" => "ast", "贤者" => "sge",
            "武僧" => "mnk", "龙骑" or "龙骑士" => "drg", "镰刀" or "钐镰客" => "rpr", "武士" => "sam",
            "忍者" => "nin", "蛇剑" or "蝰蛇剑士" => "vpr", "诗人" or "吟游诗人" => "brd", "机工" or "机工士" => "mch",
            "舞者" => "dnc", "黑魔" or "黑魔法师" => "blm", "召唤" or "召唤师" => "smn", "赤魔" or "赤魔法师" => "rdm",
            "画家" or "绘灵法师" => "pct", "刻木" or "刻木匠" => "crp", "锻铁" or "锻铁匠" => "bsm",
            "铸甲" or "铸甲匠" => "arm", "雕金" or "雕金匠" => "gsm", "制革" or "制革匠" => "ltw",
            "裁衣" or "裁衣匠" => "wvr", "炼金" or "炼金术士" => "alc", "烹调" or "烹调师" => "cul",
            "采矿" or "采矿工" => "min", "园艺" or "园艺工" => "btn", "捕鱼" or "捕鱼人" => "fsh",
            _ => null,
        };

    private static string NormalizeName(string name) => name.Trim().Replace('・', '·');

    private static string Cell(IReadOnlyList<string> row, int index)
        => index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;

    private static List<string[]> ParseCsv(string content)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (character == '"')
            {
                if (quoted && index + 1 < content.Length && content[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if ((character == '\r' || character == '\n') && !quoted)
            {
                if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n') index++;
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row.ToArray());
                row.Clear();
            }
            else field.Append(character);
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row.ToArray());
        }

        return rows;
    }
}
