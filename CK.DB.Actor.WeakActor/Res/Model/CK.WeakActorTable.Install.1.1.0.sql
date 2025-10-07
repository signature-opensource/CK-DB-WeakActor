--[beginscript]

create table CK.tWeakActor
(
	WeakActorId   int not null
	    constraint PK_CK_tWeakActor primary key nonclustered ( WeakActorId )
        constraint FK_CK_tWeakActor_WeakActorId foreign key references CK.tActor( ActorId ),

    -- Collation should be Case insensitive at least (this is the recommended practice for user-like names).
    -- 255 seems large but this is to support emails as user-like names: emails can be 254 unicode characters long.
    WeakActorName nvarchar( 255 ) collate Latin1_General_100_CI_AS not null
        constraint UK_CK_tWeakActor_WeakActorName unique,

    -- Overall storage size for datetime2(0) is the same as for datetime2(2): 7 bytes.
    -- Let's keep the better precision for it.
    CreationDate datetime2 (2) not null
        constraint DF_CK_tWeakActor_CreationDate default ( sysutcdatetime() ),

    BinDate      datetime2 (2) not null
        constraint DF_CK_tWeakActor_BinDate default ( '0001-01-01' )
);

insert into CK.tWeakActor( WeakActorId, WeakActorName ) values ( 0, N'' );

--[endscript]
