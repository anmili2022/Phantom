using System.Numerics;

namespace Phantom;

public sealed record BisDecodedGearSet(string Job, int JobLevel, int? SyncLevel, IReadOnlyList<uint> ItemIds);

public static class BisShareDecoder
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly int[] JobLevels = { 50, 60, 70, 80, 90, 100 };
    private static readonly uint[] SpecialGearIds = { 10337, 10338, 10339, 10340, 10341, 10342, 10343, 10344, 17726 };
    private static readonly string[] JobsV6 =
    {
        "PLD", "WAR", "DRK", "GNB", "WHM", "SCH", "AST", "SGE", "MNK", "DRG", "NIN", "SAM",
        "RPR", "VPR", "BRD", "MCH", "DNC", "BLM", "SMN", "RDM", "PCT", "BLU", "CRP", "BSM",
        "ARM", "GSM", "LTW", "WVR", "ALC", "CUL", "MIN", "BTN", "FSH",
    };

    public static BisDecodedGearSet Decode(string urlOrToken)
    {
        var token = urlOrToken.Contains('?', StringComparison.Ordinal)
            ? urlOrToken[(urlOrToken.IndexOf('?') + 1)..]
            : urlOrToken;
        token = token.Trim().TrimEnd('/');
        if (token.Length == 0) throw new FormatException("BIS URL 不包含装备分享 token。");

        var input = DecodeBase62(token);
        int Read(int range)
        {
            if (range <= 0) throw new FormatException("BIS token 包含无效范围。");
            var value = input % range;
            input /= range;
            return (int)value;
        }

        bool ReadBoolean() => Read(2) == 1;

        var version = Read(77);
        if (version < 6) throw new FormatException($"该链接为旧版分享格式，暂不支持导入，请在配装器重新生成分享链接。（编辑-分享-复制）");
        var jobIndex = Read(34);
        if (jobIndex < 0 || jobIndex >= JobsV6.Length) throw new FormatException("BIS token 中的职业无效。");
        var jobs = JobsV6;
        var synced = ReadBoolean();
        var jobLevel = synced ? JobLevels[Read(6)] : 100;
        int? syncLevel = synced ? Read(800) : null;

        var gearTypes = new List<int>();
        for (var index = 0; index < 8; index++)
        {
            if (ReadBoolean()) gearTypes.Add(index);
        }

        var minMateriaGrade = Read(13);
        var statCount = GetStatCount(jobs[jobIndex]);
        var materiaDecodeCount = 0;
        for (var grade = 12; grade >= minMateriaGrade; grade--)
        {
            if (grade == 0) continue;
            for (var stat = 0; stat < statCount; stat++)
            {
                if (ReadBoolean()) materiaDecodeCount++;
            }
        }
        if (minMateriaGrade == 0) materiaDecodeCount++;

        var itemIds = new List<uint>();
        var previousId = -1;
        var deltaRange = 0;
        var direction = 1;
        var ringsInverted = false;
        while (input != 0)
        {
            if (gearTypes.Count == 0) throw new FormatException("BIS token 未定义装备编码类型。");
            var gearType = gearTypes[Read(gearTypes.Count)];
            if (gearType == 6)
            {
                itemIds.Add(SpecialGearIds[Read(9)]);
                continue;
            }

            if (gearType == 7)
            {
                for (var stat = 0; stat < statCount; stat++) _ = Read(1001);
            }
            else
            {
                if (gearType > 0 && materiaDecodeCount == 0)
                    throw new FormatException("BIS token 的魔晶石编码表为空。");
                for (var materia = 0; materia < gearType; materia++) _ = Read(materiaDecodeCount);
            }

            if (previousId < 0)
            {
                previousId = Read(60000);
                deltaRange = Read(Math.Max(1, previousId));
                direction = ReadBoolean() ? 1 : -1;
                ringsInverted = ReadBoolean();
            }
            else
            {
                var encodedDelta = Read(Math.Max(1, deltaRange));
                previousId += (encodedDelta - (input == 0 ? 1 : 0)) * direction;
            }

            if (previousId > 0) itemIds.Add((uint)previousId);
        }

        if (ringsInverted != (direction == -1)) itemIds.Reverse();
        return new BisDecodedGearSet(jobs[jobIndex], jobLevel, syncLevel, itemIds.ToArray());
    }

    private static BigInteger DecodeBase62(string token)
    {
        var value = BigInteger.Zero;
        foreach (var character in token)
        {
            var index = Alphabet.IndexOf(character);
            if (index < 0) throw new FormatException($"BIS token 包含无效字符“{character}”。");
            value = value * 62 + index;
        }
        return value;
    }

    private static int GetStatCount(string job)
        => job switch
        {
            "PLD" or "WAR" or "DRK" or "GNB" or "WHM" or "SCH" or "AST" or "SGE" => 5,
            "CRP" or "BSM" or "ARM" or "GSM" or "LTW" or "WVR" or "ALC" or "CUL" or "MIN" or "BTN" or "FSH" => 3,
            _ => 4,
        };
}
