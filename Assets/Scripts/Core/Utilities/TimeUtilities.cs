using System;
using UnityEngine;

namespace GameDevClicker.Core.Utilities
{
    public static class TimeUtilities
    {
        private static DateTime _sessionStartTime;
        private static float _lastFrameTime;
        private static bool _isInitialized = false;

        public static DateTime SessionStartTime => _sessionStartTime;
        public static TimeSpan SessionDuration => DateTime.Now - _sessionStartTime;
        public static float DeltaTime => Time.deltaTime;
        public static float UnscaledDeltaTime => Time.unscaledDeltaTime;
        public static float TimeSinceStartup => Time.time;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (!_isInitialized)
            {
                _sessionStartTime = DateTime.Now;
                _lastFrameTime = Time.time;
                _isInitialized = true;
                Debug.Log($"[TimeUtilities] Initialized at {_sessionStartTime}");
            }
        }

        public static string FormatDuration(TimeSpan duration)
        {
            return NumberFormatter.FormatTimeSpan(duration);
        }

        public static string FormatDuration(float seconds)
        {
            return NumberFormatter.FormatTime(seconds);
        }

        public static TimeSpan CalculateOfflineTime(DateTime lastPlayTime)
        {
            DateTime now = DateTime.Now;
            TimeSpan offlineTime = now - lastPlayTime;
            
            return offlineTime.TotalSeconds > 0 ? offlineTime : TimeSpan.Zero;
        }

        public static bool HasBeenOfflineFor(DateTime lastPlayTime, TimeSpan minimumTime)
        {
            TimeSpan offlineTime = CalculateOfflineTime(lastPlayTime);
            return offlineTime >= minimumTime;
        }

        public static DateTime GetTodayStart()
        {
            DateTime now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
        }

        public static DateTime GetTomorrowStart()
        {
            return GetTodayStart().AddDays(1);
        }

        public static TimeSpan TimeUntilTomorrow()
        {
            return GetTomorrowStart() - DateTime.Now;
        }

        public static bool IsToday(DateTime dateTime)
        {
            DateTime today = GetTodayStart();
            return dateTime >= today && dateTime < today.AddDays(1);
        }

        public static bool IsYesterday(DateTime dateTime)
        {
            DateTime yesterday = GetTodayStart().AddDays(-1);
            return dateTime >= yesterday && dateTime < GetTodayStart();
        }

        public static int GetDaysSince(DateTime dateTime)
        {
            return (int)(DateTime.Now.Date - dateTime.Date).TotalDays;
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static long DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeStampInTicks = (dateTime.ToUniversalTime() - unixStart).Ticks;
            return unixTimeStampInTicks / TimeSpan.TicksPerSecond;
        }

        public static bool ValidateDateTime(DateTime dateTime)
        {
            return dateTime >= DateTime.MinValue && dateTime <= DateTime.MaxValue && dateTime <= DateTime.Now.AddMinutes(5);
        }

        public static DateTime SafeDateTime(DateTime dateTime)
        {
            if (!ValidateDateTime(dateTime))
            {
                Debug.LogWarning($"[TimeUtilities] Invalid DateTime detected: {dateTime}. Using current time instead.");
                return DateTime.Now;
            }
            return dateTime;
        }

        public static float GetFPSSmoothed(float smoothing = 0.9f)
        {
            float currentFPS = 1f / Time.deltaTime;
            _lastFrameTime = Mathf.Lerp(currentFPS, _lastFrameTime, smoothing);
            return _lastFrameTime;
        }

        public static float GetFrameTime()
        {
            return Time.deltaTime * 1000f; // in milliseconds
        }

        public static bool IsTimeWithinRange(DateTime time, DateTime start, DateTime end)
        {
            return time >= start && time <= end;
        }

        public static DateTime RoundToNearest(DateTime dateTime, TimeSpan interval)
        {
            long ticks = (dateTime.Ticks + (interval.Ticks / 2) + 1) / interval.Ticks;
            return new DateTime(ticks * interval.Ticks);
        }

        public static string GetRelativeTimeString(DateTime dateTime)
        {
            TimeSpan timeDiff = DateTime.Now - dateTime;
            
            if (timeDiff.TotalSeconds < 1)
                return "Just now";
            
            if (timeDiff.TotalSeconds < 60)
                return $"{(int)timeDiff.TotalSeconds} seconds ago";
            
            if (timeDiff.TotalMinutes < 60)
                return $"{(int)timeDiff.TotalMinutes} minutes ago";
            
            if (timeDiff.TotalHours < 24)
                return $"{(int)timeDiff.TotalHours} hours ago";
            
            if (timeDiff.TotalDays < 7)
                return $"{(int)timeDiff.TotalDays} days ago";
            
            if (timeDiff.TotalDays < 30)
                return $"{(int)(timeDiff.TotalDays / 7)} weeks ago";
            
            if (timeDiff.TotalDays < 365)
                return $"{(int)(timeDiff.TotalDays / 30)} months ago";
            
            return $"{(int)(timeDiff.TotalDays / 365)} years ago";
        }

        public static float CalculateTimeDilation(float baseTime, float speedMultiplier)
        {
            return baseTime / Mathf.Max(0.1f, speedMultiplier);
        }

        public static bool IsLeapYear(int year)
        {
            return DateTime.IsLeapYear(year);
        }

        public static int GetDaysInMonth(int year, int month)
        {
            return DateTime.DaysInMonth(year, month);
        }

        public static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
        {
            DateTime today = DateTime.Today;
            int daysUntilWeekday = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilWeekday == 0)
                daysUntilWeekday = 7;
            return today.AddDays(daysUntilWeekday);
        }

        public static void ResetSession()
        {
            _sessionStartTime = DateTime.Now;
            Debug.Log($"[TimeUtilities] Session reset at {_sessionStartTime}");
        }

        public static string FormatTimeRemaining(float totalSeconds, float currentSeconds)
        {
            float remaining = totalSeconds - currentSeconds;
            return FormatDuration(remaining);
        }

        public static float GetProgressPercentage(float totalSeconds, float currentSeconds)
        {
            if (totalSeconds <= 0) return 100f;
            return Mathf.Clamp01(currentSeconds / totalSeconds) * 100f;
        }
    }
}