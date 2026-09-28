--[beginscript]

IF NOT EXISTS (
  SELECT * 
  FROM   sys.columns 
  WHERE  object_id = OBJECT_ID(N'CK.tWeakActor') 
         AND name = 'ArchiveDate'
)
begin
    alter table CK.tWeakActor add
        ArchiveDate datetime2 (2) not null
        constraint DF_CK_tWeakActor_ArchiveDate default ( '0001-01-01' );
end

--[endscript]
