namespace Lib9c.Tests.Action.Coupons
{
    using Libplanet.Action;
    using Xunit;

    public class TransferCouponsTest
    {
        [Fact]
        public void Execute()
        {
            IAccountStateDelta state = new State();
            IRandom random = new TestRandom();
        }
    }
}
