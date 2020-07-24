using System;
using System.Collections.Generic;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.SDK.Cryptography;
using XDS.SDK.Messaging.CrossTierTypes.Photon;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class WalletView : ConsoleViewBase
    {
        readonly Window mainWindow;
        readonly PhotonWalletManager photonWalletManager;
        readonly ProfileViewModel profileViewModel;

        public Action OnFinished;


       
        Label labelBalance;
        Label labelReceiveAddress;
        Label labelHeight;
        Label labelHash;
        Label labelMessage;

        ListView listViewTransactions;
        ListView listViewCoins;

        public WalletView(Window mainWindow) : base(mainWindow)
        {
            this.mainWindow = mainWindow;
            this.photonWalletManager = App.ServiceProvider.Get<PhotonWalletManager>();
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
        }

        public override void Create()
        {
            this.mainWindow.RemoveAll();
            this.mainWindow.Title = $"{this.profileViewModel.Name} - Wallet";

            #region summary
            var frameViewSummary = new FrameView("Summary")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Percent(40f)
            };
            this.labelBalance = new Label("Balance:") { Width = Dim.Fill() };
            this.labelReceiveAddress = new Label($"Receive Address: {this.profileViewModel.DefaultAddress}") { Y = Pos.Bottom(this.labelBalance), Width = Dim.Fill() };
            this.labelHeight = new Label("Block Height:") { Y = Pos.Bottom(this.labelReceiveAddress), Width = Dim.Fill() };
            this.labelHash = new Label("Block Hash:") { Y = Pos.Bottom(this.labelHeight), Width = Dim.Fill() };

            this.labelMessage = new Label("Status: Please press Refresh to sync the wallet.") { Y = Pos.Bottom(this.labelHash) };
            var refreshBalanceButton = new Button("Refresh All") { X = 0, Y = Pos.Bottom(this.labelMessage) + 1, };
            refreshBalanceButton.Clicked = async () =>
            {
                try
                {

                    var address = this.profileViewModel.DefaultAddress;

                    (long balance, int height, byte[] hashBlock, IPhotonOutput[] outputs, PhotonError photonError) = await this.photonWalletManager.GetPhotonOutputs(address, PhotonFlags.Spendable);


                    if (photonError == PhotonError.UnknownAddress)
                        this.labelMessage.Text = $"Your address {address} has no confirmed transactions yet.";
                    else if (photonError == PhotonError.Success)
                    {
                        this.labelBalance.Text = $"Balance: {(decimal)balance / (decimal)100_000_000} XDS";
                        this.labelHeight.Text = $"Block Height: {height}";
                        this.labelHash.Text = $"Block Hash: {hashBlock.ToHexString()}";
                        this.labelMessage.Text = $"Status: Success";

                        var txs = new List<string>();
                        foreach (var output in outputs)
                        {
                            txs.Add($"Block {output.BlockHeight} | {output.UtxoType} + {(decimal)output.Satoshis / (decimal)100_000_000} XDS | Tx: {output.HashTx.ToHexString()}");
                        }

                        this.listViewTransactions.SetSource(txs);

                        var coins = new List<string>();
                        foreach (var output in outputs)
                        {
                            coins.Add($"Utxo: {output.HashTx.ToHexString()}-{output.Index} | Value: {(decimal)output.Satoshis / (decimal)100_000_000} | Confirmations: {height - output.BlockHeight +1} | Type: {output.UtxoType} ");
                        }

                        this.listViewCoins.SetSource(coins);
                    }
                    else
                        this.labelMessage.Text = $"Status: Photon Error {photonError}";
                }
                catch (Exception e)
                {
                    this.labelMessage.Text = $"Status: {e.GetType().Name}: {e.Message}";
                }

            };
            frameViewSummary.Add(this.labelBalance,this.labelReceiveAddress, this.labelHeight, this.labelHash, refreshBalanceButton, this.labelMessage);

            this.mainWindow.Add(frameViewSummary);

            #endregion

            #region transactions
            var frameViewTransactions = new FrameView("Transactions")
            {
                X = 0,
                Y = Pos.Bottom(frameViewSummary),
                Width = Dim.Fill(),
                Height = Dim.Percent(30f)
            };

            this.listViewTransactions = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            frameViewTransactions.Add(this.listViewTransactions);

            this.mainWindow.Add(frameViewTransactions);

            #endregion

            #region coins

            var frameViewCoinControl = new FrameView("Unspent Outputs")
            {
                X = 0,
                Y = Pos.Bottom(frameViewTransactions),
                Width = Dim.Fill(),
                Height = Dim.Percent(30f)
            };
            this.listViewCoins = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            frameViewCoinControl.Add(this.listViewCoins);

            this.mainWindow.Add(frameViewCoinControl);

            #endregion

            //this.walletLabel = new Label($"Wallet: Querying Balance...")
            //{
            //    X = 0,
            //    Y = 1,
            //    Height = 1,
            //    Width = Dim.Fill()
            //};


        }

        public override void Stop()
        {
            base.Stop();
            this.mainWindow.RemoveAll();
            this.OnFinished();
        }
    }
}
