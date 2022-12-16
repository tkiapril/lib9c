using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Coupons
{
    [Serializable]
    [ActionType("transfer_coupons")]
    public sealed class TransferCoupons : GameAction
    {
        public TransferCoupons(
            IImmutableDictionary<Address, IImmutableSet<Guid>> couponsPerRecipient)
        {
            CouponsPerRecipient = couponsPerRecipient;
        }

        public IImmutableDictionary<Address, IImmutableSet<Guid>> CouponsPerRecipient
        {
            get;
            private set;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var signerWallet = states.GetCouponWallet(context.Signer);
            var orderedRecipients = CouponsPerRecipient.OrderBy(pair => pair.Key);
            foreach ((Address recipient, IImmutableSet<Guid> couponIds) in orderedRecipients)
            {
                var recipientWallet = states.GetCouponWallet(recipient);
                foreach (Guid id in couponIds)
                {
                    if (!signerWallet.TryGetValue(id, out var coupon))
                    {
                        throw new FailedLoadStateException(
                            $"Failed to load a coupon (id: {id})."
                        );
                    }

                    signerWallet = signerWallet.Remove(id);
                    recipientWallet = recipientWallet.Add(id, coupon);
                }

                states = states.SetCouponWallet(recipient, recipientWallet, context.Rehearsal);
            }

            states = states.SetCouponWallet(context.Signer, signerWallet, context.Rehearsal);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            CouponsPerRecipient.ToImmutableDictionary(
                pair => pair.Key.ToHex(),
                pair => (IValue)new Bencodex.Types.List(
                    pair.Value.OrderBy(id => id).Select(id => id.ToByteArray()))
            );

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue) =>
            CouponsPerRecipient = plainValue.ToImmutableDictionary(
                pair => new Address(pair.Key),
                pair => (IImmutableSet<Guid>)((Bencodex.Types.List)pair.Value).Select(
                    value => new Guid((Binary)value)
                ).ToImmutableHashSet()
            );
    }
}
