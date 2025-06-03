using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BoardGameTracker.Common.Exeptions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Commands;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Disk.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class ConfigFileProviderTests
    {
        private readonly Mock<IDiskProvider> _diskProviderMock;
        private readonly ConfigFileProvider _configFileProvider;
        private readonly string _testConfigFile = PathHelper.FullConfigFile;

        public ConfigFileProviderTests()
        {
            _diskProviderMock = new Mock<IDiskProvider>();
            _configFileProvider = new ConfigFileProvider(_diskProviderMock.Object);
        }

        [Fact]
        public void Currency_ShouldReturnDefaultValue_WhenNoEnvironmentVariableAndNoConfigFile()
        {
            SetupEmptyEnvironmentAndNoConfigFile();

            var result = _configFileProvider.Currency;

            result.Should().Be("€");
            VerifyConfigFileCreationAndValueSet("CURRENCY", "€");
        }

        [Fact]
        public void Currency_ShouldReturnEnvironmentVariable_WhenEnvironmentVariableIsSet()
        {
            Environment.SetEnvironmentVariable("CURRENCY", "EUR");
            
            var result = _configFileProvider.Currency;

            result.Should().Be("EUR");
            _diskProviderMock.VerifyNoOtherCalls();
            
            Environment.SetEnvironmentVariable("CURRENCY", null);
        }

        [Fact]
        public void Currency_ShouldReturnConfigFileValue_WhenConfigFileContainsValue()
        {
            Environment.SetEnvironmentVariable("CURRENCY", null);
            var configXml = CreateConfigXml("CURRENCY", "EUR");
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns(configXml);

            var result = _configFileProvider.Currency;

            result.Should().Be("EUR");
            _diskProviderMock.Verify(x => x.FileExists(_testConfigFile), Times.Once);
            _diskProviderMock.Verify(x => x.ReadAllText(_testConfigFile), Times.Exactly(2));
            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void Currency_Set_ShouldSaveValueToConfigFile_WhenValueIsSet()
        {
            var existingConfigXml = CreateConfigXml();
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns(existingConfigXml);

            _configFileProvider.Currency = "EUR";

            var configData = "<CURRENCY>EUR</CURRENCY>";
            _diskProviderMock.Verify(x => x.WriteAllText(_testConfigFile, It.Is<string>(xml => xml.Contains(configData))), Times.Once);
        }

        [Fact]
        public void DateFormat_ShouldReturnDefaultValue_WhenNoEnvironmentVariableAndNoConfigFile()
        {
            SetupEmptyEnvironmentAndNoConfigFile();

            var result = _configFileProvider.DateFormat;

            result.Should().Be("yy-MM-dd");
            VerifyConfigFileCreationAndValueSet("DATE_FORMAT", "yy-MM-dd");
        }

        [Fact]
        public void TimeFormat_ShouldReturnDefaultValue_WhenNoEnvironmentVariableAndNoConfigFile()
        {
            SetupEmptyEnvironmentAndNoConfigFile();

            var result = _configFileProvider.TimeFormat;

            result.Should().Be("HH:mm");
            VerifyConfigFileCreationAndValueSet("TIME_FORMAT", "HH:mm");
        }

        [Fact]
        public void UILanguage_ShouldReturnDefaultValue_WhenNoEnvironmentVariableAndNoConfigFile()
        {
            SetupEmptyEnvironmentAndNoConfigFile();

            var result = _configFileProvider.UILanguage;

            result.Should().Be("en-us");
            VerifyConfigFileCreationAndValueSet("UI_LANGUAGE", "en-us");
        }

        [Fact]
        public void PostgresHost_ShouldReturnEnvironmentVariable_WhenEnvironmentVariableIsSet()
        {
            Environment.SetEnvironmentVariable("DB_HOST", "localhost");
            
            var result = _configFileProvider.PostgresHost;

            result.Should().Be("localhost");
            
            Environment.SetEnvironmentVariable("DB_HOST", null);
        }

        [Fact]
        public void PostgresUser_ShouldReturnEnvironmentVariable_WhenEnvironmentVariableIsSet()
        {
            Environment.SetEnvironmentVariable("DB_USER", "testuser");
            
            var result = _configFileProvider.PostgresUser;

            result.Should().Be("testuser");
            
            Environment.SetEnvironmentVariable("DB_USER", null);
        }

        [Fact]
        public void PostgresPassword_ShouldReturnEnvironmentVariable_WhenEnvironmentVariableIsSet()
        {
            Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
            
            var result = _configFileProvider.PostgresPassword;

            result.Should().Be("testpass");
            
            Environment.SetEnvironmentVariable("DB_PASSWORD", null);
        }

        [Fact]
        public void PostgresMainDb_ShouldReturnDefaultValue_WhenNoEnvironmentVariableSet()
        {
            Environment.SetEnvironmentVariable("DB_NAME", null);
            SetupEmptyEnvironmentAndNoConfigFile();

            var result = _configFileProvider.PostgresMainDb;

            result.Should().Be("boardgametracker");
        }

        [Fact]
        public void PostgresPort_ShouldReturnDefaultValue_WhenNoEnvironmentVariableSet()
        {
            Environment.SetEnvironmentVariable("DB_PORT", null);
            SetupEmptyEnvironmentAndNoConfigFile();

            var result = _configFileProvider.PostgresPort;

            result.Should().Be(5432);
        }

        [Fact]
        public void PostgresPort_ShouldReturnEnvironmentVariable_WhenEnvironmentVariableIsSet()
        {
            Environment.SetEnvironmentVariable("DB_PORT", "3306");
            
            var result = _configFileProvider.PostgresPort;

            result.Should().Be(3306);
            
            Environment.SetEnvironmentVariable("DB_PORT", null);
        }

        [Fact]
        public void GetPostgresConnectionString_ShouldReturnValidConnectionString_WhenAllValuesProvided()
        {
            Environment.SetEnvironmentVariable("DB_HOST", "testhost");
            Environment.SetEnvironmentVariable("DB_USER", "testuser");
            Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
            Environment.SetEnvironmentVariable("DB_PORT", "5433");

            var result = _configFileProvider.GetPostgresConnectionString("testdb");

            result.Should().Contain("Database=testdb");
            result.Should().Contain("Host=testhost");
            result.Should().Contain("Username=testuser");
            result.Should().Contain("Password=testpass");
            result.Should().Contain("Port=5433");
            result.Should().Contain("Enlist=False");
            result.Should().Contain("Include Error Detail=True");

            Environment.SetEnvironmentVariable("DB_HOST", null);
            Environment.SetEnvironmentVariable("DB_USER", null);
            Environment.SetEnvironmentVariable("DB_PASSWORD", null);
            Environment.SetEnvironmentVariable("DB_PORT", null);
        }

        [Fact]
        public void LoadConfigFile_ShouldThrowInvalidConfigFileException_WhenConfigFileIsEmpty()
        {
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns("   ");

            var action = () => _configFileProvider.Currency;

            action.Should().Throw<InvalidConfigFileException>()
                .WithMessage("*is empty*");
        }

        [Fact]
        public void LoadConfigFile_ShouldThrowInvalidConfigFileException_WhenConfigFileIsCorrupt()
        {
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns("<invalid xml");

            var action = () => _configFileProvider.Currency;

            action.Should().Throw<InvalidConfigFileException>()
                .WithMessage("*is corrupt*");
        }

        [Fact]
        public void LoadConfigFile_ShouldCreateNewConfigFile_WhenConfigFileDoesNotExist()
        {
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(false);

            var result = _configFileProvider.Currency;

            result.Should().Be("€");
            _diskProviderMock.Verify(x => x.WriteAllText(_testConfigFile, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SetValue_ShouldUpdateExistingElement_WhenElementAlreadyExists()
        {
            var existingConfigXml = CreateConfigXml("CURRENCY", "USD");
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns(existingConfigXml);

            _configFileProvider.Currency = "EUR";

            _diskProviderMock.Verify(x => x.WriteAllText(_testConfigFile, It.Is<string>(xml => xml.Contains("<CURRENCY>EUR</CURRENCY>"))), Times.Once);
        }

        [Fact]
        public void SetValue_ShouldAddNewElement_WhenElementDoesNotExist()
        {
            var existingConfigXml = CreateConfigXml();
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns(existingConfigXml);

            _configFileProvider.Currency = "JPY";

            _diskProviderMock.Verify(x => x.WriteAllText(_testConfigFile, It.Is<string>(xml => xml.Contains("<CURRENCY>JPY</CURRENCY>"))), Times.Once);
        }

        [Fact]
        public void GetValue_ShouldTrimWhitespace_WhenEnvironmentVariableHasWhitespace()
        {
            Environment.SetEnvironmentVariable("CURRENCY", "  USD  ");
            
            var result = _configFileProvider.Currency;

            result.Should().Be("USD");
            
            Environment.SetEnvironmentVariable("CURRENCY", null);
        }

        [Fact]
        public void GetValue_ShouldTrimWhitespace_WhenConfigFileValueHasWhitespace()
        {
            Environment.SetEnvironmentVariable("CURRENCY", null);
            var configXml = CreateConfigXml("CURRENCY", "  GBP  ");
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns(configXml);

            var result = _configFileProvider.Currency;

            result.Should().Be("GBP");
        }

        [Theory]
        [InlineData("Currency", "€")]
        [InlineData("DateFormat", "yy-MM-dd")]
        [InlineData("TimeFormat", "HH:mm")]
        [InlineData("UILanguage", "en-us")]
        public void ConfigProperties_ShouldReturnCorrectDefaults_WhenNoConfigurationExists(string property, string expectedDefault)
        {
            SetupEmptyEnvironmentAndNoConfigFile();

            var result = property switch
            {
                "Currency" => _configFileProvider.Currency,
                "DateFormat" => _configFileProvider.DateFormat,
                "TimeFormat" => _configFileProvider.TimeFormat,
                "UILanguage" => _configFileProvider.UILanguage,
                _ => throw new ArgumentException($"Unknown property: {property}")
            };

            result.Should().Be(expectedDefault);
        }

        [Fact]
        public void Handle_ShouldEnsureDefaultConfigFile_WhenCalled()
        {
            var command = new ApplicationStartedCommand();

            var result = _configFileProvider.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public void GetConfigDictionary_ShouldReturnAllProperties_WhenCalled()
        {
            SetupEnvironmentVariables();

            var result = CallPrivateMethod<Dictionary<string, object>>("GetConfigDictionary");

            result.Should().NotBeNull();
            result.Should().ContainKeys("Currency", "DateFormat", "TimeFormat", "UILanguage");
            result.Should().ContainKeys("PostgresHost", "PostgresUser", "PostgresPassword", "PostgresMainDb", "PostgresPort");

            CleanupEnvironmentVariables();
        }

        [Fact]
        public void SaveConfigDictionary_ShouldUpdateChangedValues_WhenCalled()
        {
            var configValues = new Dictionary<string, object>
            {
                ["Currency"] = "USD",
                ["TimeFormat"] = "h:mm tt"
            };
            var existingConfigXml = CreateConfigXml();
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns(existingConfigXml);

            CallPrivateMethod("SaveConfigDictionary", configValues);

            _diskProviderMock.Verify(x => x.WriteAllText(_testConfigFile, It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void EnsureDefaultConfigFile_ShouldCreateConfigFile_WhenFileDoesNotExist()
        {
            CallPrivateMethod("EnsureDefaultConfigFile");

            _diskProviderMock.Verify(x => x.WriteAllText(_testConfigFile, It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void ThreadSafety_ShouldHandleConcurrentAccess_WhenMultipleThreadsAccessConfig()
        {
            var configXml = CreateConfigXml("CURRENCY", "USD");
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(true);
            _diskProviderMock.Setup(x => x.ReadAllText(_testConfigFile)).Returns(configXml);

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var result = _configFileProvider.Currency;
                    result.Should().Be("USD");
                }));
            }

            var action = () => Task.WaitAll(tasks.ToArray());
            action.Should().NotThrow();
        }

        private void SetupEmptyEnvironmentAndNoConfigFile()
        {
            Environment.SetEnvironmentVariable("CURRENCY", null);
            Environment.SetEnvironmentVariable("DECIMAL_SEPARATOR", null);
            Environment.SetEnvironmentVariable("DATE_FORMAT", null);
            Environment.SetEnvironmentVariable("TIME_FORMAT", null);
            Environment.SetEnvironmentVariable("UI_LANGUAGE", null);
            _diskProviderMock.Setup(x => x.FileExists(_testConfigFile)).Returns(false);
        }

        private void SetupEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("DB_HOST", "localhost");
            Environment.SetEnvironmentVariable("DB_USER", "testuser");
            Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");
            Environment.SetEnvironmentVariable("DB_PORT", "5432");
        }

        private void CleanupEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("DB_HOST", null);
            Environment.SetEnvironmentVariable("DB_USER", null);
            Environment.SetEnvironmentVariable("DB_PASSWORD", null);
            Environment.SetEnvironmentVariable("DB_PORT", null);
        }

        private static string CreateConfigXml(params (string key, string value)[] elements)
        {
            var config = new XElement("Config");
            foreach (var (key, value) in elements)
            {
                config.Add(new XElement(key, value));
            }
            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), config).ToString();
        }

        private static string CreateConfigXml(string key, string value)
        {
            return CreateConfigXml((key, value));
        }

        private static string CreateConfigXml()
        {
            return CreateConfigXml(Array.Empty<(string, string)>());
        }

        private void VerifyConfigFileCreationAndValueSet(string key, string value)
        {
            _diskProviderMock.Verify(x => x.FileExists(_testConfigFile), Times.Exactly(2));
            _diskProviderMock.Verify(x => x.WriteAllText(_testConfigFile, It.Is<string>(xml => xml.Contains($"<{key}>{value}</{key}>"))), Times.Once);
        }

        private T CallPrivateMethod<T>(string methodName, params object[] parameters)
        {
            var method = typeof(ConfigFileProvider).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)method.Invoke(_configFileProvider, parameters);
        }

        private void CallPrivateMethod(string methodName, params object[] parameters)
        {
            var method = typeof(ConfigFileProvider).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_configFileProvider, parameters);
        }
    }