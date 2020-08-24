using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;

namespace PasswordGenWpf
{
    public class RandomGen : IDisposable
    {
        private RandomNumberGenerator rng;
        private BigInteger randomValue;
        private BigInteger entropy;

        public RandomGen()
        {
            rng = RandomNumberGenerator.Create();
            Reset();
        }

        private void Reset()
        {
            entropy = BigInteger.One << 256;
            byte[] b0 = new byte[33];
            rng.GetBytes(b0, 0, 32);
            randomValue = new BigInteger(b0);
        }

        public int NextInt32(int maxPlusOne)
        {
            if (maxPlusOne <= 0) throw new ArgumentException($"Value {nameof(maxPlusOne)} must be at least 1");
            if (maxPlusOne == 1) return 0;

            restart:

            BigInteger randomRemainder;
            BigInteger randomQuotient = BigInteger.DivRem(randomValue, maxPlusOne, out randomRemainder);

            BigInteger entropyRemainder;
            BigInteger entropyQuotient = BigInteger.DivRem(entropy, maxPlusOne, out entropyRemainder);

            if (randomQuotient >= entropyQuotient)
            {
                Reset();
                goto restart;
            }

            entropy = entropyQuotient;
            randomValue = randomQuotient;

            return (int)randomRemainder;
        }

        public void Dispose()
        {
            rng.Dispose();
        }
    }

    public enum DakutenStatus
    {
        Prohibited = 0,
        Allowed = 1,
        Required = 2
    }

    public class AlphabetSpec
    {
        public AlphabetSpec
        (
            int uppercaseWeight,
            int lowercaseWeight,
            int numberWeight,
            int hiraganaWeight,
            bool hiraganaHe,
            DakutenStatus hiraganaDakutenStatus,
            int katakanaWeight,
            bool katakanaHe,
            DakutenStatus katakanaDakutenStatus,
            int symbolPunctWeight,
            string alsoInclude,
            int alsoIncludeWeight,
            string exclude
            
        )
        {
            UppercaseWeight = uppercaseWeight;
            LowercaseWeight = lowercaseWeight;
            NumberWeight = numberWeight;
            HiraganaWeight = hiraganaWeight;
            HiraganaHe = hiraganaHe;
            HiraganaDakutenStatus = hiraganaDakutenStatus;
            KatakanaWeight = katakanaWeight;
            KatakanaHe = katakanaHe;
            KatakanaDakutenStatus = katakanaDakutenStatus;
            SymbolPunctWeight = symbolPunctWeight;
            AlsoInclude = alsoInclude;
            AlsoIncludeWeight = alsoIncludeWeight;
            Exclude = exclude;
        }

        public int UppercaseWeight { get; }
        public int LowercaseWeight { get; }
        public int NumberWeight { get; }
        public int HiraganaWeight { get; }
        public bool HiraganaHe { get; }
        public DakutenStatus HiraganaDakutenStatus { get; }
        public int KatakanaWeight { get; }
        public bool KatakanaHe { get; }
        public DakutenStatus KatakanaDakutenStatus { get; }
        public int SymbolPunctWeight { get; }
        public string AlsoInclude { get; }
        public int AlsoIncludeWeight { get; }
        public string Exclude { get; }
    }

    public class Alphabet
    {
        private readonly SortedDictionary<char, int> chars;
        private readonly int weightedSize;
        private readonly double averageBitsPerCharacter;

        public Alphabet(AlphabetSpec spec)
        {
            chars = new SortedDictionary<char, int>();

            Action<int, string> addSet = delegate (int weight, string set)
            {
                if (weight > 0)
                {
                    foreach (char ch in set)
                    {
                        if (chars.ContainsKey(ch))
                        {
                            chars[ch] += weight;
                        }
                        else
                        {
                            chars.Add(ch, weight);
                        }
                    }
                }
            };

            addSet(spec.UppercaseWeight, characterSets.Value.Uppercase);
            addSet(spec.LowercaseWeight, characterSets.Value.Lowercase);
            addSet(spec.NumberWeight, characterSets.Value.Digits);
            if (spec.HiraganaWeight > 0)
            {
                string hiragana =
                    (spec.HiraganaDakutenStatus.AllowsDakutenNo() ? (characterSets.Value.HiraganaNoDakuten + (spec.HiraganaHe ? characterSets.Value.HiraganaHeNoDakuten : "")) : "") +
                    (spec.HiraganaDakutenStatus.AllowsDakutenYes() ? (characterSets.Value.HiraganaDakuten + (spec.HiraganaHe ? characterSets.Value.HiraganaHeDakuten : "")) : "");

                addSet(spec.HiraganaWeight, hiragana);
            }
            if (spec.KatakanaWeight > 0)
            {
                string katakana =
                    (spec.KatakanaDakutenStatus.AllowsDakutenNo() ? (characterSets.Value.KatakanaNoDakuten + (spec.HiraganaHe ? characterSets.Value.KatakanaHeNoDakuten : "")) : "") +
                    (spec.KatakanaDakutenStatus.AllowsDakutenYes() ? (characterSets.Value.KatakanaDakuten + (spec.HiraganaHe ? characterSets.Value.KatakanaHeDakuten : "")) : "");
                addSet(spec.KatakanaWeight, katakana);
            }
            addSet(spec.SymbolPunctWeight, characterSets.Value.SymbolPunct);
            addSet(spec.AlsoIncludeWeight, spec.AlsoInclude);
            
            foreach(char ch in spec.Exclude ?? "")
            {
                if (chars.ContainsKey(ch))
                {
                    chars.Remove(ch);
                }
            }

            weightedSize = chars.Select(kvp => kvp.Value).Sum();

            averageBitsPerCharacter = GetAverageBitsPerCharacter();
        }

        private double GetAverageBitsPerCharacter()
        {
            double bits = 0.0;
            double charCount = 0.0;
            double recipLog2 = 1.0 / Math.Log(2.0);

            foreach(KeyValuePair<char, int> kvp in chars)
            {
                charCount += kvp.Value;
            }

            foreach(KeyValuePair<char, int> kvp in chars)
            {
                bits += kvp.Value * (Math.Log(charCount / kvp.Value) * recipLog2);
            }

            return bits / charCount;
        }

        public double AverageBitsPerCharacter { get { return averageBitsPerCharacter; } }

        private class CharacterSets
        {
            public CharacterSets()
            {
                StringBuilder uppers = new StringBuilder();
                StringBuilder lowers = new StringBuilder();
                StringBuilder digits = new StringBuilder();
                StringBuilder symbolPunct = new StringBuilder();

                for (int i = 33; i < 127; ++i)
                {
                    char ch = (char)i;
                    if (char.IsUpper(ch))
                    {
                        uppers.Append(ch);
                    }
                    else if (char.IsLower(ch))
                    {
                        lowers.Append(ch);
                    }
                    else if (char.IsDigit(ch))
                    {
                        digits.Append(ch);
                    }
                    else
                    {
                        symbolPunct.Append(ch);
                    }

                    Uppercase = uppers.ToString();
                    Lowercase = lowers.ToString();
                    Digits = digits.ToString();
                    SymbolPunct = symbolPunct.ToString();
                }
            }

            public string Uppercase { get; }
            public string Lowercase { get; }
            public string Digits { get; }
            public string SymbolPunct { get; }

            public string HiraganaNoDakuten { get; } = "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふほまみむめもやゆよらりるれろわを";
            public string HiraganaHeNoDakuten { get; } = "へ";
            public string HiraganaDakuten { get; } = "がぎぐげござじずぜぞだぢづでどばびぶぼぱぴぷぽ";
            public string HiraganaHeDakuten { get; } = "べぺ";

            public string KatakanaNoDakuten { get; } = "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフホマミムメモヤユヨラリルレロワヲ";
            public string KatakanaHeNoDakuten { get; } = "ヘ";
            public string KatakanaDakuten { get; } = "ガギグゲゴザジズゼゾダヂヅデドバビブボパピプポ";
            public string KatakanaHeDakuten { get; } = "ベペ";
        }

        private static Lazy<CharacterSets> characterSets = new Lazy<CharacterSets>(LazyThreadSafetyMode.ExecutionAndPublication);

        public int WeightedSize
        {
            get
            {
                return weightedSize;
            }
        }

        public char GetChar(int index)
        {
            if (index < 0) throw new IndexOutOfRangeException();

            foreach(KeyValuePair<char, int> kvp in chars)
            {
                if (index < kvp.Value)
                {
                    return kvp.Key;
                }
                else
                {
                    index -= kvp.Value;
                }
            }

            throw new IndexOutOfRangeException();
        }
    }

    public static class Utility
    {
        public static string GeneratePassword(Alphabet a, RandomGen random, int length)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < length; ++i)
            {
                sb.Append(a.GetChar(random.NextInt32(a.WeightedSize)));
            }
            return sb.ToString();
        }

        public static bool AllowsDakutenYes(this DakutenStatus ds)
        {
            return ds == DakutenStatus.Allowed || ds == DakutenStatus.Required;
        }

        public static bool AllowsDakutenNo(this DakutenStatus ds)
        {
            return ds == DakutenStatus.Allowed || ds == DakutenStatus.Prohibited;
        }
    }
}
