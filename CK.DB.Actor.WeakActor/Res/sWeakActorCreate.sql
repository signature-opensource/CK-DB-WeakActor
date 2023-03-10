-- SetupConfig: {}

alter procedure CK.sWeakActorCreate
(
    @ActorId int,
    @WeakActorName nvarchar( 255 ),
    @WeakActorIdResult int output
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;

    --[beginsp]

    --<PreCreate revert />

    exec CK.sActorCreate @ActorId, @WeakActorIdResult output;
    insert into CK.tWeakActor ( WeakActorId, WeakActorName ) values ( @WeakActorIdResult, @WeakActorName );

    --<PostCreate />

    --[endsp]
end

