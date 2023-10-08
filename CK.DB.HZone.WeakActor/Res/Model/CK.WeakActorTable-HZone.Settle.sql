-- SetupConfig: {}
create or alter procedure [CK].[sWeakActorZoneMove](
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

            declare @HasAnyGroupOutOfHierarchy bit = 0;

            with AllZonesInNewHierarchy as
                     (select zDescendants.ZoneId as ZoneIdInHierarchy
                      from CK.tZone zone
                               join CK.tZone zDescendants on zDescendants.HierarchicalId.IsDescendantOf
                                                                 (
                                                                 zone.HierarchicalId.GetAncestor
                                                                     ((
                                                                     -- 0 is the minimum value that can be passed as argument.
                                                                     select top (1) max(HLevel)
                                                                     from (values (0), (cast(zone.HierarchicalId.GetLevel() as int) - 1)) as HLevels(HLevel)))
                                                                 ) = 1
                      where zone.ZoneId = @NewZoneId),

                 AllGroupsInNewHierarchy as
                     (select GroupId
                      from AllZonesInNewHierarchy az
                               join CK.tGroup g on g.ZoneId = az.ZoneIdInHierarchy)

            select @HasAnyGroupOutOfHierarchy = 1
            from CK.tActorProfile ap
            left join AllGroupsInNewHierarchy ag on ag.GroupId = ap.GroupId
            where ap.ActorId = @WeakActorId
              and ap.GroupId != @WeakActorId
              and ap.GroupId != @CurrentZoneId
              and ag.GroupId is null

            if (@HasAnyGroupOutOfHierarchy = 1)
                --</AnyGroupConflicting>
                begin
                    ;throw 50000, 'WeakActor.IsInAGroup', 1
                end

        end
    else
        if @Option = 1 -- Intersect
            begin
                declare @GroupId int;
                declare @GroupMatcher cursor;

                set @GroupMatcher = cursor local fast_forward for
                    --<GroupsToRemove>
                    with AllZonesInNewHierarchy as
                             (select zDescendants.ZoneId as ZoneIdInHierarchy
                              from CK.tZone zone
                                       join CK.tZone zDescendants on zDescendants.HierarchicalId.IsDescendantOf
                                                                         (
                                                                         zone.HierarchicalId.GetAncestor
                                                                             ((
                                                                             -- 0 is the minimum value that can be passed as argument.
                                                                             select top (1) max(HLevel)
                                                                             from (values (0), (cast(zone.HierarchicalId.GetLevel() as int) - 1)) as HLevels(HLevel)))
                                                                         ) = 1
                              where zone.ZoneId = @NewZoneId),

                         AllGroupsInNewHierarchy as
                             (select GroupId
                              from AllZonesInNewHierarchy az
                                       join CK.tGroup g on g.ZoneId = az.ZoneIdInHierarchy)

                    select ap.GroupId
                    from CK.tActorProfile ap
                    left join AllGroupsInNewHierarchy ag on ag.GroupId = ap.GroupId
                    where ap.ActorId = @WeakActorId
                      and ap.GroupId != @WeakActorId
                      and ap.GroupId != @CurrentZoneId
                      and ag.GroupId is null

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

    if (@NewWeakActorName is null)
        begin
            --<PreWeakActorUpdateWithoutNewWeakActorName >

            -- The common usage would be using a @NewWeakActorName,
            -- so the @WeakActorName is resolved only here.
            declare @WeakActorName nvarchar(255);
            select @WeakActorName = WeakActorName
            from CK.tWeakActor w
            where w.WeakActorId = @WeakActorId;

            if (select CK.fIsWeakActorNameInHierarchy(@NewZoneId, @WeakActorName, @CurrentZoneId)) = 1
                begin
                    ;throw 50000, 'WeakActor.WeakActorNameShouldBeUniqueInHZone', 1;
                end
            --</PreWeakActorUpdateWithoutNewWeakActorName>

            update CK.tWeakActor
            set ZoneId = @NewZoneId
            where WeakActorId = @WeakActorId;
        end
    else
        begin
            --<PreWeakActorUpdateWithNewWeakActorName >

            if (select CK.fIsWeakActorNameInHierarchy(@NewZoneId, @NewWeakActorName, @CurrentZoneId)) = 1
                begin
                    ;throw 50000, 'WeakActor.WeakActorNameShouldBeUniqueInHZone', 1;
                end
            --</PreWeakActorUpdateWithNewWeakActorName>

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
