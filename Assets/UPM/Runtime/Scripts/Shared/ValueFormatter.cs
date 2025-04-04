using System;
using System.Globalization;

public static class ValueFormatter
{
    private static readonly string kFormat = "{0} {1}";

    private static readonly string[] sizeUnits = { "b", "kB", "MB", "GB", "TB" };

    private static readonly string[] countUnits = { "k", "M" };

    public static string PrettyBytes(long value)
    {
        if (value < 0)
        {
            return value.ToString();
        }

        if (value == 0)
        {
            return string.Format(kFormat, 0, sizeUnits[0]);
        }

        int number = (int)Math.Floor(Math.Log(value) / Math.Log(1024));
        double result = value / Math.Pow(1024, number);

        var a = result.ToString("F2", CultureInfo.InvariantCulture);
        var b = sizeUnits[number];
        return string.Format(kFormat, a, b);
    }

    public static string PrettyCount(long value)
    {
        if (value < 1000)
        {
            return value.ToString();
        }

        int unitIndex = (int)Math.Floor(Math.Log10(value) / 3);
        double scaledValue = value / Math.Pow(1000, unitIndex);

        var a = scaledValue.ToString("F2", CultureInfo.InvariantCulture);
        var b = countUnits[unitIndex - 1];

        return string.Format(kFormat, a, b);
    }
}
