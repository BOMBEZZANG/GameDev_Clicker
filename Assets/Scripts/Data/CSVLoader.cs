using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GameDevClicker.Data
{
    public class CSVLoader : MonoBehaviour
    {
        private static CSVLoader _instance;
        public static CSVLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Check if one already exists in the scene
                    _instance = FindObjectOfType<CSVLoader>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[CSVLoader]");
                        _instance = go.AddComponent<CSVLoader>();
                        DontDestroyOnLoad(go);
                        Debug.Log("[CSVLoader] Instance created");
                    }
                }
                return _instance;
            }
        }

        private Dictionary<string, List<Dictionary<string, string>>> _cachedData = new Dictionary<string, List<Dictionary<string, string>>>();

        public BalanceData LoadedBalanceData { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[CSVLoader] Duplicate instance found, destroying...");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CSVLoader] Awake - Instance set");
        }

        public void LoadAllCSVData()
        {
            Debug.Log("[CSVLoader] Starting to load all CSV data...");

            LoadedBalanceData = new BalanceData();

            LoadCSVFile("Upgrades");
            LoadCSVFile("Levels");
            LoadCSVFile("Projects");
            LoadCSVFile("Stage");
            LoadCSVFile("DualCurrency");
            LoadCSVFile("ExpectedTime");

            ParseUpgradesData();
            ParseLevelsData();
            ParseProjectsData();
            ParseStageData();
            ParseDualCurrencyData();

            Debug.Log("[CSVLoader] All CSV data loaded successfully!");
        }

        private void LoadCSVFile(string fileName)
        {
            string filePath = Path.Combine(Application.dataPath, "GameData", "CSV", "Balancing", fileName + ".csv");
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[CSVLoader] CSV file not found: {filePath}");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    Debug.LogWarning($"[CSVLoader] CSV file {fileName} is empty or has no data");
                    return;
                }

                string[] headers = ParseCSVLine(lines[0]);
                List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length != headers.Length)
                    {
                        Debug.LogWarning($"[CSVLoader] Skipping malformed line {i} in {fileName}");
                        continue;
                    }

                    Dictionary<string, string> row = new Dictionary<string, string>();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        row[headers[j]] = values[j];
                    }
                    data.Add(row);
                }

                _cachedData[fileName] = data;
                Debug.Log($"[CSVLoader] Loaded {data.Count} rows from {fileName}.csv");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CSVLoader] Error loading CSV file {fileName}: {e.Message}");
            }
        }

        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            result.Add(currentField);
            return result.ToArray();
        }

        private void ParseUpgradesData()
        {
            if (!_cachedData.ContainsKey("Upgrades")) return;

            LoadedBalanceData.Upgrades = new List<UpgradeInfo>();

            foreach (var row in _cachedData["Upgrades"])
            {
                UpgradeInfo upgrade = new UpgradeInfo
                {
                    upgradeId = row["upgrade_id"],
                    category = row["category"],
                    nameEn = row["name_en"],
                    nameKo = row["name_ko"],
                    descriptionEn = row["description_en"],
                    descriptionKo = row["description_ko"],
                    currencyType = row["currency_type"],
                    basePrice = ParseFloat(row["base_price"]),
                    priceMultiplier = ParseFloat(row["price_multiplier"]),
                    effectType = row["effect_type"],
                    effectValue = ParseFloat(row["effect_value"]),
                    maxLevel = ParseInt(row["max_level"]),
                    unlockCondition = row["unlock_condition"]
                };

                LoadedBalanceData.Upgrades.Add(upgrade);
            }

            Debug.Log($"[CSVLoader] Parsed {LoadedBalanceData.Upgrades.Count} upgrades");
        }

        private void ParseLevelsData()
        {
            if (!_cachedData.ContainsKey("Levels")) return;

            LoadedBalanceData.Levels = new List<LevelInfo>();

            foreach (var row in _cachedData["Levels"])
            {
                LevelInfo level = new LevelInfo
                {
                    level = ParseInt(row["level"]),
                    requiredExp = ParseLong(row["required_exp"]),
                    moneyMultiplier = ParseFloat(row["money_multiplier"]),
                    unlockFeature = row["unlock_feature"],
                    bonusReward = ParseLong(row["bonus_reward"]),
                    specialEvent = row["special_event"]
                };

                LoadedBalanceData.Levels.Add(level);
            }

            Debug.Log($"[CSVLoader] Parsed {LoadedBalanceData.Levels.Count} levels");
        }

        private void ParseProjectsData()
        {
            if (!_cachedData.ContainsKey("Projects")) return;

            LoadedBalanceData.Projects = new List<ProjectInfo>();

            foreach (var row in _cachedData["Projects"])
            {
                ProjectInfo project = new ProjectInfo
                {
                    projectId = row["project_id"],
                    stage = ParseInt(row["stage"]),
                    nameEn = row["name_en"],
                    nameKo = row["name_ko"],
                    requiredExp = ParseLong(row["required_exp"]),
                    baseReward = ParseLong(row["base_reward"]),
                    completionTime = ParseFloat(row["completion_time"]),
                    streakBonus = ParseFloat(row["streak_bonus"])
                };

                LoadedBalanceData.Projects.Add(project);
            }

            Debug.Log($"[CSVLoader] Parsed {LoadedBalanceData.Projects.Count} projects");
        }

        private void ParseStageData()
        {
            if (!_cachedData.ContainsKey("Stage")) return;

            LoadedBalanceData.Stages = new List<StageInfo>();

            foreach (var row in _cachedData["Stage"])
            {
                StageInfo stage = new StageInfo
                {
                    stage = ParseInt(row.ContainsKey("stage") ? row["stage"] : "0"),
                    nameEn = row.ContainsKey("name_en") ? row["name_en"] : "",
                    nameKo = row.ContainsKey("name_ko") ? row["name_ko"] : "",
                    requiredLevel = ParseInt(row.ContainsKey("required_level") ? row["required_level"] : "0"),
                    unlockFeatures = row.ContainsKey("unlock_features") ? row["unlock_features"] : ""
                };

                LoadedBalanceData.Stages.Add(stage);
            }

            Debug.Log($"[CSVLoader] Parsed {LoadedBalanceData.Stages.Count} stages");
        }

        private void ParseDualCurrencyData()
        {
            if (!_cachedData.ContainsKey("DualCurrency")) return;

            LoadedBalanceData.DualCurrencySettings = new List<DualCurrencyInfo>();

            foreach (var row in _cachedData["DualCurrency"])
            {
                DualCurrencyInfo dualCurrency = new DualCurrencyInfo
                {
                    level = ParseInt(row.ContainsKey("level") ? row["level"] : "0"),
                    expRate = ParseFloat(row.ContainsKey("exp_rate") ? row["exp_rate"] : "1"),
                    moneyRate = ParseFloat(row.ContainsKey("money_rate") ? row["money_rate"] : "1"),
                    conversionRatio = ParseFloat(row.ContainsKey("conversion_ratio") ? row["conversion_ratio"] : "1")
                };

                LoadedBalanceData.DualCurrencySettings.Add(dualCurrency);
            }

            Debug.Log($"[CSVLoader] Parsed {LoadedBalanceData.DualCurrencySettings.Count} dual currency settings");
        }

        private float ParseFloat(string value)
        {
            if (float.TryParse(value, out float result))
                return result;
            return 0f;
        }

        private int ParseInt(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        private long ParseLong(string value)
        {
            if (long.TryParse(value, out long result))
                return result;
            return 0;
        }

        public UpgradeInfo GetUpgradeById(string upgradeId)
        {
            return LoadedBalanceData?.Upgrades?.FirstOrDefault(u => u.upgradeId == upgradeId);
        }

        public LevelInfo GetLevelInfo(int level)
        {
            return LoadedBalanceData?.Levels?.FirstOrDefault(l => l.level == level);
        }

        public ProjectInfo GetProjectById(string projectId)
        {
            return LoadedBalanceData?.Projects?.FirstOrDefault(p => p.projectId == projectId);
        }

        public List<ProjectInfo> GetProjectsByStage(int stage)
        {
            return LoadedBalanceData?.Projects?.Where(p => p.stage == stage).ToList() ?? new List<ProjectInfo>();
        }

        public StageInfo GetStageInfo(int stage)
        {
            return LoadedBalanceData?.Stages?.FirstOrDefault(s => s.stage == stage);
        }
    }

    [Serializable]
    public class BalanceData
    {
        public List<UpgradeInfo> Upgrades;
        public List<LevelInfo> Levels;
        public List<ProjectInfo> Projects;
        public List<StageInfo> Stages;
        public List<DualCurrencyInfo> DualCurrencySettings;
    }

    [Serializable]
    public class UpgradeInfo
    {
        public string upgradeId;
        public string category;
        public string nameEn;
        public string nameKo;
        public string descriptionEn;
        public string descriptionKo;
        public string currencyType;
        public float basePrice;
        public float priceMultiplier;
        public string effectType;
        public float effectValue;
        public int maxLevel;
        public string unlockCondition;
    }

    [Serializable]
    public class LevelInfo
    {
        public int level;
        public long requiredExp;
        public float moneyMultiplier;
        public string unlockFeature;
        public long bonusReward;
        public string specialEvent;
    }

    [Serializable]
    public class ProjectInfo
    {
        public string projectId;
        public int stage;
        public string nameEn;
        public string nameKo;
        public long requiredExp;
        public long baseReward;
        public float completionTime;
        public float streakBonus;
    }

    [Serializable]
    public class StageInfo
    {
        public int stage;
        public string nameEn;
        public string nameKo;
        public int requiredLevel;
        public string unlockFeatures;
    }

    [Serializable]
    public class DualCurrencyInfo
    {
        public int level;
        public float expRate;
        public float moneyRate;
        public float conversionRatio;
    }
}