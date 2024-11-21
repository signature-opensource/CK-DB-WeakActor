alter table CK.tWeakActor
    add ZoneId int not null constraint DF_TEMP0 default( 0 );

alter table CK.tWeakActor drop constraint DF_TEMP0;

alter table CK.tWeakActor
    add constraint FK_CK_tWeakActor_ZoneId foreign key (ZoneId) references CK.tZone( ZoneId );

alter table CK.tWeakActor drop constraint UK_CK_tWeakActor_WeakActorName;

alter table CK.tWeakActor
    add constraint UK_CK_tWeakActor_WeakActorName_ZoneId unique( WeakActorName, ZoneId )
