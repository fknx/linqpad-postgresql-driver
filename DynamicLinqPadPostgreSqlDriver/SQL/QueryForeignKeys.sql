SELECT
    tc.constraint_name AS "ConstraintName", tc.table_name AS "TableName", kcu.column_name AS "ColumnName", 
    ccu.table_name AS "ForeignTableName", ccu.column_name AS "ForeignColumnName" 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name
    JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name
WHERE constraint_type = 'FOREIGN KEY';