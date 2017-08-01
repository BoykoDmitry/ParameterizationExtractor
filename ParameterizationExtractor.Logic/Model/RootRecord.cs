﻿using Quipu.ParameterizationExtractor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Quipu.ParameterizationExtractor.Logic.Model
{
    public class TableToExtract
    {
        public TableToExtract() { }
        public TableToExtract(string tableName, ExtractStrategy extractStrategy) : this(tableName, extractStrategy, new SqlBuildStrategy())
        {

        }

        public TableToExtract(string tableName) : this(tableName,new FKDependencyExtractStrategy(), new SqlBuildStrategy())
        {

        }

        public TableToExtract(string tableName,  ExtractStrategy extractStrategy, SqlBuildStrategy sqlBuildStrategy)
        {
            Affirm.NotNullOrEmpty(tableName, "tableName");
            Affirm.ArgumentNotNull(extractStrategy, "extractStrategy");
            Affirm.ArgumentNotNull(sqlBuildStrategy, "sqlBuildStrategy");

            TableName = tableName;
            ExtractStrategy = extractStrategy;
            SqlBuildStrategy = sqlBuildStrategy;
        }
        [XmlAttribute()]
        public string TableName { get; set; }
        [XmlAttribute()]
        public List<string> UniqueColumns { get; set; }

        public ExtractStrategy ExtractStrategy { get; set; }
        public SqlBuildStrategy SqlBuildStrategy { get; set; }
    }

    public class RecordsToExtract 
    {
        public RecordsToExtract() { }
        public RecordsToExtract(string tableName, string where)
        {
            Affirm.NotNullOrEmpty(tableName, "tableName");          

            TableName = tableName;
            Where = where;
        }
        [XmlAttribute()]
        public string TableName { get; set; }
        [XmlAttribute()]
        public string Where { get; set; }
        [XmlAttribute()]
        public int ProcessingOrder { get; set; }
    }
}
