--[beginscript]

create table CK.tWeakActor
(
	WeakActorId   int not null
	    constraint PK_CK_tWeakActor primary key nonclustered ( WeakActorId )
        constraint FK_CK_tWeakActor_WeakActorId foreign key references CK.tActor( ActorId ),

    WeakActorName nvarchar( 255 ) collate Latin1_General_100_CI_AS not null
        constraint UK_CK_tWeakActor_WeakActorName unique
);

insert into CK.tWeakActor( WeakActorId, WeakActorName ) values ( 0, N'' );

--[endscript]
