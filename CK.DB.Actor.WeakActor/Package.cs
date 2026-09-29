using CK.Core;

namespace CK.DB.Actor.WeakActor;

/// <summary>
/// Package for Actor.WeakActor
/// </summary>
[SqlPackage( Schema = "CK", ResourcePath = "Res" )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:vGroupMember" )]
public abstract class Package : SqlPackage
{
    void StObjConstruct( Actor.Package actorPackage ) { }
}
