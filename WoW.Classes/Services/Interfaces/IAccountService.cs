using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using WoW.Messages.Accounts;

namespace WoW.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IAccountService
    {
        [OperationContractAttribute(AsyncPattern=true)]
        [WebInvoke(Method = "POST", UriTemplate="/{acctname}", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginCreate(AccountRequest request, string acctname, AsyncCallback callback, object asyncState);

        AccountResponse EndCreate(IAsyncResult result);
        [OperationContractAttribute(AsyncPattern = true)]
        [WebInvoke(Method = "PUT", UriTemplate = "/{acctname}", RequestFormat = WebMessageFormat.Json,
                   ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        IAsyncResult BeginGet(AccountRequest request, string acctname, AsyncCallback callback, object asyncState);

        AccountResponse EndGet(IAsyncResult result);
    }
}
