-- Cleanup of the function that has been removed in v2 (WeakActor became a regular group Member).
-- The setup doesn't drop objects that are no more defined: this one is left in upgraded databases.
if object_id('CK.fIsWeakActorNameInHierarchy') is not null drop function CK.fIsWeakActorNameInHierarchy;
