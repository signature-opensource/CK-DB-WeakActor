--[beginscript]

IF NOT EXISTS (
  SELECT * 
  FROM   sys.columns 
  WHERE  object_id = OBJECT_ID(N'CK.tWeakActor') 
         AND name = 'DisableDate'
)
begin
    alter table CK.tWeakActor add
        DisableDate datetime2 (2) not null
        constraint DF_CK_tWeakActor_DisableDate default ( '0001-01-01' );
end

--[endscript]
