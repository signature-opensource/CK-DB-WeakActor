# CK.DB.Actor.WeakActor

A WeakActor is weak because it cannot be trusted: it is a form of User (an Actor) that can log into the System simply with its name.
A weak actor can be added to a **Group** as any User: at this level it acts exactly like a User. But, when CK.DB.Zone is installed,
the CK.DB.Zone.WeakActor associates a weak actor to a unique Zone and the weak actor can then only appear in the defining Zone's Groups.

Its name (`WeakActorName`) is unique and uses the Latin1_General_100_CI_AS collation (like `tUser.UserName`), it can be disabled/enabled thanks to
a `DisableDate` (a WeakActor is disabled by default: it must be explicitly enabled after its creation).

This package extends [CK-DB-Actor](https://github.com/signature-opensource/CK-DB/tree/stable/CK.DB.Actor) by introducing a new `CK.tWeakActor` table
and basic stored procedure to create, destroy, rename, enable and disable it.

The view `CK.vWeakActor` is mirroring all fields from the table `CK.tWeakActor`. It aims to be transformed by other packages.
This package transforms the `CK.vGroupMember` view to add the `WeakActor` member type.

