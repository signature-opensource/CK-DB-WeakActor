# CK.DB.Zone.WeakActor

This package strongly binds the basic WeakActor (defined by [CK.DB.Actor.WeakActor](../CK.DB.Actor.WeakActor)) to a Zone
(from [CK.DB.Zone](https://github.com/signature-opensource/CK-DB/tree/stable/CK.DB.Zone)):
a WeakActor is scoped to a Zone (its WeakActorName is unique only in its defining Zone) and **can then only appear** in the Groups
of the defining Zone.

_A WeakActor belongs to its defining Zone and can only appear in its Zone's Groups_ is the fundamental invariant.

