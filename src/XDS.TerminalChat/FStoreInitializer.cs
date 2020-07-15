using System;
using System.Diagnostics;
using System.IO;
using XDS.Messaging.SDK.ApplicationBehavior.Models.Chat;
using XDS.SDK.Messaging.BlockchainClient.Data;
using XDS.SDK.Messaging.CrossTierTypes.FStore;
using XDS.SDK.Messaging.MessageHostClient.Data;

namespace XDS.Messaging.TerminalChat
{
    public static class FStoreInitializer
	{

		public static readonly FStoreConfig FStoreConfig = new FStoreConfig
		{
			DefaultStoreName = "FStore",
			StoreLocation = GetStoreLocation(),
			Initializer = InitFStore
		};

		static DirectoryInfo GetStoreLocation()
		{
			try
            {
                //Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "XDSChatData");
                ProcessModule processModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    var path = Path.Combine(Path.GetDirectoryName(processModule.FileName),".xdschatdata");
                    Console.WriteLine($"Data store location: {path}");
                    return new DirectoryInfo(path);
                }

                return new DirectoryInfo(".xdschatdata");
            }
			catch (Exception e)
			{
				Console.WriteLine($"Error in GetStoreLocation(): {e.Message}");
				Environment.Exit(1);

			}

			return null;
		}

		public static void InitFStore()
		{
			
			var fStore = new FStoreMono(FStoreConfig);
			if (!fStore.StoreExists())
				fStore.CreateStore();

			var profilesTable = new FSTable(nameof(Profile), IdMode.UserGenerated); // Single item, Id does not matter but speeds things up
			if (!fStore.TableExists(profilesTable, null))
				fStore.CreateTable(profilesTable);
			FStoreTables.TableConfig[typeof(Profile)] = profilesTable;

			var contactsTable = new FSTable(nameof(Identity), IdMode.UserGenerated); // Id is necessary to retrieve an item
			if (!fStore.TableExists(contactsTable, null))
				fStore.CreateTable(contactsTable);
			FStoreTables.TableConfig[typeof(Identity)] = contactsTable;

			var messagesTable = new FSTable(nameof(Message), IdMode.Auto, true, true); // e.g. /tbl_Message/1234567890/999999
			if (!fStore.TableExists(messagesTable, null))                 //       /[page: recipientId]/[auto-id]
				fStore.CreateTable(messagesTable);
			FStoreTables.TableConfig[typeof(Message)] = messagesTable;

			var peersTable = new FSTable(nameof(Peer), IdMode.UserGenerated); // Id is necessary to retrieve an item
			if (!fStore.TableExists(peersTable, null))
				fStore.CreateTable(peersTable);
			FStoreTables.TableConfig[typeof(Peer)] = peersTable;

            var messageRelayRecordsTable = new FSTable(nameof(MessageRelayRecord), IdMode.UserGenerated); // Id is necessary to retrieve an item
            if (!fStore.TableExists(messageRelayRecordsTable, null))
                fStore.CreateTable(messageRelayRecordsTable);
            FStoreTables.TableConfig[typeof(MessageRelayRecord)] = messageRelayRecordsTable;
		}
	}
}
