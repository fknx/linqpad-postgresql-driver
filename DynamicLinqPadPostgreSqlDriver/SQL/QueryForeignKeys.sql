SELECT
        "ConstraintName",
        "TableName",
        ARRAY_TO_STRING(ARRAY_AGG("ColumnName"),',') AS "ColumnNames", 
        "ForeignTableName", 
        ARRAY_TO_STRING(ARRAY_AGG("ForeignColumnName"),',') AS "ForeignColumnNames"
FROM (
        SELECT 
                con."ConstraintName",
                "ChildTables".relname AS "TableName",
                "ChildColumns".attname AS "ColumnName", 
                "ParentTables".relname AS "ForeignTableName", 
                "ParentColumns".attname AS "ForeignColumnName"
        FROM
             (SELECT
                        pg_namespace.nspname as "ConstraintSchema",
                        pg_constraint.conname as "ConstraintName",
                        pg_constraint.conrelid as "ChildTableId",
                        pg_constraint.confrelid as "ParentTableId",
                        UNNEST(pg_constraint.conkey) AS "ChildColumnId",
                        UNNEST(pg_constraint.confkey) AS "ParentColumnId"
                FROM 
                        pg_constraint
                        JOIN pg_class ON pg_class.oid = pg_constraint.conrelid
                        JOIN pg_namespace ON pg_namespace.oid = pg_class.relnamespace
                WHERE
                        pg_namespace.nspname = @TableSchema AND pg_constraint.contype = 'f'
             ) con
        -- retrieves info about columns
        JOIN pg_attribute "ChildColumns" ON
                 "ChildColumns".attrelid = con."ChildTableId" AND "ChildColumns".attnum = con."ChildColumnId"
        JOIN pg_attribute "ParentColumns" ON
                 "ParentColumns".attrelid = con."ParentTableId" AND "ParentColumns".attnum = con."ParentColumnId"
        -- retrieves info about tables
        JOIN pg_class "ChildTables" ON
                 "ChildTables".oid = con."ChildTableId"
        JOIN pg_class "ParentTables" ON
                 "ParentTables".oid = con."ParentTableId"
        -- retrieves info about column ordinal
        JOIN information_schema.key_column_usage kcu ON
                 kcu.constraint_schema = con."ConstraintSchema" AND kcu.constraint_name = con."ConstraintName" AND
                 kcu.table_name = "ChildTables".relname AND kcu.column_name = "ChildColumns".attname
        ORDER BY con."ConstraintName", kcu.ordinal_position
) pivot
GROUP BY "ConstraintName", "TableName", "ForeignTableName"
ORDER BY "ConstraintName"