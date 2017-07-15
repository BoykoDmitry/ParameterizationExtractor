﻿using Quipu.ParameterizationExtractor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quipu.ParameterizationExtractor.Common
{
    public static class SqlHelper
    {
        public static IEnumerable<PField> NotIdentityFields(PRecord table)
        {
            return table.Where(_ => !_.MetaData.IsIdentity);
        }

        public static IEnumerable<PField> InjectSqlVariable(IEnumerable<PField> fields, string sqlVar, string fieldName)
        {         
            var f = fields.FirstOrDefault(_ => _.FieldName == fieldName);

            if (f != null)
                f.Expression = sqlVar;

            return fields;
        }

        public static IEnumerable<PField> PrepareFieldsForChild(PRecord child, string sqlVar, PDependentTable fk)
        {
            return InjectSqlVariable(NotIdentityFields(child), sqlVar, child.TableName == fk.ParentTable ? fk.ParentColumn : fk.ReferencedColumn);
        }

        public static string GetSeparatedNameValueString(IEnumerable<PField> fields, string separator, Func<PField,string> valueGetter)
        {
            return string.Join(separator, fields.Select(_ => string.Format("[{0}] = {1}", _.FieldName, valueGetter?.Invoke(_))));

        }

        public static string GetNameValueString(IEnumerable<PField> fields)
        {
            return GetSeparatedNameValueString(fields, " and ", _ => _.ValueToSqlString());
            //return string.Join(" and ", fields.Select(_ => string.Format("[{0}] = {1}", _.FieldName, _.ValueToSqlString())));
        }

        public static string GetNameValueForUpdateString(IEnumerable<PField> fields)
        {
            return GetSeparatedNameValueString(fields, " , ", _ => _.ValueToSqlString());
        }
        public static string GetNameNormalValueString(IEnumerable<PField> fields)
        {
            return GetSeparatedNameValueString(fields, " ", _ => _.Value.ToString());
            //return string.Join(" ", fields.Select(_ => string.Format("[{0}] = {1}", _.FieldName, _.Value)));
        }

        public static string IfExistsSql(PRecord table)
        {
            var fields = NotIdentityFields(table);

            var sql = new StringBuilder();

            sql.AppendFormat("if not exists(select top 1 1 from {0} {1})", table.TableName, table.GetUniqueSqlWhere());
            sql.AppendLine();
            sql.AppendFormat("insert into {0} (", table.TableName);
            sql.Append(string.Join(",", fields.Select(_=>_.FieldName)));           
            sql.AppendFormat(")");
            sql.AppendLine("Values(");
            sql.Append(string.Join(",", fields.Select(_ => _.ValueToSqlString())));
            sql.Append(")");

            return sql.ToString();
        }
        public static bool IsNumericType(this object o)
        {
            return o.GetType().IsNumericType();
        }
        public static bool IsNumericType(this Type o)
        {
            switch (Type.GetTypeCode(o))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }


}
