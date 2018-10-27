using System.Collections.Generic;
using System.Runtime.Serialization;
using WoW.Classes;
using WoW.Enums;
/// <summary>
/// Holds the incoming and outgoing data messages between the services and the client.
/// </summary>
namespace WoW.Messages.Characters
{
    #region Input

    [DataContract]
    public class CharacterBaseRequest : BaseRequest
    {
        
    }

    [DataContract]
    public class GetRequest : BaseRequest
    {
    }

    [DataContract]
    public class CreateRequest : CharacterBaseRequest
    {
        [DataMember]
        public Class Class;
        [DataMember]
        public Race Race;
        [DataMember]
        public Faction Faction;
    }

    [DataContract]
    public class UpdateRequest : CharacterBaseRequest
    {
        [DataMember]
        public Class? Class;
        [DataMember]
        public Race? Race;
        [DataMember]
        public Faction? Faction;
    }

    [DataContract]
    public class NameRequest : CharacterBaseRequest 
    {
        [DataMember]
        public string NewName;
    }


    #endregion

    #region Output

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

    [DataContract]
    public class CharactersResponse : BaseResponse
    {
        public CharactersResponse(string message, List<CharacterResponse> characterResponses): base(message)
        {
            Characters = characterResponses;
        }
        [DataMember]
        public List<CharacterResponse> Characters = new List<CharacterResponse>();
    }


    #endregion

}
