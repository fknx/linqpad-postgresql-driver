SELECT 
	table_catalog AS "TableCatalog", 
	table_name  AS "TableName",
	table_schema AS "TableSchema"
FROM information_schema.tables 
WHERE table_type='BASE TABLE' AND table_schema in (select schema_name from information_schema.schemata where schema_owner != 'postgres' or schema_name='public')