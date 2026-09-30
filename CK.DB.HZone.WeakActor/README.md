# CK.DB.HZone.WeakActor

This package adapts the WeakActor bound to a Zone (defined by [CK.DB.Zone.WeakActor](../CK.DB.Zone.WeakActor/README.md)) to the
hierarchical Zones of [CK.DB.HZone](https://github.com/signature-opensource/CK-DB/tree/stable/CK.DB.HZone).

A WeakActor still has a defining Zone, but it is **reachable** from its defining Zone and from all the child Zones (recursively)
of its defining Zone: it can be a member of these Zones and of their Groups.
It is never a member of the Zones above its defining Zone.

The root Zone 0 is the ultimate parent Zone: a WeakActor defined in the Zone 0 is reachable from every Zone and can be in any Zone
and Group (like any member, it is implicitly in the Zone 0 and must be registered in the parent Zones of a Zone). Its name is unique
in the whole hierarchy.

## WeakActorName uniqueness

The WeakActorName is unique **along a hierarchy branch**: a WeakActor cannot have the same name as a WeakActor defined in a parent
or in a child Zone of its defining Zone. Sibling branches can reuse a name.

The `UK_CK_tWeakActor_WeakActorName_ZoneId` constraint (unique in the defining Zone) is not enough: this is checked by the
stored procedures that throw `WeakActor.WeakActorNameShouldBeUniqueInHZone`:
- `CK.sWeakActorCreate`;
- the Zone move (`CK.sZoneMove` or `CK.sGroupMove` on a Zone): the names defined in the moved Zone and its child Zones must not exist
  in the new parent Zone or its parents.

## Membership

- Adding a WeakActor to a Group (`CK.sGroupMemberAdd`) or to a Zone (`CK.sZoneMemberAdd`) that is not its defining Zone or one of its
  child Zones throws `Group.InvalidWeakActorZone`.
- Like any member, a WeakActor must be registered in the Zones between its defining Zone and the target Zone: with
  `autoAddMemberInParentZone` (or `autoAddMemberInZone` for a Group), it is automatically registered in them, otherwise
  `Zone.MemberNotInParentZone` is thrown.
  The Zones above the defining Zone are never considered.
- Removing a WeakActor from a Zone removes it from the child Zones and their Groups (standard HZone behavior). It cannot be removed
  from its defining Zone (`Zone.CannotRemoveWeakActor`).

## Moving a Group or a Zone

When a Group is moved to another Zone (or a Zone is moved to another parent Zone):
- The WeakActors that are no more reachable (their defining Zone is not the target Zone nor one of its parents) are **always removed**,
  whatever the `GroupMoveOption` is. For a Zone, this removes them from its child Zones and their Groups.
- The WeakActors defined in a moved Zone (or in its child Zones) follow it: they are always reachable.
- The other WeakActors are reachable from the target Zone: they are handled like Users by the `GroupMoveOption`:
  - `None`: the move throws `Group.MemberNotInZone` if one of them is not in the target Zone;
  - `Intersect`: the ones that are not in the target Zone are removed from the Group;
  - `AutoUserRegistration`: the ones that are not in the target Zone are registered in it (like a User, they must already be
    in the Zones between their defining Zone and the target Zone).

This is the same invariant as the non hierarchical CK.DB.Zone.WeakActor: a WeakActor never appears out of its reachable Zones.
