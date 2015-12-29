SELECT pg_attribute.attname AS "Name" FROM pg_index, pg_class, pg_attribute, pg_namespace
WHERE pg_class.oid = @Oid AND indrelid = pg_class.oid AND nspname = 'public'
  AND pg_class.relnamespace = pg_namespace.oid AND pg_attribute.attrelid = pg_class.oid
  AND pg_attribute.attnum = any(pg_index.indkey) AND indisprimary;