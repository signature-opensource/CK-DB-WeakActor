create view CK.vWeakActor
as
    select WeakActorId,
           WeakActorName,
           CreationDate
    from CK.tWeakActor;
