SELECT
    tc.constraint_name AS "ConstraintName", tc.table_name AS "TableName", kcu.column_name AS "ColumnName", 
    ccu.table_name AS "ForeignTableName", ccu.column_name AS "ForeignColumnName" 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu ON kcu.constraint_name = tc.constraint_name AND kcu.constraint_schema = tc.constraint_schema
    JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name AND ccu.constraint_schema = tc.constraint_schema
WHERE constraint_type = 'FOREIGN KEY'
AND tc.constraint_schema = @TableSchema
ORDER BY tc.constraint_name;