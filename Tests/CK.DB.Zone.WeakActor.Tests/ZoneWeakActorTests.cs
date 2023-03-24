using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using static CK.Testing.DBSetupTestHelper;
using Dapper;
using Microsoft.Data.SqlClient;
using static CK.DB.Zone.WeakActor.WeakActorZoneMoveOption;

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

        [Test]
        public void two_weak_actors_can_be_added_to_the_same_zone()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var weakActor1 = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );
                var weakActor2 = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );
                weakActor1.Should().NotBe( weakActor2 );
            }
        }

        [Test]
        public void display_name_should_be_unique()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var sql = "select DisplayName from CK.vWeakActor";

                var zoneId1 = ZoneTable.CreateZone( context, 1 );
                var zoneId2 = ZoneTable.CreateZone( context, 1 );
                var weakActorName1 = Guid.NewGuid().ToString();
                var weakActorName2 = Guid.NewGuid().ToString();
                var weakActorName3 = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );
                WeakActorTable.Create( context, 1, weakActorName2, zoneId1 );
                WeakActorTable.Create( context, 1, weakActorName2, zoneId2 );
                WeakActorTable.Create( context, 1, weakActorName3, zoneId2 );
                var group = GroupTable.CreateGroup( context, 1, zoneId1 );
                WeakActorTable.AddIntoGroup( context, 1, group, weakActorId );

                var weakActors = context[WeakActorTable].Query<string>( sql );
                weakActors.Should().OnlyHaveUniqueItems();
            }
        }

        [Test]
        public void move_a_group_containing_a_weak_actor_with_option_0_none_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId1 = ZoneTable.CreateZone( context, 1 );
                var zoneId2 = ZoneTable.CreateZone( context, 1 );

                var weakActorName1 = Guid.NewGuid().ToString();
                var weakActorName2 = Guid.NewGuid().ToString();

                var weakActorId1 = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );
                var weakActorId2 = WeakActorTable.Create( context, 1, weakActorName2, zoneId2 );

                var group = GroupTable.CreateGroup( context, 1, zoneId1 );

                WeakActorTable.AddIntoGroup( context, 1, group, weakActorId1 );

                GroupTable.Invoking( sut => sut.MoveGroup( context, 1, group, zoneId2, GroupMoveOption.None ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*Group.UserNotInZone*" );
            }
        }

        [Test]
        public void move_a_group_containing_a_weak_actor_with_option_1_intersect_should_not_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId1 = ZoneTable.CreateZone( context, 1 );
                var zoneId2 = ZoneTable.CreateZone( context, 1 );

                var weakActorName = Guid.NewGuid().ToString();

                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId1 );

                var groupId = GroupTable.CreateGroup( context, 1, zoneId1 );

                var sqlCheckGroup =
                "select count(*) from CK.tActorProfile where GroupId=@GroupId and ActorId=@WeakActorId";
                var checkGroupParams = new { groupId, weakActorId };

                var checkGroupBefore = context[WeakActorTable].QuerySingle<int>( sqlCheckGroup, checkGroupParams );
                checkGroupBefore.Should().Be( 0 );

                WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );
                var checkGroup = context[WeakActorTable].QuerySingle<int>( sqlCheckGroup, checkGroupParams );
                checkGroup.Should().Be( 1 );

                GroupTable.Invoking( sut => sut.MoveGroup( context, 1, groupId, zoneId2, GroupMoveOption.Intersect ) )
                          .Should()
                          .NotThrow<SqlDetailedException>();

                var checkGroupAfter = context[WeakActorTable].QuerySingle<int>( sqlCheckGroup, checkGroupParams );
                checkGroupAfter.Should().Be( checkGroupBefore )
                               .And.Be( 0 );

                context[WeakActorTable].QuerySingle<int>
                                       (
                                           @"
select count(*)
from CK.tActorProfile
where GroupId=@ZoneId
    and ActorId=@WeakActorId
union
select count(*)
from CK.tWeakActor
where WeakActorId=@WeakActorId
    and ZoneId=@ZoneId;
",
                                           new { ZoneId = zoneId2, weakActorId }
                                       )
                                       .Should().Be( 0 );

                context[WeakActorTable].QuerySingle<int>
                                       (
                                           @"
select count(*)
from CK.tActorProfile
where GroupId=@ZoneId
    and ActorId=@WeakActorId
union
select count(*)
from CK.tWeakActor
where WeakActorId=@WeakActorId
    and ZoneId=@ZoneId;
",
                                           new { ZoneId = zoneId1, weakActorId }
                                       )
                                       .Should().Be( 1 );
            }
        }

        [Test]
        public void move_a_group_containing_a_weak_actor_with_option_2_auto_user_registration_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId1 = ZoneTable.CreateZone( context, 1 );
                var zoneId2 = ZoneTable.CreateZone( context, 1 );

                var weakActorName1 = Guid.NewGuid().ToString();
                var weakActorName2 = Guid.NewGuid().ToString();

                var weakActorId1 = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );
                var weakActorId2 = WeakActorTable.Create( context, 1, weakActorName2, zoneId2 );

                var group = GroupTable.CreateGroup( context, 1, zoneId1 );

                WeakActorTable.AddIntoGroup( context, 1, group, weakActorId1 );

                GroupTable.Invoking
                          (
                              sut => sut.MoveGroup( context, 1, group, zoneId2, GroupMoveOption.AutoUserRegistration )
                          )
                          .Should()
                          .Throw<SqlDetailedException>();
            }
        }

        [Test]
        public void move_group_which_is_weak_actor_zone_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId1 = ZoneTable.CreateZone( context, 1 );
                var zoneId2 = ZoneTable.CreateZone( context, 1 );

                var weakActorName1 = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );

                var currentGroupId = context[WeakActorTable].QuerySingle<int>
                (
                    "select GroupId from CK.tActorProfile where ActorId=@WeakActorId and GroupId!=@WeakActorId;",
                    new { WeakActorId = weakActorId }
                );
                currentGroupId.Should().Be( zoneId1 );

                GroupTable.Invoking( sut => sut.MoveGroup( context, 1, zoneId1, zoneId2, GroupMoveOption.Intersect ) )
                          .Should().Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*Zone.CannotRemoveWeakActor*" );

                var noGroupId = context[WeakActorTable].QuerySingle<int>
                (
                    "select GroupId from CK.tActorProfile where ActorId=@WeakActorId and GroupId!=@WeakActorId",
                    new { WeakActorId = weakActorId }
                );

                noGroupId.Should().Be( currentGroupId );
            }
        }

        [Test]
        public void sZoneUserRemove_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );

                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                context[WeakActorTable].QuerySingle<int>
                                       (
                                           "select count(*) from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@GroupId;",
                                           new { weakActorId, GroupId = zoneId }
                                       )
                                       .Should().Be( 1 );

                ZoneTable.Invoking( sut => sut.RemoveUser( context, 1, zoneId, weakActorId ) )
                         .Should().Throw<SqlDetailedException>();
            }
        }

        [Test]
        public void sGroupUserRemove_should_simply_remove_the_association_with_weak_actor_from_tActorProfile()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );

                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                var groupId = GroupTable.CreateGroup( context, 1, zoneId );
                WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

                context[WeakActorTable].QuerySingle<int>
                                       (
                                           "select count(*) from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@GroupId;",
                                           new { weakActorId, groupId }
                                       )
                                       .Should().Be( 1 );

                GroupTable.RemoveUser( context, 1, groupId, weakActorId );

                context[WeakActorTable].QuerySingle<int>
                                       (
                                           "select count(*) from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@GroupId;",
                                           new { weakActorId, groupId }
                                       )
                                       .Should().Be( 0 );
            }
        }

        [Test]
        public void sGroupUserRemove_which_target_a_zone_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );

                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                context[WeakActorTable].QuerySingle<int>
                                       (
                                           "select count(*) from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@GroupId;",
                                           new { weakActorId, GroupId = zoneId }
                                       )
                                       .Should().Be( 1 );

                GroupTable.Invoking( sut => sut.RemoveUser( context, 1, zoneId, weakActorId ) )
                          .Should().Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>();

                context[WeakActorTable].QuerySingle<int>
                                       (
                                           "select count(*) from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@GroupId;",
                                           new { weakActorId, GroupId = zoneId }
                                       )
                                       .Should().Be( 1 );
            }
        }

        [Test]
        public void add_a_weak_actor_to_a_group_that_is_a_zone_should_throw()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var groupZoneId = ZoneTable.CreateZone( context, 1 );

                // bypass WeakActor.OutOfZone
                GroupTable.MoveGroup( context, 1, groupZoneId, zoneId );

                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                WeakActorTable.Invoking( sut => sut.AddIntoGroup( context, 1, groupZoneId, weakActorId ) )
                              .Should().Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.AddToZoneForbidden*" );
            }
        }

        #region sWeakActorZoneMove

        [Test]
        public void should_move_weak_actor_to_a_new_zone_in_a_perfect_world()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

                WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget );

                var zoneIdResult = context[WeakActorTable].QuerySingle<int>
                (
                    "select ZoneId from CK.vWeakActor where WeakActorId=@WeakActorId",
                    new { weakActorId }
                );
                zoneIdResult.Should().Be( zoneIdTarget );

                var oldProfileResult = context[WeakActorTable].QuerySingle<int>
                (
                    "select 1 from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@ZoneIdTarget",
                    new { weakActorId, zoneIdTarget }
                );
                oldProfileResult.Should().Be( 1 );

                var newProfileResult = context[WeakActorTable].Query<int>
                (
                    "select 1 from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@ZoneId",
                    new { weakActorId, zoneId }
                );
                newProfileResult.Should().BeEmpty();

                WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );
            }
        }

        [Test]
        public void should_throw_when_moving_a_weak_actor_to_a_new_zone_where_name_clash()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                var zoneIdTarget = ZoneTable.CreateZone( context, 1 );
                WeakActorTable.Create( context, 1, weakActorName, zoneIdTarget );

                WeakActorTable.Invoking( sut => sut.MoveZone( context, 1, weakActorId, zoneIdTarget ) )
                              .Should().Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*UK_CK_tWeakActor_WeakActorName_ZoneId*" );
            }
        }

        [Test]
        public void should_not_throw_when_moving_a_weak_actor_with_a_new_unique_name()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                var zoneIdTarget = ZoneTable.CreateZone( context, 1 );
                WeakActorTable.Create( context, 1, weakActorName, zoneIdTarget );

                var displayName = context[WeakActorTable].QuerySingle<string>
                (
                    "select DisplayName from CK.vWeakActor where WeakActorId=@WeakActorId",
                    new { weakActorId }
                );
                WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget, newWeakActorName: displayName );
            }
        }

        [Test]
        public void should_throw_when_moving_a_weak_actor_to_a_new_zone_while_in_a_group()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                var groupId = GroupTable.CreateGroup( context, 1, zoneId );
                WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

                var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

                WeakActorTable.Invoking
                              (
                                  sut => sut.MoveZone( context, 1, weakActorId, zoneIdTarget, option: None )
                              )
                              .Should()
                              .Throw<SqlDetailedException>()
                              .WithInnerException<SqlException>()
                              .WithMessage( "*WeakActor.IsInAGroup*" );
            }
        }

        [Test]
        public void should_not_throw_when_moving_a_weak_actor_to_a_new_zone_while_in_a_group_with_option_1_intersect()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var zoneId = ZoneTable.CreateZone( context, 1 );
                var weakActorName = Guid.NewGuid().ToString();
                var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

                var groupId = GroupTable.CreateGroup( context, 1, zoneId );
                WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

                var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

                WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget, option: Intersect );


                var zoneIdResult = context[WeakActorTable].QuerySingle<int>
                (
                    "select ZoneId from CK.vWeakActor where WeakActorId=@WeakActorId",
                    new { weakActorId }
                );
                zoneIdResult.Should().Be( zoneIdTarget );

                var oldProfileResult = context[WeakActorTable].QuerySingle<int>
                (
                    "select 1 from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@ZoneIdTarget",
                    new { weakActorId, zoneIdTarget }
                );
                oldProfileResult.Should().Be( 1 );

                var newProfileResult = context[WeakActorTable].Query<int>
                (
                    "select 1 from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@ZoneId",
                    new { weakActorId, zoneId }
                );
                newProfileResult.Should().BeEmpty();

                WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );

                context[WeakActorTable].Query
                                       (
                                           "select * from CK.tActorProfile where ActorId=@WeakActorId and GroupId=@GroupId",
                                           new { weakActorId, groupId }
                                       )
                                       .Should().BeEmpty();
            }
        }

        #endregion
    }
}
