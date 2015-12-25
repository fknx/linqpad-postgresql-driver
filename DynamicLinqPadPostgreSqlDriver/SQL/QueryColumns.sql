SELECT column_name "ColumnName", is_nullable "Nullable", data_type "DataType", udt_name "UdtName", column_default "ColumnDefault"
FROM information_schema.columns 
WHERE table_catalog=@DatabaseName AND table_name=@TableName ORDER BY ordinal_position;