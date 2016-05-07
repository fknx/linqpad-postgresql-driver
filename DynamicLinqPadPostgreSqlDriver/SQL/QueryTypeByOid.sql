select 
    oid
	, typnamespace
	, typname
	, typarray
	, typisdefined
from 
    pg_type
where oid in (@oids)