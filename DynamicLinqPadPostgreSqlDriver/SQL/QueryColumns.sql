SELECT column_name AS "ColumnName", is_nullable AS "Nullable", data_type AS "DataType", udt_name AS "UdtName", column_default AS "ColumnDefault"
FROM information_schema.columns 
WHERE table_catalog=@DatabaseName AND table_name=@TableName ORDER BY ordinal_position;