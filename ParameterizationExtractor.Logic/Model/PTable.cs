﻿using Quipu.ParameterizationExtractor.Logic.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quipu.ParameterizationExtractor.Logic.Model
{
    [DebuggerDisplay("{PRecord.TableName} {FK.ParentTable}-{FK.ReferencedTable}")]
    public class PTableDependency
    {
        public PRecord PRecord { get; set; }
        public PDependentTable FK { get; set; }
    }

    [DebuggerDisplay("{TableName} {PK}")]
    public class PRecord : List<PField>
    {
        private readonly PTableMetadata _metaData;
        public PRecord(IDataRecord dataRow, PTableMetadata metaData)
        {
            _metaData = metaData;

            foreach (var field in metaData)
            {
                if (dataRow[field.FieldName] != null
                    && !dataRow.IsDBNull(dataRow.GetOrdinal(field.FieldName)))
                    Add(new PField(field) { FieldName = field.FieldName, Value = dataRow[field.FieldName] });
            }

            PkField = (from f in _metaData
                       join v in this
                             on f.FieldName equals v.FieldName
                       where f.IsPK
                       select v
                  ).FirstOrDefault();

            UniqueFields = (from f in this
                            join v in _metaData.UniqueColumnsCollection
                                  on f.FieldName equals v
                            select f).ToList();
    
            _parents = new List<PTableDependency>();
            _children = new List<PTableDependency>();
        }

        public ExtractStrategy ExtractStrategy { get; set; }
        public SqlBuildStrategy SqlBuildStrategy { get; set; }
        private readonly IList<PTableDependency> _parents;
        private readonly IList<PTableDependency> _children;
        public IList<PTableDependency> Childern { get { return _children; } }
        public IList<PTableDependency> Parents { get { return _parents; } }
        public IList<PField> UniqueFields { get; private set; }
        public string PK { get { return PkField?.Value.ToString(); } }
        public PField PkField { get; private set; }
        public bool IsStartingPoint { get; set; }
        public string TableName { get { return _metaData.TableName; } }
        public PTableMetadata MetaData { get { return _metaData; } }
        public string Source { get; set; }
        public override bool Equals(object obj)
        {
            var p = obj as PRecord;
            if (p != null)
                return p.ToString() == this.ToString();

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}",TableName,PK);
        }        

        public IEnumerable<PField> GetUniqueFields()
        {
            var list = new List<PField>();
            if (UniqueFields != null
                && UniqueFields.Any())
                list.AddRange(UniqueFields);
            else if (PkField != null)
                list.Add(PkField);

            return list;
        }

        public string GetUniqueSqlWhere()
        {
            string s = string.Empty;

            var list = GetUniqueFields();

            if (list.Any())
            {
                return SqlHelper.GetNameValueString(list);
            }

            return s;
        }

        private static Regex varRgx = new Regex("(?:[^a-z0-9@]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        public string GetPKVarName()
        {
            var nm = string.Format("@{0}_{1}_{2}", TableName, PkField?.FieldName, PK);
            nm = nm.Replace("-", "Ngt");

            return varRgx.Replace(nm, string.Empty);
        }

        public bool IsNumericPK { get { return PkField == null ? false : PkField.MetaData.FieldType.IsNumericType(); } }
        public bool IsIdentityPK { get { return PkField == null ? false : PkField.MetaData.IsIdentity; } }

        public IEnumerable<PField> GetAllFKs()
        {
            return this.Where(_ => Parents.Any(f => f.FK.ReferencedColumn == _.FieldName));
        }

        public bool IsAllFKInitialized()
        {
            return GetAllFKs().All(_ => string.IsNullOrEmpty(_.Expression));
        }
    }

    [DebuggerDisplay("{FieldName}")]
    public class PField
    {
        private readonly PFieldMetadata _metaData;
        public PField(PFieldMetadata metaData)
        {
            _metaData = metaData;
        }
        public override string ToString()
        {
            return string.Format("{0}", FieldName);
        }
        public PFieldMetadata MetaData { get { return _metaData; } }
        public string FieldName { get; set; }
        public object Value { get; set; }
        public string Expression { get; set; }
        public string ValueToSqlString()
        {
            if (!string.IsNullOrEmpty(Expression))
                return Expression;

            var str = string.Empty;

            if (_metaData.FieldType == typeof(string))
            {
                if (!string.IsNullOrEmpty(Value.ToString()))
                {
                    string format = "'{0}'";
                    if (_metaData.BaseTypeName == "nvarchar")
                        format = "N'{0}'";

                    return string.Format(format, PrepareValueForScript(Value.ToString()));
                }
                else
                {
                    if (!MetaData.IsNullable)
                        return "''";
                    else
                        return "null";
                }
            }
            else if (_metaData.FieldType == typeof(bool))
            {
                return Value.ToString() == bool.TrueString ? "1" : "0";
            }
            else if (_metaData.FieldType == typeof(DateTime))
            {
                var date = Convert.ToDateTime(Value);

                return string.Format("'{0}'", date.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            else if (_metaData.FieldType == typeof(decimal)
                || _metaData.FieldType == typeof(Double)
                 || _metaData.FieldType == typeof(Single))
            {
                var num = Convert.ToString(Value, System.Globalization.CultureInfo.InvariantCulture);

                return num;
            }
            else
                str = PrepareValueForScript(Value.ToString());

            return str;
        }        

        private string PrepareValueForScript(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
