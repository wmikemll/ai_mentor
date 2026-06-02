namespace InstantBot.Application.Services;

public static class NumerologyCalculator
{
    public static int CalculateLifePathNumber(DateOnly birthDate)
    {
        var sum = SumDigits(birthDate.Day) + SumDigits(birthDate.Month) + SumDigits(birthDate.Year);
        return ReduceToSingleDigit(sum);
    }

    public static int CalculateDestinyNumber(string fullName)
    {
        var sum = fullName.ToLowerInvariant().Where(char.IsLetter).Sum(GetPythagoreanValue);
        return ReduceToSingleDigit(sum);
    }

    public static int CalculateSoulNumber(string fullName)
    {
        const string vowels = "аеёиоуыэюяaeiouy";
        var sum = fullName.ToLowerInvariant().Where(c => vowels.Contains(c)).Sum(GetPythagoreanValue);
        return ReduceToSingleDigit(sum);
    }

    public static int ReduceToSingleDigitPublic(int n) => ReduceToSingleDigit(n);

    private static int ReduceToSingleDigit(int n)
    {
        if (n == 11 || n == 22) return n;
        while (n > 9) n = SumDigits(n);
        return n;
    }

    private static int SumDigits(int n)
    {
        var s = 0;
        while (n > 0) { s += n % 10; n /= 10; }
        return s;
    }

    private static int GetPythagoreanValue(char c) => c switch
    {
        'a' or 'j' or 's' => 1, 'b' or 'k' or 't' => 2, 'c' or 'l' or 'u' => 3,
        'd' or 'm' or 'v' => 4, 'e' or 'n' or 'w' => 5, 'f' or 'o' or 'x' => 6,
        'g' or 'p' or 'y' => 7, 'h' or 'q' or 'z' => 8, 'i' or 'r' => 9,
        'а' or 'й' or 'с' => 1, 'б' or 'к' or 'т' => 2, 'в' or 'л' or 'у' => 3,
        'г' or 'м' or 'ф' => 4, 'д' or 'н' or 'х' => 5, 'е' or 'о' or 'ц' => 6,
        'ё' or 'п' or 'ч' => 7, 'ж' or 'р' or 'ш' => 8, 'з' or 'щ' => 9,
        'и' or 'ъ' => 1, 'ы' or 'ь' => 2, 'э' => 3, 'ю' => 4, 'я' => 5,
        _ => 0
    };
}
