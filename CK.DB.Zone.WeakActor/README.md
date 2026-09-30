# CK.DB.Zone.WeakActor

This package strongly binds the basic WeakActor (defined by [CK.DB.Actor.WeakActor](../CK.DB.Actor.WeakActor)) to a Zone
(from [CK.DB.Zone](https://github.com/signature-opensource/CK-DB/tree/stable/CK.DB.Zone)):
a WeakActor is scoped to a Zone (its WeakActorName is unique only in its defining Zone) and **can then only appear** in the Groups
of the defining Zone.

_A WeakActor belongs to its defining Zone and can only appear in its Zone's Groups_ is the fundamental invariant.

This is enforced by the stored procedures:
- A WeakActor is created in its defining Zone (`CK.sWeakActorCreate` with the `@ZoneId`) and cannot be removed from it
  (`Zone.CannotRemoveWeakActor`).
- Adding it to another Zone or to a Group of another Zone throws `Group.InvalidWeakActorZone`.
- When a Group is moved to another Zone, its WeakActors don't follow it:
  - with `Intersect` and `AutoUserRegistration`, they are removed from the Group;
  - with `None`, a Group that contains only WeakActors of its Zone is cleared, otherwise the move throws `Group.MemberNotInZone`.

[CK.DB.HZone.WeakActor](../CK.DB.HZone.WeakActor/README.md) extends this invariant to the child Zones of the defining Zone.

