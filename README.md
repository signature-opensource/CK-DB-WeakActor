# CK-DB-Actor-WeakActor

[![Nuget](https://img.shields.io/nuget/vpre/CK.DB.Actor.WeakActor.svg)](https://www.nuget.org/packages/CK.DB.Actor.WeakActor/)
[![Licence](https://img.shields.io/github/license/Invenietis/CK-DB-Actor-WeakActor.svg)](https://github.com/Invenietis/CK-DB-Actor-WeakActor/blob/develop/LICENSE)

A WeakActor is not a user.

A WeakActor is a kind of Actor that has a low complexity, aimed to be used as an auto login to perform simple tasks associated with a name.
Considering this, a WeakActor is defined by its unique WeakActorName.

Extend [CK-DB-Actor](https://github.com/Invenietis/CK-DB/tree/develop/CK.DB.Actor) that is a minimal model that handles Users and Groups.

## Actor.WeakActor

Create a weak actor with a **unique name**.

You can identify any weak actor with an **id**, and monitor it with its unique **name**.

A weak actor can be added to a **Group** as any actor, with its [specific stored procedure](CK.DB.Actor.WeakActor/Res/sGroupWeakActorAdd.sql).

## Zone.WeakActor

Adds the support of Zones that are Groups and contains a set of Groups. Zones implement a
one-level only group hierarchies.

Create a weak actor within a **unique zone**.

You can identity any weak actor with an id, and monitor it with its unique couple of **Name/Zone**.

## HZone.WeakActor

Extends the CK.DB.Zone to be hierarchical. Thanks to this package, Zones (that are Groups) can be
subordinated to a parent Zone.

