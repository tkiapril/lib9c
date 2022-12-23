namespace Lib9c.Tests.Action.Coupons
{
    using System;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Action.Coupons;
    using Nekoyume.Model.Coupons;
    using Xunit;

    public class RedeemCouponTest
    {
        [Fact]
        public void Execute()
        {
            IAccountStateDelta state = new Lib9c.Tests.Action.State();
            IRandom random = new TestRandom();

            var agent1Avatar0Address = CouponsFixture.AgentAddress1
                .Derive(SerializeKeys.AvatarAddressKey)
                .Derive("avatar-states-0");
            var agent1Avatar1Address = CouponsFixture.AgentAddress1
                .Derive(SerializeKeys.AvatarAddressKey)
                .Derive("avatar-states-1");
            var agent2Avatar0Address = CouponsFixture.AgentAddress2
                .Derive(SerializeKeys.AvatarAddressKey)
                .Derive("avatar-states-0");

            // can't redeem a coupon with an arbitrary guid
            Assert.Equal(
                state,
                new RedeemCoupon(
                    new Guid("AEB63B38-1850-4003-B549-19D37B37AC89"),
                    agent1Avatar0Address)
                    .Execute(
                    new ActionContext
                    {
                        PreviousStates = state,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    }));

            var agent1CouponWallet = state.GetCouponWallet(CouponsFixture.AgentAddress1);
            var agent2CouponWallet = state.GetCouponWallet(CouponsFixture.AgentAddress2);

            var guid1 = new Guid("9CB96C65-3D47-4BAD-8BE6-18D97042B6C9");
            var guid2 = new Guid("85BECFD9-7F5A-4C14-A2E7-C6EA83A23758");
            var guid3 = new Guid("BC911842-11E3-48EB-BFFC-3719465718A5");

            agent1CouponWallet = agent1CouponWallet
                .Add(guid1, new Coupon(guid1, CouponsFixture.RewardSet1))
                .Add(guid2, new Coupon(guid2, CouponsFixture.RewardSet2));
            agent2CouponWallet = agent2CouponWallet
                .Add(guid3, new Coupon(guid3, CouponsFixture.RewardSet3));

            state = state
                .SetCouponWallet(CouponsFixture.AgentAddress1, agent1CouponWallet)
                .SetCouponWallet(CouponsFixture.AgentAddress2, agent2CouponWallet);

            var rehearsedState = new RedeemCoupon(
                    guid1,
                    agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousStates = state,
                        Rehearsal = true,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });

            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(agent1Avatar0Address));

            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(
                    agent1Avatar0Address.Derive(SerializeKeys.LegacyInventoryKey)));

            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(
                    agent1Avatar0Address.Derive(SerializeKeys.LegacyWorldInformationKey)));

            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(
                    agent1Avatar0Address.Derive(SerializeKeys.LegacyQuestListKey)));

            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(
                    CouponsFixture.AgentAddress1.Derive(SerializeKeys.CouponWalletKey)));

            // can't redeem other person's coupon
            var expected = state.GetAvatarStateV2(agent1Avatar0Address);
            state = new RedeemCoupon(
                    guid3,
                    agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousStates = state,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            Assert.Equal(
                    expected,
                    state.GetAvatarStateV2(agent1Avatar0Address));
            Assert.Equal(
                agent2CouponWallet,
                state.GetCouponWallet(CouponsFixture.AgentAddress2));

            // can't redeem to a nonexistent avatar
            state = new RedeemCoupon(
                    guid1,
                    agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousStates = state,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            Assert.Null(
                state.GetAvatarStateV2(
                    CouponsFixture.AgentAddress1
                        .Derive(SerializeKeys.AvatarAddressKey)
                        .Derive("avatar-states-2")));
            Assert.Equal(
                agent1CouponWallet,
                state.GetCouponWallet(CouponsFixture.AgentAddress1));
        }
    }
}
