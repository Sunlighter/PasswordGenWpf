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

    public class AlphabetSpec
    {
        public AlphabetSpec
        (
            int uppercaseWeight,
            int lowercaseWeight,
            int numberWeight,
            int symbolPunctWeight,
            string alsoInclude,
            int alsoIncludeWeight,
            string exclude
            
        )
        {
            UppercaseWeight = uppercaseWeight;
            LowercaseWeight = lowercaseWeight;
            NumberWeight = numberWeight;
            SymbolPunctWeight = symbolPunctWeight;
            AlsoInclude = alsoInclude;
            AlsoIncludeWeight = alsoIncludeWeight;
            Exclude = exclude;
        }

        public int UppercaseWeight { get; }
        public int LowercaseWeight { get; }
        public int NumberWeight { get; }
        public int SymbolPunctWeight { get; }
        public string AlsoInclude { get; }
        public int AlsoIncludeWeight { get; }
        public string Exclude { get; }
    }

    public class Alphabet
    {
        private readonly SortedDictionary<char, int> chars;
        private readonly int weightedSize;

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
        }

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
    }
}
