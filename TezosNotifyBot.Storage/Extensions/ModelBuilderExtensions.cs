using System;
using Microsoft.EntityFrameworkCore;
using TezosNotifyBot.Shared.Extensions;

namespace TezosNotifyBot.Storage.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyPostgresConventions(this ModelBuilder builder)
        {
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                // Replace column names            
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.GetColumnName().ToSnakeCase());
                }

                if (entity.IsOwned())
                    continue;

                // var tableName = entity.GetTableName();
                // if (tableName == entity.ClrType.Name)
                // {
                //     // Replace table names
                //     entity.SetTableName(tableName.ToSnakeCase());
                // }
                entity.SetTableName(entity.ClrType.Name.ToSnakeCase());

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetName().ToSnakeCase());
                }

                foreach (var key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(key.GetConstraintName().ToSnakeCase());
                }

                foreach (var index in entity.GetIndexes())
                {
                    var name = index.GetName();

                    // FIXME: Fix issue with not handled IX cases
                    if (name.StartsWith("ix", StringComparison.OrdinalIgnoreCase))
                        name = name.Substring(2);

                    index.SetName($"ix_{name.ToSnakeCase()}");
                }
            }
        }
    }
}