SELECT 
	table_catalog AS "TableCatalog", 
	table_name  AS "TableName",
	table_schema AS "TableSchema"
FROM information_schema.tables 
WHERE table_type='BASE TABLE'
AND table_schema NOT IN ('pg_catalog', 'information_schema', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')