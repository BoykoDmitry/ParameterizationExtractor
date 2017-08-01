﻿using Quipu.ParameterizationExtractor.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quipu.ParameterizationExtractor.Logic.Model;
using Quipu.ParameterizationExtractor.Common;
using System.Threading;
using Quipu.ParameterizationExtractor.Logic.MSSQL;
using System.Data;
using System.ComponentModel.Composition;

namespace ParameterizationExtractor.Logic.MSSQL
{   
    [Export(typeof(IObjectMetaDataProvider))]
    public class ObjectMetaDataProvider : IObjectMetaDataProvider
    {
        public static string sqlFKs = @"        
SELECT  
    fk.name as [Name],
    OBJECT_NAME(fk.parent_object_id) 'ParentTable',
    c1.name 'ParentColumn',
    OBJECT_NAME(fk.referenced_object_id) 'ReferencedTable',
    c2.name 'ReferencedColumn'
FROM 
    sys.foreign_keys fk
INNER JOIN 
    sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN
    sys.columns c1 ON fkc.parent_column_id = c1.column_id AND fkc.parent_object_id = c1.object_id
INNER JOIN
    sys.columns c2 ON fkc.referenced_column_id = c2.column_id AND fkc.referenced_object_id = c2.object_id
        ";

        private IUnitOfWorkFactory _uowFactory;
        private IMetaDataInitializer _initializer;
        private ILog _log;

        [ImportingConstructor]
        public ObjectMetaDataProvider(IUnitOfWorkFactory uowf, ILog log, IMetaDataInitializer initializer)
        {
            Affirm.ArgumentNotNull(uowf, "uowf");
            Affirm.ArgumentNotNull(log, "log");

            _log = log;
            _uowFactory = uowf;
            _initializer = initializer;
        }

        public async Task<IEnumerable<PDependentTable>> GetDependentTables(string tableName, CancellationToken cancellationToken)
        {
            var sql = sqlFKs;
            if (!string.IsNullOrEmpty(tableName))
                sql = string.Format("{0} where OBJECT_NAME(fk.parent_object_id) = '{1}' or OBJECT_NAME(fk.referenced_object_id) = '{1}'", sql, tableName);

            var result = new List<PDependentTable>();

            using (var uof = _uowFactory.GetUnitOfWork())
            using (var dr = await uof.ExecuteReaderAsync(sql, cancellationToken))
            {
                var dt = new DataTable();
                dt.Load(dr);

                foreach (DataRow r in dt.Rows)
                {
                    var item = new PDependentTable
                    {
                        Name = r["Name"].ToString(),
                        ParentColumn = r["ParentColumn"].ToString(),
                        ParentTable = r["ParentTable"].ToString(),
                        ReferencedColumn = r["ReferencedColumn"].ToString(),
                        ReferencedTable = r["ReferencedTable"].ToString()
                    };

                    result.Add(item);
                }
            }

            return result;
        }

        public async Task<PTableMetadata> GetTableMetaData(string tableName, CancellationToken cancellationToken)
        {
            string sql = string.Format("{0} and object_name(C.[object_id]) = '{1}'", MSSQLSourceSchema.sqlPKColumns, tableName);

            using (var uof = _uowFactory.GetUnitOfWork())
            using (var dr = await uof.ExecuteReaderAsync(sql, cancellationToken))
            {
                var ptable = new PTableMetadata() { TableName = tableName };
                var dt = new DataTable();
                dt.Load(dr);

                foreach (DataRow r in dt.Rows)
                {
                    var field = _initializer.InitTableMetaData(r);

                    ptable.Add(field);
                }

                return ptable;
            }           
        }

        public Task<IEnumerable<PDependentTable>> GetDependentTables(CancellationToken cancellationToken)
        {
            return GetDependentTables(string.Empty, cancellationToken);
        }
    }
}
