using CK.Core;

namespace CK.DB.Zone.WeakActor.SimpleNaming;

/// <summary>
/// Package for Zone.WeakActor.SimpleNaming
/// </summary>
[SqlPackage( ResourcePath = "Res", ResourceType = typeof( Package ) )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:vWeakActor" )]
public abstract class Package : Zone.SimpleNaming.Package { }
