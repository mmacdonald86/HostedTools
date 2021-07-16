using com.antlersoft.HostedTools.Framework.Interface.Plugin;
using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Framework.Model.Menu;
using com.antlersoft.HostedTools.Framework.Model.Plugin;
using com.antlersoft.HostedTools.Framework.Model.Setting;
using com.gt.NeptuneTest.Interface;
using com.gt.NeptuneTest.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace com.gt.NeptuneTest
{
    [Export(typeof(ISettingDefinitionSource))]
    public class TestSetup : SimpleWorker, ISettingDefinitionSource
    {
        static internal ISettingDefinition InitialConfigTemplate = new PathSettingDefinition("InitialConfigTemplate", "NeptuneTest", "Initial configuration file template", false, false, ".json|*.json", "Global values for all config files; equivalent to range values");
        static internal ISettingDefinition TestConfigPath = new PathSettingDefinition("TestConfigPath", "NeptuneTest", "Test configuration file", false, false, ".json|*.json", "Configuration file with module settings");
        static internal ISettingDefinition NeptuneHome = new PathSettingDefinition("NeptuneHome", "NeptuneTest", "Neptune home", false, true, null, "Neptune runtime home; ordinarily /home/xad/neptune", "/home/xad/neptune");
        static internal ISettingDefinition NeptuneSrc = new PathSettingDefinition("NeptuneSrc", "NeptuneTest", "Neptune source", false, true, null, "Nise folder in neptune source; something like /neptune-src/nise", "/neptune-src/nise");
        static internal ISettingDefinition UseDocker = new SimpleSettingDefinition("UseDocker", "NeptuneTest", "Use Docker", "Run commands in the named container", typeof(bool), "false", false, 0);
        static internal ISettingDefinition DockerExe = new PathSettingDefinition("DockerExe", "NeptuneTest", "Docker Command", false, false, null, "Complete path to docker command line command", "c:/Program Files/Docker/Docker/resources/bin/docker.exe");
        static internal ISettingDefinition ContainerName = new SimpleSettingDefinition("ContainerName", "NeptuneTest", "Docker container", "Docker container name", null, "neptune-dev-test_neptune_1");
        static internal ISettingDefinition MySqlBin = new PathSettingDefinition("MySqlBin", "NeptuneTest", "MySQL bin folder", false, true, null, null, "/usr/bin");
        static internal ISettingDefinition PostgresBin = new PathSettingDefinition("PostgresBin", "NeptuneTest", "PostgreSQL bin folder", false, true, null, null, "/usr/bin");
        static internal ISettingDefinition TearDownData = new SimpleSettingDefinition("TearDownData", "NeptuneTest", null, null, null, null, false);
        static internal ISettingDefinition DataRoot = new PathSettingDefinition("DataRoot", "NeptuneTest", "Base of test data tree", false, true);


        [ImportMany]
        public List<INeptuneTestModule> Modules;

        public TestSetup()
        : base(new MenuItem[] { new MenuItem("NeptuneTest", "Neptune Testing"),
        new MenuItem("NeptuneTest.TestSetup", "Test Setup", typeof(TestSetup).FullName, "NeptuneTest") },
        new string[] { InitialConfigTemplate.FullKey(), TestConfigPath.FullKey(), NeptuneHome.FullKey(), NeptuneSrc.FullKey(), UseDocker.FullKey(), ContainerName.FullKey(), DockerExe.FullKey(), MySqlBin.FullKey(), PostgresBin.FullKey(), DataRoot.FullKey()  })
        { }

        public IEnumerable<ISettingDefinition> Definitions => new ISettingDefinition[] { InitialConfigTemplate, TestConfigPath, NeptuneHome, NeptuneSrc, UseDocker, DockerExe, ContainerName, MySqlBin, PostgresBin, TearDownData, DataRoot };

        public override void Perform(IWorkMonitor monitor)
        {
            var instance = new TestInstance(SettingManager, Modules, monitor);
            instance.RunSetup();
        }
    }

    public class TestTeardown : SimpleWorker
    {
        public TestTeardown()
        : base(new MenuItem("NeptuneTest.TestTeardown", "Test Teardown", typeof(TestTeardown).FullName, "NeptuneTest"),
        new string[] { TestSetup.InitialConfigTemplate.FullKey(), TestSetup.TestConfigPath.FullKey(), TestSetup.NeptuneHome.FullKey(), TestSetup.NeptuneSrc.FullKey(), TestSetup.UseDocker.FullKey(), TestSetup.ContainerName.FullKey(),
        TestSetup.DockerExe.FullKey(), TestSetup.MySqlBin.FullKey(), TestSetup.PostgresBin.FullKey() })
        { }

        [ImportMany]
        public List<INeptuneTestModule> Modules;

        public override void Perform(IWorkMonitor monitor)
        {
            var instance = new TestInstance(SettingManager, Modules, monitor);
            instance.RunTeardown();
        }
    }
}
