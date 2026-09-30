using CK.Core;

namespace CK.DB.HZone.WeakActor;

/// <summary>
/// Package for HZone.WeakActor
/// </summary>
[SqlPackage( ResourcePath = "Res", ResourceType = typeof( Package ) )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:sWeakActorCreate, transform:sGroupMemberAdd, transform:sZoneMemberAdd, transform:sGroupMove" )]
public abstract class Package : Zone.WeakActor.Package
{
    void StObjConstruct( HZone.Package hZone ) { }
}
