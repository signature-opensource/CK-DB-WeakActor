-- SetupConfig: {}
create function CK.fIsWeakActorNameInHierarchy
(
    @ZoneId int,
    @WeakActorName nvarchar(255)
)
    returns bit
as
begin

    declare @NameAlreadyExists bit = 0;

    with AllZonesInHierarchy as
    (
        select zDescendants.ZoneId as ZoneIdInHierarchy
        from CK.tZone zone
        join CK.tZone zDescendants on zDescendants.HierarchicalId.IsDescendantOf
        (
            zone.HierarchicalId.GetAncestor
            ((
                -- 0 is the minimum value that can be passed as argument.
                select top 1 max(HLevel)
                from (values (0), (cast(zone.HierarchicalId.GetLevel() as int) - 1)) as HLevels(HLevel)
            ))
        ) = 1
        where zone.ZoneId = @ZoneId
     )

    select @NameAlreadyExists = 1
    from AllZonesInHierarchy a
    join CK.tWeakActor w on w.ZoneId = a.ZoneIdInHierarchy
    where WeakActorName = @WeakActorName

    return (@NameAlreadyExists);

end
