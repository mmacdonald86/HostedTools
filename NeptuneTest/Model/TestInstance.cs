using com.antlersoft.HostedTools.Framework.Interface.Plugin;
using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Interface;
using com.antlersoft.HostedTools.Pipeline;
using com.antlersoft.HostedTools.Serialization;
using com.gt.NeptuneTest.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Model
{
    public class TestInstance : ITestInstance
    {
        private ISettingManager _settingManager;
        private List<INeptuneTestModule> _modules;
        private IJsonFactory _jsonFactory = new JsonFactory();
        private TestConfig _testConfig;

        private IHtValue _configSettings;

        private string _instanceFolder;

        public TestInstance(ISettingManager settingManager, IEnumerable<INeptuneTestModule> modules, IWorkMonitor monitor)
        {
            _settingManager = settingManager;
            _modules = new List<INeptuneTestModule>(modules);
            var configPath = TestSetup.TestConfigPath.Value<string>(_settingManager);
            using (StreamReader sr = new StreamReader(configPath))
            using (JsonTextReader jr = new JsonTextReader(sr))
            {
                _testConfig = _jsonFactory.GetSerializer().Deserialize<TestConfig>(jr);
            }
            Monitor = monitor;
            NeptuneHome = TestSetup.NeptuneHome.Value<string>(_settingManager);
            NeptuneSource = TestSetup.NeptuneSrc.Value<string>(_settingManager);
            UseDocker = TestSetup.UseDocker.Value<bool>(_settingManager);
            DockerExe = TestSetup.DockerExe.Value<string>(_settingManager);
            ContainerName = TestSetup.ContainerName.Value<string>(_settingManager);
        }
        public IList<INeptuneTestModule> AllModules => _modules;

        public string InstanceFolder => _instanceFolder;

        public string NeptuneHome { get; }

        public string NeptuneSource { get; }

        public IWorkMonitor Monitor { get; }

        private bool UseDocker { get; }

        private string DockerExe { get; }

        private string ContainerName { get; }

        public ITestConfig Config => _testConfig;

        public string GetExpandedConfigurationValue(string key)
        {
            string v;
            var val = _configSettings[key];
            if (val != null)
            {
                return ExpandValue(val.AsString);
            }
            return ExpandValue(_settingManager[key].GetExpanded());
        }

        enum LexState
        {
            Initial,
            SeenBackslash,
            SeenOpenBrace
        }

        public string ExpandValue(string v)
        {
            var result = new StringBuilder();
            var keyBuilder = new StringBuilder();
            var state = LexState.Initial;
            foreach (var ch in v)
            {
                switch (state)
                {
                    case LexState.Initial:
                        if (ch == '\\')
                        {
                            state = LexState.SeenBackslash;
                        }
                        else if (ch == '{')
                        {
                            state = LexState.SeenOpenBrace;
                        }
                        else
                        {
                            result.Append(ch);
                        }
                        break;
                    case LexState.SeenBackslash:
                        if (ch == '{')
                        {
                            state = LexState.Initial;
                            result.Append('{');
                        }
                        else
                        {
                            result.Append('\\');
                            result.Append(ch);
                        }
                        state = LexState.Initial;
                        break;
                    case LexState.SeenOpenBrace:
                        if (ch == '}')
                        {
                            result.Append(GetExpandedConfigurationValue(keyBuilder.ToString()));
                            keyBuilder.Clear();
                            state = LexState.Initial;
                        }
                        else
                        {
                            keyBuilder.Append(ch);
                        }
                        break;
                }
            }
            if (state == LexState.SeenBackslash)
            {
                result.Append('\\');
            }
            return result.ToString();
        }

        enum ERequoteState
        {
            Initial,
            SeenBackslash,
            InSingleQuote,
            InSingleQuoteBackslash,
            InDoubleQuote,
            InDoubleQuoteBackslash
        }
        private string Requote(string txt)
        {
            return txt;
            /*
            ERequoteState state = ERequoteState.Initial;
            StringBuilder result = new StringBuilder();
            foreach (var ch in txt)
            {
                switch (state)
                {
                    case ERequoteState.Initial:
                        switch (ch)
                        {
                            case '\\':
                                state = ERequoteState.SeenBackslash;
                                break;
                            case '\'':
                                result.Append("\"'");
                                state = ERequoteState.InSingleQuote;
                                break;
                            case '"':
                                result.Append("'\"");
                                state = ERequoteState.InDoubleQuote;
                                break;
                            default:
                                result.Append(ch);
                                break;
                        }
                        break;
                    case ERequoteState.SeenBackslash:
                        result.Append('\\');
                        result.Append('\\');
                        switch (ch)
                        {
                            case '\\':
                                result.Append('\\');
                                result.Append('\\');
                                break;
                            default:
                                result.Append(ch);
                                break;
                        }
                        state = ERequoteState.Initial;
                        break;
                    case ERequoteState.InSingleQuote:
                        switch (ch)
                        {
                            case '\\':
                                state = ERequoteState.InSingleQuoteBackslash;
                                break;
                            case '"':
                                result.Append("\\\"");
                                break;
                            case '\'':
                                result.Append("'\"");
                                state = ERequoteState.Initial;
                                break;
                            default:
                                result.Append(ch);
                                break;
                        }
                        break;
                    case ERequoteState.InSingleQuoteBackslash:
                        result.Append('\\');
                        result.Append('\\');
                        result.Append(ch);
                        state = ERequoteState.InSingleQuote;
                        break;
                    case ERequoteState.InDoubleQuote:
                        switch (ch)
                        {
                            case '\\':
                                state = ERequoteState.InDoubleQuoteBackslash;
                                break;
                            case '"':
                                result.Append("\"'");
                                state = ERequoteState.Initial;
                                break;
                            default:
                                result.Append(ch);
                                break;
                        }
                        break;
                    case ERequoteState.InDoubleQuoteBackslash:
                        result.Append('\\');
                        result.Append('\\');
                        result.Append(ch);
                        state = ERequoteState.InSingleQuote;
                        break;
                }
            }
            return result.ToString(); */
        }

        public async Task<Process> RunCommandLine(string executable, string arguments, bool detach = false, Stream input = null)
        {
            if (detach && input != null)
            {
                throw new InvalidOperationException("RunCommandLine: Can't both detach and redirect input");
            }
            if (UseDocker)
            {
                arguments = $"exec -i {ContainerName} {executable} {Requote(arguments)}";
                executable = DockerExe;
            }
            ProcessStartInfo psi = new ProcessStartInfo(executable, arguments);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            if (detach)
            {
                return Process.Start(psi);
            }

            var resultList = new List<Process>();

            var result = await Monitor.InvokeConsoleCommand(psi, resultList, input).ConfigureAwait(false);

            return resultList[0];
        }

        public void SetConfigurationValue(string key, string value)
        {
            _configSettings[key] = new JsonHtValue(value);
        }

        internal void RunSetup()
        {
            _instanceFolder = Path.GetTempFileName();
            File.Delete(_instanceFolder);
            _instanceFolder = $"{TestSetup.DataRoot.Value<string>(_settingManager)}/{Path.GetFileName(_instanceFolder)}";
            Directory.CreateDirectory(_instanceFolder);

            var teardown = new TearDownData();

            teardown.InstanceFolder = _instanceFolder;
            teardown.ModuleData = new JsonHtValue();

            InitialConfigTemplate configTemplate;

            var configPath = TestSetup.InitialConfigTemplate.Value<string>(_settingManager);
            using (StreamReader sr = new StreamReader(configPath))
            using (JsonTextReader jr = new JsonTextReader(sr))
            {
                configTemplate = _jsonFactory.GetSerializer().Deserialize<InitialConfigTemplate>(jr);
            }

            var regexReplacements = new List<Tuple<Regex, string>>();
            foreach (var t in configTemplate.RegExpReplace.AsDictionaryElements)
            {
                regexReplacements.Add(new Tuple<Regex, string>(new Regex(t.Key), t.Value.AsString));
            }

            _configSettings = configTemplate.ConfigValues;

            var setupResults = new Dictionary<string, Task<IHtValue>>();
            foreach (var moduleData in _testConfig.Modules.AsDictionaryElements)
            {
                var module = AllModules.First(m => moduleData.Key == m.Name);
                setupResults[module.Name] = module.Setup(this, moduleData.Value);
            }
            CancellationToken token;
            if (Monitor.Cast<ICancelableMonitor>() is ICancelableMonitor cancelable)
            {
                token = cancelable.Cancellation;
            }
            else
            {
                token = CancellationToken.None;
            }

            Task.WaitAll(setupResults.Values.ToArray(), token);

            if (token.IsCancellationRequested)
            {
                return;
            }
            foreach (var pair in setupResults)
            {
                teardown.ModuleData[pair.Key] = pair.Value.Result;
            }

            teardown.Settings = _configSettings;

            _settingManager[TestSetup.TearDownData.FullKey()].SetRaw(JsonConvert.SerializeObject(teardown, _jsonFactory.GetSettings()));

            _settingManager.Save();
            Monitor.Writer.WriteLine("Teardown data saved");

            var configTasks = new List<Task>();
            foreach (var configFile in configTemplate.ConfigFiles)
            {
                configTasks.Add(CreateConfigurationFileFromTemplate(configFile, regexReplacements));
            }
            Task.WaitAll(configTasks.ToArray(), token);

        }

        private async Task CreateConfigurationFileFromTemplate(string configFile, IList<Tuple<Regex, string>> regexReplacements)
        {
            using (var reader = new StreamReader($"{NeptuneSource}/config/{configFile}.template"))
            using (var writer = new StreamWriter($"{NeptuneHome}/conf/{configFile}"))
            {
                await CreateConfigurationFileFromTemplate(reader, writer, regexReplacements).ConfigureAwait(false);
            }
        }

        private async Task CreateConfigurationFileFromTemplate(TextReader reader, TextWriter writer, IList<Tuple<Regex, string>> regexReplacements)
        {
            for (var line = await reader.ReadLineAsync().ConfigureAwait(false); line != null; line = await reader.ReadLineAsync().ConfigureAwait(false))
            {
                // Replace things that weren't macros but should be based on regex
                foreach (var r in regexReplacements)
                {
                    line = r.Item1.Replace(line, r.Item2);
                }
                line = ReplaceMacros(line);
                await writer.WriteLineAsync(line);
            }
        }

        static Regex match_for_map_macro = new Regex("(.*)%%(.*)%%(.*)");
        static Regex match_for_scalar_macro = new Regex(@"(.*)%\$(.*)\$%(.*)");
        static Regex match_for_list_macro = new Regex(@"(.*)\$%(.*)%\$(.*)");
        static Regex match_for_string_macro = new Regex(@"(.*)%\^(.*)\^%(.*)");

        string ReplaceMacros(string line)
        {
            Match match;
            do
            {
                if ((match = match_for_map_macro.Match(line)).Success)
                {
                    // Since we aren't evaluating maps in values for this, ReplaceScalarMacro works
                    line = ReplaceMacro(line, match, ReplaceScalarMacro);
                }
                else if ((match = match_for_scalar_macro.Match(line)).Success)
                {
                    line = ReplaceMacro(line, match, ReplaceScalarMacro);
                }
                else if ((match = match_for_list_macro.Match(line)).Success)
                {
                    line = ReplaceMacro(line, match, ReplaceListMacro);
                }
                else if ((match = match_for_string_macro.Match(line)).Success)
                {
                    line = ReplaceMacro(line, match, ReplaceStringMacro);
                }
            }
            while (match.Success);

            return line;
        }

        string ReplaceMacro(string line, Match match, Func<string, Match, IHtValue, string> replacer)
        {
            var macroReference = match.Groups[2].Value;
            var firstColon = macroReference.IndexOf(':');
            string macroName = firstColon >= 0 ? macroReference.Substring(0, firstColon) : macroReference;
            string defaultValue = firstColon >= 0 ? macroReference.Substring(firstColon + 1) : null;
            IHtValue macroValue;
            if (defaultValue == null)
            {
                macroValue = GetExpandedConfigurationValues(macroName);
            }
            else
            {
                try
                {
                    macroValue = GetExpandedConfigurationValues(macroName);
                }
                catch
                {
                    macroValue = new JsonHtValue(defaultValue);
                }
            }
            return replacer(line, match, macroValue);
        }

        string ReplaceScalarMacro(string line, Match match, IHtValue replacement)
        {
            string subst = replacement.IsArray ? replacement[0].AsString : replacement.AsString;

            return match.Groups[1].Value + subst + match.Groups[3].Value;
        }

        string ReplaceListMacro(string line, Match match, IHtValue replacement)
        {
            if (!replacement.IsArray)
            {
                var tmp = new JsonHtValue();
                tmp[0] = new JsonHtValue(replacement.AsString);
                replacement = tmp;
            }
            var builder = new StringBuilder();
            foreach (var s in replacement.AsArrayElements)
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }
                builder.Append(s.AsString);
            }
            return builder.ToString();
        }

        string ReplaceStringMacro(string line, Match match, IHtValue replacement)
        {
            string subst = replacement.IsArray ? replacement[0].AsString : replacement.AsString;

            var len = subst.Length;
            if (len >= 2 && subst[0] == '"' && subst[len - 1] == '"')
            {
                subst = subst.Substring(1, len - 2);
            }

            return match.Groups[1].Value + subst + match.Groups[3].Value;
        }


        internal void RunTeardown()
        {
            var teardown = JsonConvert.DeserializeObject<TearDownData>(_settingManager[TestSetup.TearDownData.FullKey()].GetRaw(), _jsonFactory.GetSettings());

            _instanceFolder = teardown.InstanceFolder;

            _configSettings = teardown.Settings;

            CancellationToken token;
            if (Monitor.Cast<ICancelableMonitor>() is ICancelableMonitor cancelable)
            {
                token = cancelable.Cancellation;
            }
            else
            {
                token = CancellationToken.None;
            }

            foreach (var kvp in teardown.ModuleData.AsDictionaryElements)
            {
                _modules.First(m => m.Name == kvp.Key).Teardown(this, kvp.Value).Wait(token);
            }
            if (!token.IsCancellationRequested)
            {
                Directory.Delete(_instanceFolder);
            }
        }

        public void SetConfigurationValues(string key, IEnumerable<string> value)
        {
            var val = new JsonHtValue();
            int i = 0;
            foreach (var s in value)
            {
                val[i++] = new JsonHtValue(s);
            }
            _configSettings[key] = val;
        }

        public IHtValue GetExpandedConfigurationValues(string key)
        {
            var v = _configSettings[key];
            if (v == null || !v.IsArray)
            {
                return new JsonHtValue(GetExpandedConfigurationValue(key));
            }
            else
            {
                var result = new JsonHtValue();
                int i = 0;
                foreach (var s in v.AsArrayElements)
                {
                    result[i++]= new JsonHtValue(ExpandValue(s.AsString));
                }
                return result;
            }
        }
    }
}
