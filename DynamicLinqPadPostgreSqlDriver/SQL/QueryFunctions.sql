select 
    proname AS "Name"
	, typname AS "ReturnType"
	, pronargs AS "ArgumentCount"
	, proargnames AS "ArgumentNames"
	, proargtypes AS "ArgumentTypeOids"
	, pg_get_expr(proargdefaults, 0) AS "ArgumentDefaults"
	, proretset AS "IsMultiValueReturn"
from 
    pg_proc 
    join pg_namespace n 
        on pronamespace = n.oid 
    join pg_type t
        on prorettype = t.oid
where nspname = 'public'