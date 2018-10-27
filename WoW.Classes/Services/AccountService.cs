using System;
using WoW.Classes;
using WoW.Messages.Accounts;
using WoW.Messages;

namespace WoW.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class AccountService : IAccountService
    {
        public IAsyncResult BeginCreate(AccountRequest request, string acctname, AsyncCallback callback, object asyncState)
        {
            return new CompletedAsyncResult<AccountResponse>(Account.Create(acctname, request.Login.Password));
            //return new Account.Create(request.Login.Name, request.Login.Password);
        }

        public AccountResponse EndCreate(IAsyncResult result)
        {
            CompletedAsyncResult<AccountResponse> r = result as CompletedAsyncResult<AccountResponse>;
            return r.Data;
        }


        public IAsyncResult BeginGet(AccountRequest request, string acctname, AsyncCallback callback, object asyncState)
        {
            AccountResponse response;
            Account account = Account.Login(acctname, request.Login.Password);
            if (account != null)
                response = new AccountResponse(string.Empty, new PublicAccount(account.Name, account.CharacterNames, account.Faction));
            else
                response = new AccountResponse("Error: Bad username or password.", null);
            return new CompletedAsyncResult<AccountResponse>(response);
        }

        public AccountResponse EndGet(IAsyncResult result)
        {
            CompletedAsyncResult<AccountResponse> r = result as CompletedAsyncResult<AccountResponse>;
            return r.Data;
        }
    }
}
