SELECT table_catalog AS "TableCatalog", table_name AS "TableName" 
FROM information_schema.tables 
WHERE table_type='BASE TABLE' AND table_schema='public';