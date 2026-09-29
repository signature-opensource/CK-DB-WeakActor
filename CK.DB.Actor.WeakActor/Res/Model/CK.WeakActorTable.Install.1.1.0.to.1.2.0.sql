--[beginscript]

alter table CK.tWeakActor drop constraint DF_CK_tWeakActor_BinDate
exec sp_rename N'CK.tWeakActor.BinDate', N'DisableDate', N'COLUMN';
alter table CK.tWeakActor add constraint DF_CK_tWeakActor_DisableDate default ( '0001-01-01' ) for DisableDate;

-- Fix the WeakActorName collation.
-- Quick and dirty shrotcut: handle Zone package here if it exists.
declare @HaveZone bit;
if object_id('CK.UK_CK_tWeakActor_WeakActorName') is not null
begin
    set @HaveZone = 0;
    alter table CK.tWeakActor drop constraint UK_CK_tWeakActor_WeakActorName;
end
else if object_id('CK.UK_CK_tWeakActor_WeakActorName_ZoneId') is not null
begin
    set @HaveZone = 1;
    alter table CK.tWeakActor drop constraint UK_CK_tWeakActor_WeakActorName_ZoneId;
end

alter table CK.tWeakActor alter column WeakActorName nvarchar(255) COLLATE Latin1_General_100_CI_AI

if @HaveZone = 0
begin
    alter table CK.tWeakActor add constraint UK_CK_tWeakActor_WeakActorName unique( WeakActorName );
end
else if @HaveZone = 1
begin
    alter table CK.tWeakActor add constraint UK_CK_tWeakActor_WeakActorName_ZoneId unique( WeakActorName, ZoneId );
end

--[endscript]
