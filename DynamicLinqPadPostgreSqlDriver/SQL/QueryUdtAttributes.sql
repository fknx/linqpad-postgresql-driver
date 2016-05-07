select attname AS "AttributeName", format_type(atttypid, atttypmod) AS "AttributeType"
from pg_type
     join pg_class on pg_class.oid = pg_type.typrelid
     join pg_attribute on pg_attribute.attrelid = pg_class.oid
where typname = :typname
order by attnum