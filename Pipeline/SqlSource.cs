﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.Common;

using com.antlersoft.HostedTools.Framework.Interface.Menu;
using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Framework.Model.Menu;
using com.antlersoft.HostedTools.Framework.Model.Plugin;
using com.antlersoft.HostedTools.Framework.Model.Setting;
using com.antlersoft.HostedTools.Interface;
using com.antlersoft.HostedTools.Serialization;
using com.antlersoft.HostedTools.Sql;
using com.antlersoft.HostedTools.Sql.Interface;
using com.antlersoft.HostedTools.Sql.Model;

namespace com.antlersoft.HostedTools.Pipeline
{
    public abstract class SqlSourceBase : EditOnlyPlugin, IHtValueSource, ISqlIndexInfo
    {
        protected SqlSourceBase(IEnumerable<IMenuItem> menuEntries, IEnumerable<string> keys)
        : base(menuEntries, keys)
        {}
        protected SqlSourceBase(IMenuItem item, IEnumerable<string> keys)
        : base(item, keys)
        {}
        public abstract DbConnection GetConnection();

        public abstract string QueryType { get; }

        protected virtual IHtValue GetColumnValue(IDataReader reader, int col)
        {
            return SqlUtil.GetColumnValue(reader, col);
        }

        public virtual IEnumerable<IHtValue> GetRows()
        {
            return SqlUtil.GetRows(this, SqlCommand.Value<string>(SettingManager), CommandTimeout.Value<int>(SettingManager), GetColumnValue);
        }

        public virtual Dictionary<string,IIndexSpec> GetIndexInfo(IBasicTable table)
        {
            var result = new Dictionary<string,IIndexSpec>();
            string currentIndexName = null;
            IndexSpec currentIndexColumns=null;
            foreach (var row in SqlUtil.GetRows(this,
                $"SELECT index_name, seq_in_index, column_name FROM information_schema.statistics WHERE table_schema='{table.Schema}' and table_name='{table.Name}' order by index_name, seq_in_index",
                CommandTimeout.Value<int>(SettingManager)))
            {
                var newRow = SqlUtil.LowerCaseKeys(row);
                var indexName = newRow["index_name"].AsString;
                if (indexName != currentIndexName)
                {
                    currentIndexColumns = new IndexSpec();
                    result[indexName] = currentIndexColumns;
                    currentIndexName = indexName;
                }
                currentIndexColumns.AddColumn(new IndexColumn(table[newRow["column_name"].AsString]));
            }
            return result;
        }

        public virtual string SourceDescription
        {
            get
            {
                string query = SqlCommand.Value<string>(SettingManager);
                int index = query.IndexOf('\n');
                if (index >= 0)
                {
                    query = query.Substring(0, index);
                }
                return $"{QueryType}: {query}";
            }
        }

        protected static ISettingDefinition SqlCommand = new MultiLineSettingDefinition("SqlCommand", "Pipeline", 8, "Sql Query");
        protected static ISettingDefinition CommandTimeout = new SimpleSettingDefinition("CommandTimeout", "Pipeline", "Command Timeout", "Number of seconds before query will timeout", typeof(int), "120");
    }

    [Export(typeof(ISettingDefinitionSource))]
    [Export(typeof(IHtValueSource))]
    public class SqlSource : SqlSourceBase, ISettingDefinitionSource, IHtValueSource
    {

        public SqlSource()
            : base(new MenuItem("DevTools.Pipeline.Input.SqlSource", "Sql Query", typeof(SqlSource).FullName, "DevTools.Pipeline.Input"), new [] {"Common.SqlDataConnectionString", SqlCommand.FullKey(), CommandTimeout.FullKey()} )
        { }

        public IEnumerable<ISettingDefinition> Definitions
        {
            get { return new[] {SqlCommand, CommandTimeout}; }
        }

        public override DbConnection GetConnection()
        {
            return DbProviderFactories.GetFactory(SqlDataParameters.DbProviderFactoryName.Value<string>(SettingManager)).CreateConnection();
        }

        public override string QueryType => "SQL Server";
    }
}
