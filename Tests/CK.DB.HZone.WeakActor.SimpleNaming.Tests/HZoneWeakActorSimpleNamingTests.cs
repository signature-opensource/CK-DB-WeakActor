using System;
using CK.Core;
using CK.DB.Zone;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using NUnit.Framework;
using CK.DB.Zone.WeakActor;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.HZone.WeakActor.SimpleNaming.Tests;

public class HZoneWeakActorSimpleNamingTests
{
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>().ShouldNotBeNull();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>().ShouldNotBeNull();

    [Test]
    public void display_name_should_be_unique()
    {
        var commonName = Guid.NewGuid().ToString();
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            for( var i = 0; i < 100; i++ )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                WeakActorTable.Create( context, 1, commonName, zoneId );
            }

            var weakActors = context[WeakActorTable].Query<string>( "select DisplayName from CK.vWeakActor" );

            weakActors.ShouldBeUnique();
        }
    }

}
