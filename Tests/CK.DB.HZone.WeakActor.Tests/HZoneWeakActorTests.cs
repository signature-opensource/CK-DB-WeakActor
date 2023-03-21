using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CK.DB.Zone;
using static CK.Testing.DBSetupTestHelper;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CK.DB.HZone.WeakActor.Tests
{
    public class HZoneWeakActorTests
    {
        Package Package => TestHelper.StObjMap.StObjs.Obtain<Package>();

        WeakActorTable WeakActorTable => TestHelper.StObjMap.StObjs.Obtain<WeakActorTable>();

        ZoneTable ZoneTable => TestHelper.StObjMap.StObjs.Obtain<ZoneTable>();

        GroupTable GroupTable => TestHelper.StObjMap.StObjs.Obtain<GroupTable>();

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
        public void can_add_weak_actor_to_a_group_in_hierarchy()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var rootZone = ZoneTable.CreateZone( context, 1 );
                var zone = ZoneTable.CreateZone( context, 1, rootZone );
                var innerZone = ZoneTable.CreateZone( context, 1, zone );
                var groupInInnerZone = GroupTable.CreateGroup( context, 1, innerZone );

                var weakActor = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zone );
                WeakActorTable.AddIntoGroup( context, 1, groupInInnerZone, weakActor );
            }
        }

        [Test]
        public void move_a_simple_zone_containing_a_weak_actor_with_option_0_none_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
                var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
                var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneUnderTest );

                ZoneTable.Invoking
                         (
                             sut => sut.MoveZone( context, 1, zoneUnderTest, rootZoneTarget, GroupMoveOption.None )
                         )
                         .Should()
                         .Throw<SqlDetailedException>()
                         .WithInnerException<SqlException>()
                         .WithMessage( "*Group.UserNotInZone*" );

                ZoneTable.MoveZone( context, 1, zoneUnderTest, rootZoneOrigin, GroupMoveOption.None );
            }
        }

        [Test]
        public void move_a_simple_zone_not_containing_any_weak_actor_with_option_1_none_should_not_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
                var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
                var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

                ZoneTable.MoveZone( context, 1, zoneUnderTest, rootZoneTarget, GroupMoveOption.Intersect );
                ZoneTable.MoveZone( context, 1, zoneUnderTest, rootZoneOrigin, GroupMoveOption.Intersect );
            }
        }

        [Test]
        public void move_a_simple_zone_containing_a_weak_actor_with_option_1_none_should_throw()
        {
            // sZoneMove => sGroupMove => sGroupUserRemove => sZoneUserRemove
            //TODO: Here we want either :
            // - throw because the last call (sZoneUserRemove) throws by design.
            // - Leave the weak actor in the zone, so check :
            //       * Unique name in the new HZone
            //       * Groups that are in the old hzone, not in the new hzone are not compatible
            // but the second option may be more about option 2 auto registration in a specific way for WA.
            using( var context = new SqlStandardCallContext() )
            {
                var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
                var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
                var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneUnderTest );

                ZoneTable.Invoking
                         (
                             sut => sut.MoveZone( context, 1, zoneUnderTest, rootZoneTarget, GroupMoveOption.Intersect )
                         )
                         .Should()
                         .Throw<SqlDetailedException>()
                         .WithInnerException<SqlException>()
                         .WithMessage( "*Zone.CannotRemoveWeakActor*" );
            }
        }

        [Test]
        public void move_a_simple_zone_containing_a_weak_actor_with_option_2_none_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
                var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
                var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneUnderTest );

                ZoneTable.Invoking
                         (
                             sut => sut.MoveZone
                             (
                                 context,
                                 1,
                                 zoneUnderTest,
                                 rootZoneTarget,
                                 GroupMoveOption.AutoUserRegistration
                             )
                         )
                         .Should()
                         .Throw<SqlDetailedException>()
                         .WithInnerException<SqlException>()
                         .WithMessage( "*Group.WeakActorCannotUseAutoUserRegistration*" );
            }
        }

        [Test]
        public void should_find_a_weak_actor_name_in_hierarchy()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var weakActorName = Guid.NewGuid().ToString();
                var rootZone = ZoneTable.CreateZone( context, 1 );
                var childZone = ZoneTable.CreateZone( context, 1, rootZone );
                var otherChildZone = ZoneTable.CreateZone( context, 1, rootZone );
                var childChildZone = ZoneTable.CreateZone( context, 1, childZone );

                WeakActorTable.IsWeakActorNameInHierarchy( context, 0, weakActorName ).Should().BeFalse();
                WeakActorTable.IsWeakActorNameInHierarchy( context, rootZone, weakActorName ).Should().BeFalse();
                WeakActorTable.IsWeakActorNameInHierarchy( context, childZone, weakActorName ).Should().BeFalse();
                WeakActorTable.IsWeakActorNameInHierarchy( context, otherChildZone, weakActorName ).Should().BeFalse();
                WeakActorTable.IsWeakActorNameInHierarchy( context, childChildZone, weakActorName ).Should().BeFalse();

                WeakActorTable.Create( context, 1, weakActorName, childZone );

                WeakActorTable.IsWeakActorNameInHierarchy( context, 0, weakActorName ).Should().BeTrue();
                WeakActorTable.IsWeakActorNameInHierarchy( context, rootZone, weakActorName ).Should().BeTrue();
                WeakActorTable.IsWeakActorNameInHierarchy( context, childZone, weakActorName ).Should().BeTrue();
                WeakActorTable.IsWeakActorNameInHierarchy( context, otherChildZone, weakActorName ).Should().BeTrue();
                WeakActorTable.IsWeakActorNameInHierarchy( context, childChildZone, weakActorName ).Should().BeTrue();
            }
        }

        [Test]
        public void create_a_weak_actor_should_throw_when_weak_actor_name_candidate_is_in_hierarchy_already()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var weakActorName = Guid.NewGuid().ToString();
                var rootZone = ZoneTable.CreateZone( context, 1 );

                var childZone10 = ZoneTable.CreateZone( context, 1, rootZone );
                var childZone11 = ZoneTable.CreateZone( context, 1, childZone10 );
                var childZone12 = ZoneTable.CreateZone( context, 1, childZone10 );

                var childZone20 = ZoneTable.CreateZone( context, 1, rootZone );
                var childZone21 = ZoneTable.CreateZone( context, 1, childZone20 );
                var childZone22 = ZoneTable.CreateZone( context, 1, childZone20 );

                WeakActorTable.Create( context, 1, weakActorName, childZone22 );

                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString() );
                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), rootZone );
                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone10 );
                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone11 );
                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone12 );
                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone20 );
                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone21 );
                WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone22 );

                // Why the inner message is not the same as the test i've written in CK.DB.Actor ?
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, rootZone ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone10 ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone11 ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone12 ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone20 ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone21 ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
                WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone22 ) )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            }
        }
    }
}
