-- SetupConfig: {}
create procedure CK.sWeakActorZoneMove
(
    @ActorId int,
    @WeakActorId int,
    @NewZoneId int,
    @Option int, -- not null enum { "None": 0, "Intersect": 1 }
    @NewWeakActorName nvarchar(255) = null
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @NewZoneId < 0 throw 50000, 'Zone.InvalidId', 1;

    --[beginsp]

    declare @CurrentZoneId int;
    select @CurrentZoneId = w.ZoneId
    from CK.tWeakActor w
    where w.WeakActorId = @WeakActorId;

    if (@CurrentZoneId is null)
    begin
        ;throw 50000, 'WeakActor.InvalidWeakActor', 1;
    end

    --<PreWeakActorCheck revert />

    if @Option = 0 -- None
    begin
        --<AnyGroupConflicting>
        if exists
        (
            select GroupId
            from CK.tActorProfile ap
            where ap.ActorId = @WeakActorId
              and ap.GroupId != @WeakActorId
              and ap.GroupId != @CurrentZoneId
        )
        --</AnyGroupConflicting>
        begin
            ;throw 50000, 'WeakActor.IsInAGroup', 1
        end
    end
    else if @Option = 1 -- Intersect
    begin
        declare @GroupId int;
        declare @GroupMatcher cursor;

        set @GroupMatcher = cursor local fast_forward for
            --<GroupsToRemove>
            select GroupId
            from CK.tActorProfile ap
            where ap.ActorId = @WeakActorId
              and ap.GroupId != @WeakActorId
              and ap.GroupId != @CurrentZoneId;
            --</GroupsToRemove>

        open @GroupMatcher;
        fetch from @GroupMatcher into @GroupId;

        while @@FETCH_STATUS = 0
            begin
                exec CK.sGroupUserRemove @ActorId, @GroupId, @WeakActorId;
                fetch next from @GroupMatcher into @GroupId;
            end

        deallocate @GroupMatcher;
    end
    else
    begin
        ;throw 50000, 'ArgumentNotSupported', 1;
    end

    --<PostWeakActorCheck />

    --<PreWeakActorMove revert />

    -- With only Zone, no HZone, we can rely on the table constraint.
    -- Here it will throw if WeakActorName is not unique within the Zone.
    if (@NewWeakActorName is null)
    begin
        --<PreWeakActorUpdateWithoutNewWeakActorName />
        update CK.tWeakActor
        set ZoneId = @NewZoneId
        where WeakActorId = @WeakActorId;
    end
    else
    begin
        --<PreWeakActorUpdateWithNewWeakActorName />
        update CK.tWeakActor
        set ZoneId        = @NewZoneId,
            WeakActorName = @NewWeakActorName
        where WeakActorId = @WeakActorId;
    end

    update CK.tActorProfile
    set GroupId = @NewZoneId
    where ActorId = @WeakActorId
      and GroupId = @CurrentZoneId;
    --<PostWeakActorMove />

    --[endsp]

end
