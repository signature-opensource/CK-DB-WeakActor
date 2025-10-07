create procedure CK.sWeakActorRename
(
    @ActorId int,
    @WeakActorId int,
    @WeakActorName nvarchar( 255 )
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @WeakActorId <= 0 throw 50000, 'WeakActor.InvalidWeakActorId', 1;

    -- <PostArgumentsCheck />

    if exists( select 1 from CK.tWeakActor where WeakActorId = @WeakActorId )
    begin
        if exists( select 1 from CK.tWeakActor where WeakActorId <> @WeakActorId and WeakActorName = @WeakActorName )
            throw 50000, 'WeakActor.NameAlreadyTaken', 1;

        -- <PreUpdate />

        update CK.tWeakActor
        set WeakActorName = @WeakActorName
        where WeakActorId = @WeakActorId;

        -- <PostUpdate />
    end
end
