create procedure CK.sWeakActorRestore
(
    @ActorId int,
    @WeakActorId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @WeakActorId <= 0 throw 50000, 'WeakActor.InvalidWeakActorId', 1;

    --<PostArgumentsCheck />

    if exists( select 1 from CK.tWeakActor where WeakActorId = @WeakActorId )
    begin
        --<PreUpdate />

        update CK.tWeakActor
        set BinDate = '0001-01-01'
        where WeakActorId = @WeakActorId;

        --<PostUpdate />
    end
end
