using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using WoW.Messages.Characters;

namespace WoW.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface ICharacterService
    {
        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = "/{acctname}/{name}", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginCreate(CreateRequest request, string acctname, string name, AsyncCallback callback, object asyncState);
        CharacterResponse EndCreate(IAsyncResult result);

        //Something of a personal design approach: I use PUT requests with headers instead of GET
        //requests when writing a GET in a RESTful implementation when the GET requests are
        //being authenticated. The alternative is to use standard authentication, but I prefer
        //building token-based login systems that dump them in the header instead. This only 
        //authenticates with login/pass, of course, but implementing a token in this setup would be
        //fairly easy an adjustment.
        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "PUT", UriTemplate = "/{acctname}", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginGetCharacters(GetRequest request, string acctname, AsyncCallback callback, object asyncState);
        CharactersResponse EndGetCharacters(IAsyncResult result);
        
        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = "/{acctname}/{name}?level={level}", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginSetLevel(CharacterBaseRequest request, string acctname, string name, byte level, AsyncCallback callback, object asyncState);
        CharacterResponse EndSetLevel(IAsyncResult result);

        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = "/{acctname}/{name}/update", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginUpdate(UpdateRequest request, string acctname, string name, AsyncCallback callback, object asyncState);
        CharacterResponse EndUpdate(IAsyncResult result);

        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = "/{acctname}/{name}/namechange", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginChangeName(NameRequest request, string acctname, string name, AsyncCallback callback, object asyncState);
        CharacterResponse EndChangeName(IAsyncResult result);

        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "DELETE", UriTemplate = "/{acctname}/{name}", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginDelete(CharacterBaseRequest request, string acctname, string name, AsyncCallback callback, object asyncState);
        CharacterResponse EndDelete(IAsyncResult result);

        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "POST", UriTemplate = "/{acctname}/{name}/restore", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginRestore(CharacterBaseRequest request, string acctname, string name, AsyncCallback callback, object asyncState);
        CharacterResponse EndRestore(IAsyncResult result);
    }
}
