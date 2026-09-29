--[beginscript]

alter table CK.tWeakActor drop constraint DF_CK_tWeakActor_BinDate
exec sp_rename N'CK.tWeakActor.BinDate', N'DisableDate', N'COLUMN';
alter table CK.tWeakActor add constraint DF_CK_tWeakActor_DisableDate default ( '0001-01-01' ) for DisableDate;

--[endscript]
