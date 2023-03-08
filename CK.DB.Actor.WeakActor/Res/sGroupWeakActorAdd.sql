create procedure CK.sGroupWeakActorAdd
(
    @ActorId int,
    @GroupId int,
    @WeakActorId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @GroupId <= 0 throw 50000, 'Group.InvalidId', 1;

    if not exists( select 1 from CK.tWeakActor where WeakActorId = @WeakActorId )
        throw 50000, 'WeakActor.InvalidWeakActor', 1;

	--[beginsp]

    if @GroupId <> @WeakActorId and not exists( select 1 from CK.tActorProfile where GroupId = @GroupId and ActorId = @WeakActorId )
    begin
        -- If this is the System Group, only members of it can add new Users.
		if @GroupId = 1 
		begin
			if not exists( select 1 from CK.tActorProfile p where p.GroupId = 1 and p.ActorId = @ActorId ) 
				throw 50000, 'Security.ActorMustBeSytem', 1;
		end

		--<PreWeakActorAdd revert />

        insert into CK.tActorProfile( ActorId, GroupId ) values( @WeakActorId, @GroupId );

		--<PostWeakActorAdd />
    end

    --[endsp]
end
