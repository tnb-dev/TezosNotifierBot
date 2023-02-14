using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TezosNotifyBot.BetterCallDev;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage;
/*
namespace TezosNotifyBot.Services
{
    public class TokenService
    {
        private TezosDataContext Db { get; }
        private IBetterCallDevClient Bcd { get; }

        public TokenService(TezosDataContext db, IBetterCallDevClient bcd)
        {
            Db = db;
            Bcd = bcd;
        }

        public async Task<Domain.Token> GetToken(string contract, int tokenId)
        {
            var token = await Db.Tokens
                .SingleOrDefaultAsync(x => x.ContractAddress == contract && x.Token_id == tokenId);
            if (token != null)
                return token;

            var tokenModel = Bcd.GetToken(contract, tokenId);
            if (tokenModel == null)
                return null;

            token = new Domain.Token()
            {
                Decimals = tokenModel.decimals,
                Level = tokenModel.level,
                Name = tokenModel.name,
                Symbol = tokenModel.symbol,
                Token_id = tokenModel.token_id,
                ContractAddress = tokenModel.contract
            };

            await Db.AddAsync(token);
            await Db.SaveChangesAsync();

            return token;
        }
    }
}
*/