SELECT table_catalog "TableCatalog", table_name "TableName" 
FROM information_schema.tables 
WHERE table_type='BASE TABLE' AND table_schema='public';