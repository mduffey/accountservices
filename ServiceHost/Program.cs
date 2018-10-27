using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Description;
using System.IO;

namespace WoW.Services.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            WebServiceHost accountHost = CreateHost("http://localhost:8080/account", typeof(AccountService), typeof(IAccountService));
            WebServiceHost characterHost = CreateHost("http://localhost:8080/character", typeof(CharacterService), typeof(ICharacterService));
            
            accountHost.Open();
            characterHost.Open();

            Console.WriteLine("WoW Services are running." + Environment.NewLine);
            Console.WriteLine(PrintAddresses("Account", accountHost));
            Console.WriteLine(PrintAddresses("Character", characterHost));
            ConsoleKey key;
            do
            {
                Console.WriteLine();
                while (Console.KeyAvailable)
                    Console.ReadLine();
                Console.Write("Press Q to quit, T to test, X to destroy current file store: ");
                key = Console.ReadKey().Key;
                switch (key)
                {
                    case ConsoleKey.Q:
                        break;
                    case ConsoleKey.X:
                        Console.WriteLine();
                        Console.WriteLine("Note: This will not work if any service have been called since launch.");
                        Console.Write(Environment.NewLine + "Type in \"DELETE\", with caps, and hit Enter if you're sure: ");
                        string response = Console.ReadLine();
                        if (response != "DELETE" && response != "\"DELETE\"")
                        {
                            Console.WriteLine(Environment.NewLine + "\"DELETE not entered. Information will not be deleted.\"");
                        }
                        else
                        {
                            if (key == ConsoleKey.X)
                                Directory.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "WoW" + Path.DirectorySeparatorChar + "data", true);
                        }
                        break;
                    case ConsoleKey.T:
                        Tests.TestMain tests = new Tests.TestMain("http://localhost:8080/account", "http://localhost:8080/character");
                        tests.TestSelect();
                        break;
                    default:
                        Console.Clear();
                        Console.WriteLine("WoW Services are running." + Environment.NewLine);
                        Console.WriteLine(PrintAddresses("Account", accountHost));
                        Console.WriteLine(PrintAddresses("Character", characterHost));
                        Console.WriteLine(Environment.NewLine + "I said which keys to use!");
                        break;
                }    
            } while (key != ConsoleKey.Q);
            accountHost.Close();
            accountHost = null;
            characterHost.Close();
            characterHost = null;
        }

        private static WebServiceHost CreateHost(string uri, Type serviceType, Type contractType)
        {
            WebServiceHost result = new WebServiceHost(serviceType);
            result.AddServiceEndpoint(contractType, new WebHttpBinding(), uri);
            result.Description.Behaviors.Add(CreateBehavior(uri));
#if DEBUG
            ServiceBehaviorAttribute beAt = (ServiceBehaviorAttribute)result.Description.Behaviors[typeof(ServiceBehaviorAttribute)];
            
            beAt.IncludeExceptionDetailInFaults = true;
#endif
            
            return result;
        }

        private static ServiceMetadataBehavior CreateBehavior(string uri)
        {
            ServiceMetadataBehavior result = new ServiceMetadataBehavior();
            result.HttpGetEnabled = true;
            result.HttpGetUrl = new Uri(uri + "/mex");
            return result;
        }

        private static string PrintAddresses(string name, ServiceHost host)
        {
            string result = name + ":" + Environment.NewLine + "\tAddresses:" + Environment.NewLine;
            foreach (System.ServiceModel.Dispatcher.ChannelDispatcher dispatcher in host.ChannelDispatchers)
            {
                result += "\t" + dispatcher.Listener.Uri + Environment.NewLine;
            }
            result += Environment.NewLine;
            return result;
        }
    }
}
