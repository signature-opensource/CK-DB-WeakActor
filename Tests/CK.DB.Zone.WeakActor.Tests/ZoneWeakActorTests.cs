using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.Zone.WeakActor.Tests
{
    public class ZoneWeakActorTests
    {
        WeakActorTable WeakActorTable => TestHelper.StObjMap.StObjs.Obtain<WeakActorTable>();
        ZoneTable ZoneTable => TestHelper.StObjMap.StObjs.Obtain<ZoneTable>();
        GroupTable GroupTable => TestHelper.StObjMap.StObjs.Obtain<GroupTable>();

        [Test]
        public void create_twice_the_same_weak_actor_name_on_different_zone_should_not_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId1 = ZoneTable.CreateZone( context, 1 );
                var zoneId2 = ZoneTable.CreateZone( context, 1 );

                var name = Guid.NewGuid().ToString();
                WeakActorTable.Create( context, 1, name, zoneId1 );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, name, zoneId2 ) )
                              .Should()
                              .NotThrow<SqlDetailedException>();
            }
        }

        [Test]
        public void should_be_unique_inside_a_zone()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var name = Guid.NewGuid().ToString();
                var zoneId1 = ZoneTable.CreateZone( context, 1 );
                var zoneId2 = ZoneTable.CreateZone( context, 1 );
                WeakActorTable.Create( context, 1, name, zoneId1 );
                WeakActorTable.Create( context, 1, name, zoneId2 );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, name, zoneId1 ) )
                              .Should()
                              .Throw<SqlDetailedException>();
            }
        }

        [Test]
        public void should_add_weak_actor_into_a_group_if_target_group_is_inside_weak_actor_zone()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var groupId = GroupTable.CreateGroup( context, 1, zoneId );
                var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );

                WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );
            }
        }

        [Test]
        public void should_throw_when_add_weak_actor_into_a_group_out_of_weak_actor_zone()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var weakActorZoneId = ZoneTable.CreateZone( context, 1 );
                var groupZoneId = ZoneTable.CreateZone( context, 1 );
                var groupId = GroupTable.CreateGroup( context, 1, groupZoneId );
                var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), weakActorZoneId );

                WeakActorTable.Invoking( sut => sut.AddIntoGroup( context, 1, groupId, weakActorId ) )
                          .Should()
                          .Throw<SqlDetailedException>();
            }
        }

        [Test]
        public void can_be_added_to_every_group_inside_zone()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var groupId1 = GroupTable.CreateGroup( context, 1, zoneId );
                var groupId2 = GroupTable.CreateGroup( context, 1, zoneId );
                var groupId3 = GroupTable.CreateGroup( context, 1, zoneId );
                var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );

                WeakActorTable.AddIntoGroup( context, 1, groupId1, weakActorId );
                WeakActorTable.AddIntoGroup( context, 1, groupId2, weakActorId );
                WeakActorTable.AddIntoGroup( context, 1, groupId3, weakActorId );
            }
        }

        [Test]
        public void name_and_zone_id_should_be_unique()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var weakActorName = Guid.NewGuid().ToString();
                WeakActorTable.Create( context, 1, weakActorName, zoneId );

                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, zoneId ) )
                              .Should()
                              .Throw<SqlDetailedException>();
            }
        }
    }
}
