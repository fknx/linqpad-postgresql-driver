SELECT
    tc.constraint_name "ConstraintName", tc.table_name "TableName", kcu.column_name "ColumnName", 
    ccu.table_name "ForeignTableName", ccu.column_name "ForeignColumnName" 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name
    JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name
WHERE constraint_type = 'FOREIGN KEY';