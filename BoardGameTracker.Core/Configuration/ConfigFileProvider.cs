using System.Xml;
using System.Xml.Linq;
using BoardGameTracker.Common.Exeptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Commands;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Extensions;
using Npgsql;

namespace BoardGameTracker.Core.Configuration;

public class ConfigFileProvider : IConfigFileProvider
{
    private readonly string _configFile;
    private static readonly object Mutex = new object();
    public const string CONFIG_ELEMENT_NAME = "Config";
    
    public string PostgresHost => GetValue("PostgresHost", string.Empty);
    public string PostgresUser => GetValue("PostgresUser", string.Empty);
    public string PostgresPassword => GetValue("PostgresPassword", string.Empty);
    public string PostgresMainDb => GetValue("PostgresMainDb", "boardgametracker-main");
    public int PostgresPort => GetValueInt("PostgresPort", 5432);
    private readonly IDiskProvider _diskProvider;
    

    public ConfigFileProvider(IDiskProvider diskProvider)
    {
        _diskProvider = diskProvider;
        _configFile = PathHelper.FullConfigFile;
    }
    
    public string GetPostgresConnectionString(string dbName)
    {
        var connectionBuilder = new NpgsqlConnectionStringBuilder
        {
            Database = dbName,
            Host = PostgresHost,
            Username = PostgresUser,
            Password = PostgresPassword,
            Port = PostgresPort,
            Enlist = false,
            IncludeErrorDetail = true
        };

        return connectionBuilder.ConnectionString;
    }
    
    private int GetValueInt(string key, int defaultValue, bool persist = true)
    {
        return Convert.ToInt32(GetValue(key, defaultValue, persist));
    }
    
    private string GetValue(string key, object defaultValue, bool persist = true)
    {
        var xDoc = LoadConfigFile();
        var config = xDoc.Descendants(CONFIG_ELEMENT_NAME).Single();

        var parentContainer = config;
        var valueHolder = parentContainer.Descendants(key).ToList();
        if (valueHolder.Count == 1)
        {
            return valueHolder.First().Value.Trim();
        }

        if (persist)
        {
            SetValue(key, defaultValue);
        }

        return defaultValue.ToString();
    }
    
    private void SetValue(string key, object value)
    {
        var valueString = value.ToString().Trim();
        var xDoc = LoadConfigFile();
        var config = xDoc.Descendants(CONFIG_ELEMENT_NAME).Single();

        var parentContainer = config;
        var keyHolder = parentContainer.Descendants(key);
        if (keyHolder.Count() != 1)
        {
            parentContainer.Add(new XElement(key, valueString));
        }
        else
        {
            parentContainer.Descendants(key).Single().Value = valueString;
        }

        SaveConfigFile(xDoc);
    }
    
    private XDocument LoadConfigFile()
    {
        try
        {
            lock (Mutex)
            {
                if (_diskProvider.FileExists(_configFile))
                {
                    var contents = _diskProvider.ReadAllText(_configFile);
                    if (string.IsNullOrWhiteSpace(contents))
                    {
                        throw new InvalidConfigFileException($"{_configFile} is empty. Please delete the config file and GameBoardTracker will recreate it.");
                    }

                    return XDocument.Parse(_diskProvider.ReadAllText(_configFile));
                }

                var xDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
                xDoc.Add(new XElement(CONFIG_ELEMENT_NAME));

                return xDoc;
            }
        }
        catch (XmlException ex)
        {
            throw new InvalidConfigFileException($"{_configFile} is corrupt is invalid. Please delete the config file and GameBoardTracker will recreate it.", ex);
        }
    }

    private void SaveConfigFile(XDocument xDoc)
    {
        lock (Mutex)
        {
            _diskProvider.WriteAllText(_configFile, xDoc.ToString());
        }
    }
    
    public Task Handle(ApplicationStartedCommand request, CancellationToken cancellationToken)
    {
        EnsureDefaultConfigFile();
        return Task.CompletedTask;
    }
    
    private void EnsureDefaultConfigFile()
    {
        if (!File.Exists(_configFile))
        {
            SaveConfigDictionary(GetConfigDictionary());
        }
    }

    private void SaveConfigDictionary(Dictionary<string, object> configValues)
    {
        var allWithDefaults = GetConfigDictionary();
        foreach (var configValue in configValues.Where(configValue => !configValue.Key.Equals("ApiKey", StringComparison.InvariantCultureIgnoreCase)))
        {
            allWithDefaults.TryGetValue(configValue.Key, out var currentValue);
            if (currentValue == null)
            {
                continue;
            }

            var equal = configValue.Value.ToString().Equals(currentValue.ToString());
            if (!equal)
            {
                SetValue(configValue.Key.FirstCharToUpper(), configValue.Value.ToString());
            }
        }
    }
    
    private Dictionary<string, object> GetConfigDictionary()
    {
        var dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        var type = GetType();
        var properties = type.GetProperties();

        foreach (var propertyInfo in properties)
        {
            var value = propertyInfo.GetValue(this, null);

            dict.Add(propertyInfo.Name, value);
        }

        return dict;
    }
}