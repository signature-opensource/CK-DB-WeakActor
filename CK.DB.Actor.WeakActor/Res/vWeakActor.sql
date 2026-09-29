create view CK.vWeakActor
as
    select w.WeakActorId,
           w.WeakActorName,
           w.CreationDate,
           DisplayName = w.WeakActorName,
           w.DisableDate
    from CK.tWeakActor w;
