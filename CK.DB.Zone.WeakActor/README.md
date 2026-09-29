# CK.DB.Zone.WeakActor

This package strongly binds the basic WeakActor (defined by [CK.DB.Actor.WeakActor](../CK.DB.Actor.WeakActor)) to a Zone
(from [CK.DB.Zone](https://github.com/signature-opensource/CK-DB/tree/stable/CK.DB.Zone)):
a WeakActor is scoped to a Zone (its WeakActorName is unique only in its defining Zone) and can then only appear in the Groups
of the defining Zone.

This package extends [CK-DB-Actor](https://github.com/signature-opensource/CK-DB/tree/stable/CK.DB.Actor) by introducing a new `CK.tWeakActor` table
and basic stored procedure to create, destroy, rename, enable end disable it.

The view `CK.vWeakActor` is mirroring all fields from the table `CK.tWeakActor`. It aims to be transformed by other packages.
This package transforms the `CK.vGroupMember` view to add the `WeakActor` member type.

