using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Threading;
using Newtonsoft.Json;

namespace WoW.Messages
{
    [DataContract]
    public class Login
    {
        //[DataMember]
        //[Required]
        //public string Name;
        [DataMember]
        [Required]
        public string Password;
    }

    [DataContract]
    public abstract class BaseRequest
    {
        [DataMember]
        public Login Login;
    }

    [DataContract]
    public abstract class BaseResponse
    {
        private const string SUCCESS = "Success!";
        public BaseResponse(string message)
        {
            if (message != string.Empty)
                Message = message;
            else
                Message = SUCCESS;
        }
        [DataMember]
        public readonly string Message;
        public bool Success { get { return Message == SUCCESS; } }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class CompletedAsyncResult<T> : IAsyncResult
    {
        T data;

        public CompletedAsyncResult(T data)
        { this.data = data; }

        public T Data
        { get { return data; } }

        #region IAsyncResult Members
        public object AsyncState
        { get { return (object)data; } }

        public WaitHandle AsyncWaitHandle
        { get { throw new Exception("The method or operation is not implemented."); } }

        public bool CompletedSynchronously
        { get { return true; } }

        public bool IsCompleted
        { get { return true; } }
        #endregion
    }
}
