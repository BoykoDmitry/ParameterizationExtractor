﻿using Quipu.ParameterizationExtractor.Logic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Quipu.ParameterizationExtractor.Logic.Interfaces
{
    public interface ISourceSchema
    {
        IEnumerable<PTableMetadata> Tables { get; }
        IEnumerable<PDependentTable> DependentTables { get; }
        PTableMetadata GetTableMetaData(string tableName);
        bool WasInit { get; }
        Task Init(CancellationToken cancellationToken);
        string Database { get; }
        string DataSource { get; }       
    }

    public interface IConnectionStringResolver
    {
        string GetConnectionString(string serverName, string dbName);
    }

    public interface IObjectMetaDataProvider
    {
        Task<IEnumerable<PDependentTable>> GetDependentTables(string tableName, CancellationToken cancellationToken);
        Task<IEnumerable<PDependentTable>> GetDependentTables(CancellationToken cancellationToken);
        Task<PTableMetadata> GetTableMetaData(string tableName, CancellationToken cancellationToken);
    }
}
