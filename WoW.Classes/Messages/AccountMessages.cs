using System.Runtime.Serialization;
using WoW.Classes;

/// <summary>
/// Holds the incoming and outgoing data messages between the services and the client.
/// </summary>
namespace WoW.Messages.Accounts
{
    #region Input

    [DataContract]
    public class AccountRequest : BaseRequest
    {

    }

    #endregion

    #region Output

    

    [DataContract]
    public class AccountResponse: BaseResponse
    {
        public AccountResponse(string message, PublicAccount account): base(message)
        {
            Account = account;
        }
        [DataMember]
        public readonly PublicAccount Account;
    }

    [DataContract]
    public class CharacterResponse: BaseResponse
    {
        public CharacterResponse(string message, Character character): base(message)
        {
            Character = character;
        }
        [DataMember]
        public readonly Character Character;
    }

    #endregion
}
