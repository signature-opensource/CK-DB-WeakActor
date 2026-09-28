# CK.DB.Actor.WeakActor

A WeakActor is a User (a kind of Actor) that can log into the System with its name. This WeakActorName is unique.

This package extends [CK-DB-Actor](https://github.com/signature-opensource/CK-DB/tree/stable/CK.DB.Actor) by introducing a new `CK.tWeakActor` table
and basic stored procedure to create, destroy, and rename it.

A weak actor can be added to a **Group** as any Actor.
The only view, `CK.vWeakActor` is mirroring all fields from the table `CK.tWeakActor`. It aims to be transformed by other packages.

