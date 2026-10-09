-- Cleanup of the procedures that have been removed or renamed in v2 (WeakActor became a regular group Member).
-- The setup doesn't drop objects that are no more defined: these ones are left in upgraded databases.
if object_id('CK.sGroupWeakActorAdd') is not null drop procedure CK.sGroupWeakActorAdd;
if object_id('CK.sWeakActorArchive') is not null drop procedure CK.sWeakActorArchive;
if object_id('CK.sWeakActorRestore') is not null drop procedure CK.sWeakActorRestore;
