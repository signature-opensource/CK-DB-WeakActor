-- SetupConfig: {}

alter procedure CK.sWeakActorDestroy
(
	@ActorId int,
	@WeakActorId int
)
as
begin
	if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
	if @WeakActorId is null or @WeakActorId <= 0 throw 50000, 'Argument.InvalidActorWeakActorId', 1;

	--[beginsp]

	--<PreDestroy revert />

    delete from CK.tActorProfile where ActorId = @WeakActorId;
    delete from CK.tWeakActor where WeakActorId = @WeakActorId;
    delete from CK.tActor where ActorId = @WeakActorId;

	--<PostDestroy />


    --[endsp]
end

