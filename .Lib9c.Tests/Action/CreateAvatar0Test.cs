namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CreateAvatar0Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;

        public CreateAvatar0Test()
        {
            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive("avatar");
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Execute()
        {
            var action = new CreateAvatar0()
            {
                avatarAddress = _avatarAddress,
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));
            var ranking = new RankingState0();
            for (var i = 0; i < RankingState0.RankingMapCapacity; i++)
            {
                ranking.RankingMap[RankingState0.Derive(i)] = new HashSet<Address>().ToImmutableHashSet();
            }

            var sheets = TableSheetsImporter.ImportSheets();
            var state = new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(
                    Addresses.GoldDistribution,
                    GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize()
                )
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                )
                .SetState(Addresses.Ranking, ranking.Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
            });

            Assert.Equal(
                0,
                nextState.GetBalance(default, gold.Currency).MajorUnit
            );
            Assert.True(nextState.TryGetAgentAvatarStates(
                default,
                _avatarAddress,
                out var agentState,
                out var nextAvatarState)
            );
            Assert.True(agentState.avatarAddresses.Any());
            Assert.Equal("test", nextAvatarState.name);
            Assert.Equal(_avatarAddress, nextState.GetRankingState().RankingMap[nextAvatarState.RankingMapAddress].First());
        }

        [Theory]
        [InlineData("홍길동")]
        [InlineData("山田太郎")]
        public void ExecuteThrowInvalidNamePatterException(string nickName)
        {
            var agentAddress = default(Address);
            var avatarAddress = agentAddress.Derive("avatar");

            var action = new CreateAvatar0()
            {
                avatarAddress = avatarAddress,
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = nickName,
            };

            var state = new State();

            Assert.Throws<InvalidNamePatternException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var action = new CreateAvatar0()
            {
                avatarAddress = _avatarAddress,
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var state = new State().SetState(_avatarAddress, avatarState.Serialize());

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void ExecuteThrowAvatarIndexOutOfRangeException(int index)
        {
            var agentState = new AgentState(_agentAddress);
            var state = new State().SetState(_agentAddress, agentState.Serialize());
            var action = new CreateAvatar0()
            {
                avatarAddress = _avatarAddress,
                index = index,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            Assert.Throws<AvatarIndexOutOfRangeException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ExecuteThrowAvatarIndexAlreadyUsedException(int index)
        {
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[index] = _avatarAddress;
            var state = new State().SetState(_agentAddress, agentState.Serialize());

            var action = new CreateAvatar0()
            {
                avatarAddress = _avatarAddress,
                index = index,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            Assert.Throws<AvatarIndexAlreadyUsedException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var agentAddress = default(Address);
            var avatarAddress = agentAddress.Derive("avatar");

            var action = new CreateAvatar0()
            {
                avatarAddress = avatarAddress,
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));
            var updatedAddresses = new List<Address>()
            {
                agentAddress,
                avatarAddress,
                Addresses.GoldCurrency,
                Addresses.Ranking,
            };
            for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
            {
                var slotAddress = avatarAddress.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                updatedAddresses.Add(slotAddress);
            }

            var state = new State()
                .SetState(Addresses.Ranking, new RankingState0().Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize());

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(
                updatedAddresses.ToImmutableHashSet(),
                nextState.UpdatedAddresses
            );
        }

        [Fact]
        public void SerializeWithDotnetAPI()
        {
            var formatter = new BinaryFormatter();
            var action = new CreateAvatar0()
            {
                avatarAddress = default,
                index = 2,
                hair = 1,
                ear = 4,
                lens = 5,
                tail = 7,
                name = "test",
            };

            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (CreateAvatar0)formatter.Deserialize(ms);

            Assert.Equal(default, deserialized.avatarAddress);
            Assert.Equal(2, deserialized.index);
            Assert.Equal(1, deserialized.hair);
            Assert.Equal(4, deserialized.ear);
            Assert.Equal(5, deserialized.lens);
            Assert.Equal(7, deserialized.tail);
            Assert.Equal("test", deserialized.name);
        }
    }
}
