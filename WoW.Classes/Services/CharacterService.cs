using System;
using System.Collections.Generic;
using WoW.Classes;
using WoW.Messages.Characters;
using WoW.Messages;

namespace WoW.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class CharacterService : ICharacterService
    {
        public IAsyncResult BeginCreate(CreateRequest request, string acctname, string name, AsyncCallback callback, object asyncState)
        {
            CharacterResponse response = null;
            Account account = Account.Login(acctname, request.Login.Password);
            if (account != null)
            {
                response = Character.Create(account, name, request.Class, request.Race, request.Faction);
            }
            else
                response = new CharacterResponse("Error: Bad username or password.", null);
            return new CompletedAsyncResult<CharacterResponse>(response);
        }

        public CharacterResponse EndCreate(IAsyncResult result)
        {
            CompletedAsyncResult<CharacterResponse> r = result as CompletedAsyncResult<CharacterResponse>;
            return r.Data;
        }

        public IAsyncResult BeginGetCharacters(GetRequest request, string acctname, AsyncCallback callback, object asyncState)
        {
            Account account = Account.Login(acctname, request.Login.Password);
            string message = string.Empty;
            List<CharacterResponse> chars = null;
            if (account != null)
            {
                chars = new List<CharacterResponse>();
                foreach (string charnames in account.CharacterNames)
                {
                    chars.Add(Character.Load(account.Name, charnames));
                }
            }
            else
                message = "Error: Bad username or password.";
            return new CompletedAsyncResult<CharactersResponse>(new CharactersResponse(message, chars));
        }

        public CharactersResponse EndGetCharacters(IAsyncResult result)
        {
            CompletedAsyncResult<CharactersResponse> r = result as CompletedAsyncResult<CharactersResponse>;
            return r.Data;
        }

        public IAsyncResult BeginSetLevel(CharacterBaseRequest request, string acctname, string name, byte level, AsyncCallback callback, object asyncState)
        {
            CharacterResponse response = null;
            Account account = Account.Login(acctname, request.Login.Password);
            if (account != null)
            {
                response = Character.Load(acctname, name);
                if (response.Success && !response.Character.Deleted)
                {
                    string message = response.Character.SetLevel(level);
                    if (message != string.Empty)
                        response = new CharacterResponse(message, response.Character);

                }
            }
            else
                response = new CharacterResponse("Error: Bad username or password.", null);
            return new CompletedAsyncResult<CharacterResponse>(response);
        }

        public CharacterResponse EndSetLevel(IAsyncResult result)
        {
            CompletedAsyncResult<CharacterResponse> r = result as CompletedAsyncResult<CharacterResponse>;
            return r.Data;
        }

        public IAsyncResult BeginUpdate(UpdateRequest request, string acctname, string name, AsyncCallback callback, object asyncState)
        {
            CharacterResponse response = null;
            Account account = Account.Login(acctname, request.Login.Password);
            if (account != null)
            {
                response = Character.Load(acctname, name);
                if (response.Success && !response.Character.Deleted)
                {
                    string message = response.Character.UpdateCharacter(account, request.Faction, request.Race, request.Class);
                    if (message != string.Empty)
                        response = new CharacterResponse(message, response.Character);
                }
            }
            else
                response = new CharacterResponse("Error: Bad username or password.", null);
            return new CompletedAsyncResult<CharacterResponse>(response);
        }

        public CharacterResponse EndUpdate(IAsyncResult result)
        {
            CompletedAsyncResult<CharacterResponse> r = result as CompletedAsyncResult<CharacterResponse>;
            return r.Data;
        }

        public IAsyncResult BeginChangeName(NameRequest request, string acctname, string name, AsyncCallback callback, object asyncState)
        {
            CharacterResponse response = null;
            Account account = Account.Login(acctname, request.Login.Password);
            if (account != null)
            {
                response = Character.Load(acctname, name);
                if (response.Success && !response.Character.Deleted)
                {
                    string message = response.Character.SetName(account, request.NewName);
                    if (message != string.Empty)
                        response = new CharacterResponse(message, response.Character);
                }
            }
            else
                response = new CharacterResponse("Error: Bad username or password.", null);
            return new CompletedAsyncResult<CharacterResponse>(response);
        }

        public CharacterResponse EndChangeName(IAsyncResult result)
        {
            CompletedAsyncResult<CharacterResponse> r = result as CompletedAsyncResult<CharacterResponse>;
            return r.Data;
        }


        public IAsyncResult BeginDelete(CharacterBaseRequest request, string acctname, string name, AsyncCallback callback, object asyncState)
        {
            CharacterResponse response = null;
            Account account = Account.Login(acctname, request.Login.Password);
            if (account != null)
            {
                response = Character.Load(acctname, name);
                if (response.Success)
                {
                    string message = response.Character.Delete();
                    response = new CharacterResponse(message, response.Character);
                }
            }
            else
                response = new CharacterResponse("Error: Bad username or password.", null);
            return new CompletedAsyncResult<CharacterResponse>(response);
        }

        public CharacterResponse EndDelete(IAsyncResult result)
        {
            CompletedAsyncResult<CharacterResponse> r = result as CompletedAsyncResult<CharacterResponse>;
            return r.Data;
        }

        public IAsyncResult BeginRestore(CharacterBaseRequest request, string acctname, string name, AsyncCallback callback, object asyncState)
        {
            CharacterResponse response = null;
            Account account = Account.Login(acctname, request.Login.Password);
            if (account != null)
            {
                response = Character.Restore(account, name);
            }
            else
                response = new CharacterResponse("Error: Bad username or password.", null);
            
            return new CompletedAsyncResult<CharacterResponse>(response);
        }

        public CharacterResponse EndRestore(IAsyncResult result)
        {
            CompletedAsyncResult<CharacterResponse> r = result as CompletedAsyncResult<CharacterResponse>;
            return r.Data;
        }
    }
}
