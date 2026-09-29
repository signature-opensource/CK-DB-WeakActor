using CK.Core;
using CK.DB.Zone;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;
using System.Threading.Tasks;

namespace CK.DB.HZone.WeakActor.Tests;

public class HZoneWeakActorTests
{
    Package Package => SharedEngine.Map.StObjs.Obtain<Package>().ShouldNotBeNull();
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>().ShouldNotBeNull();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>().ShouldNotBeNull();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>().ShouldNotBeNull();

    [Test]
    public async Task can_add_weak_actor_to_a_group_in_hierarchy_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZone = ZoneTable.CreateZone( context, 1 );
            var zone = ZoneTable.CreateZone( context, 1, rootZone );
            var innerZone = ZoneTable.CreateZone( context, 1, zone );
            var groupInInnerZone = GroupTable.CreateGroup( context, 1, innerZone );

            var weakActor = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zone );
            await GroupTable.AddMemberAsync( context, 1, groupInInnerZone, weakActor, autoAddMemberInZone: true );
        }
    }

    [Test]
    public void should_find_a_weak_actor_name_in_hierarchy()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var weakActorName = Guid.NewGuid().ToString();
            var rootZone = ZoneTable.CreateZone( context, 1 );
            var childZone = ZoneTable.CreateZone( context, 1, rootZone );
            var otherChildZone = ZoneTable.CreateZone( context, 1, rootZone );
            var childChildZone = ZoneTable.CreateZone( context, 1, childZone );

            WeakActorTable.IsWeakActorNameInHierarchy( context, 0, weakActorName ).ShouldBeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, rootZone, weakActorName ).ShouldBeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childZone, weakActorName ).ShouldBeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, otherChildZone, weakActorName ).ShouldBeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childChildZone, weakActorName ).ShouldBeFalse();

            WeakActorTable.Create( context, 1, weakActorName, childZone );

            WeakActorTable.IsWeakActorNameInHierarchy( context, 0, weakActorName ).ShouldBeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, rootZone, weakActorName ).ShouldBeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childZone, weakActorName ).ShouldBeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, otherChildZone, weakActorName ).ShouldBeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childChildZone, weakActorName ).ShouldBeTrue();
        }
    }

    [Test]
    public async Task create_a_weak_actor_should_throw_when_weak_actor_name_candidate_is_in_hierarchy_already_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var weakActorName = Guid.NewGuid().ToString();
            var rootZone = ZoneTable.CreateZone( context, 1 );

            var childZone10 = ZoneTable.CreateZone( context, 1, rootZone );
            var childZone11 = ZoneTable.CreateZone( context, 1, childZone10 );
            var childZone12 = ZoneTable.CreateZone( context, 1, childZone10 );

            var childZone20 = ZoneTable.CreateZone( context, 1, rootZone );
            var childZone21 = ZoneTable.CreateZone( context, 1, childZone20 );
            var childZone22 = ZoneTable.CreateZone( context, 1, childZone20 );

            await WeakActorTable.CreateAsync( context, 1, weakActorName, childZone22 );

            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString() );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), rootZone );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZone10 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZone11 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZone12 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZone20 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZone21 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZone22 );

            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, weakActorName ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );

            Should.Throw<SqlDetailedException>( () => WeakActorTable.Create( context, 1, weakActorName, rootZone ) )
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            Should.Throw<SqlDetailedException>( () => WeakActorTable.Create( context, 1, weakActorName, childZone10 ) )
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            Should.Throw<SqlDetailedException>( () => WeakActorTable.Create( context, 1, weakActorName, childZone11 ) )
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            Should.Throw<SqlDetailedException>( () => WeakActorTable.Create( context, 1, weakActorName, childZone12 ) )
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            Should.Throw<SqlDetailedException>( () => WeakActorTable.Create( context, 1, weakActorName, childZone20 ) )
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            Should.Throw<SqlDetailedException>( () => WeakActorTable.Create( context, 1, weakActorName, childZone21 ) )
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            Should.Throw<SqlDetailedException>( () => WeakActorTable.Create( context, 1, weakActorName, childZone22 ) )
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
        }
    }

}
