select 
    proname, typname, pronargs, proargnames, proargtypes, proargdefaults
from 
    pg_proc 
    join pg_namespace n 
        on pronamespace = n.oid 
    join pg_type t
        on prorettype = t.oid
where nspname = 'public'