using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevClicker.Core.Utilities
{
    public static class NumberFormatter
    {
        private static readonly Dictionary<long, string> _formatCache = new Dictionary<long, string>();
        private const int MAX_CACHE_SIZE = 1000;

        private static readonly string[] _shortSuffixes = 
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc", 
            "Ud", "Dd", "Td", "Qad", "Qid", "Sxd", "Spd", "Ocd", "Nod", "Vg",
            "Uvg", "Dvg", "Tvg", "Qavg", "Qivg", "Sxvg", "Spvg", "Ocvg", "Novg", "Tg"
        };

        private static readonly string[] _longSuffixes = 
        {
            "", "Thousand", "Million", "Billion", "Trillion", "Quadrillion", "Quintillion", 
            "Sextillion", "Septillion", "Octillion", "Nonillion", "Decillion",
            "Undecillion", "Duodecillion", "Tredecillion", "Quattuordecillion", "Quindecillion",
            "Sexdecillion", "Septendecillion", "Octodecillion", "Novemdecillion", "Vigintillion"
        };

        public static string Format(long number, bool useShortFormat = true, int decimalPlaces = 2)
        {
            if (number == 0) return "0";
            
            if (_formatCache.TryGetValue(number, out string cached))
                return cached;

            string formatted = FormatInternal(number, useShortFormat, decimalPlaces);
            
            if (_formatCache.Count < MAX_CACHE_SIZE)
                _formatCache[number] = formatted;
            
            return formatted;
        }

        public static string Format(float number, bool useShortFormat = true, int decimalPlaces = 2)
        {
            return Format((long)number, useShortFormat, decimalPlaces);
        }

        public static string Format(double number, bool useShortFormat = true, int decimalPlaces = 2)
        {
            return Format((long)number, useShortFormat, decimalPlaces);
        }

        private static string FormatInternal(long number, bool useShortFormat, int decimalPlaces)
        {
            if (number < 0)
                return "-" + FormatInternal(-number, useShortFormat, decimalPlaces);

            if (number < 1000)
                return number.ToString();

            int suffixIndex = 0;
            double displayNumber = number;

            while (displayNumber >= 1000 && suffixIndex < _shortSuffixes.Length - 1)
            {
                displayNumber /= 1000;
                suffixIndex++;
            }

            string[] suffixes = useShortFormat ? _shortSuffixes : _longSuffixes;
            
            if (suffixIndex >= suffixes.Length)
            {
                return number.ToString("E2");
            }

            string suffix = suffixes[suffixIndex];
            
            if (displayNumber >= 100)
                decimalPlaces = 0;
            else if (displayNumber >= 10)
                decimalPlaces = Math.Min(decimalPlaces, 1);

            string format = decimalPlaces > 0 ? $"F{decimalPlaces}" : "F0";
            string formattedNumber = displayNumber.ToString(format);

            return $"{formattedNumber}{suffix}";
        }

        public static string FormatWithCommas(long number)
        {
            return number.ToString("N0");
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 60)
                return $"{seconds:F1}s";
            
            if (seconds < 3600)
            {
                int minutes = (int)(seconds / 60);
                int remainingSeconds = (int)(seconds % 60);
                return $"{minutes}m {remainingSeconds}s";
            }
            
            if (seconds < 86400)
            {
                int hours = (int)(seconds / 3600);
                int minutes = (int)((seconds % 3600) / 60);
                return $"{hours}h {minutes}m";
            }
            
            int days = (int)(seconds / 86400);
            int remainingHours = (int)((seconds % 86400) / 3600);
            return $"{days}d {remainingHours}h";
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.TotalSeconds:F1}s";
            
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h";
        }

        public static string FormatPercentage(float percentage, int decimalPlaces = 1)
        {
            return $"{percentage.ToString($"F{decimalPlaces}")}%";
        }

        public static string FormatCurrency(long amount, string currencySymbol = "$")
        {
            return $"{currencySymbol}{Format(amount)}";
        }

        public static string FormatRate(long amount, string unit = "/sec")
        {
            return $"{Format(amount)}{unit}";
        }

        public static string FormatMultiplier(float multiplier, int decimalPlaces = 2)
        {
            return $"x{multiplier.ToString($"F{decimalPlaces}")}";
        }

        public static void ClearCache()
        {
            _formatCache.Clear();
        }

        public static bool TryParseFormattedNumber(string formattedNumber, out long result)
        {
            result = 0;
            
            if (string.IsNullOrEmpty(formattedNumber))
                return false;

            formattedNumber = formattedNumber.Trim().ToUpper();
            
            if (long.TryParse(formattedNumber, out result))
                return true;

            for (int i = 1; i < _shortSuffixes.Length; i++)
            {
                string suffix = _shortSuffixes[i].ToUpper();
                if (formattedNumber.EndsWith(suffix))
                {
                    string numberPart = formattedNumber.Substring(0, formattedNumber.Length - suffix.Length);
                    if (double.TryParse(numberPart, out double baseNumber))
                    {
                        result = (long)(baseNumber * Math.Pow(1000, i));
                        return true;
                    }
                }
            }

            return false;
        }
    }
}