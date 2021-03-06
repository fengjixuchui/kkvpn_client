﻿using kkvpn_client.Communication;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace kkvpn_client.Screens
{
    /// <summary>
    /// Interaction logic for PagePeers.xaml
    /// </summary>
    public partial class PagePeers : Page
    {
        private const int RefreshTime = 1000;

        private MainWindow ParentWindow;
        private ConnectionManager Connection;

        private Timer StatsRefreshTimer;

        public PagePeers(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            this.Connection = ((App)Application.Current).Connection;
            InitializeComponent();

            Connection.OnPeerListChanged += Connection_OnPeerListChanged;

            StatsRefreshTimer = new Timer(RefreshTime);
            StatsRefreshTimer.Elapsed += StatsRefreshTimer_Elapsed;
            StatsRefreshTimer.Start();
        }

        private void StatsRefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() => {
                RefreshPeerList(Connection.GetPeers());
            });
        }

        private void RefreshPeerList(PeerData[] Peers)
        {
            lvPeerList.Items.Clear();
            foreach (PeerData peer in Peers)
            {
                lvPeerList.Items.Add(peer);
            }
        }

        void Connection_OnPeerListChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => { 
                RefreshPeerList(((PeerListChangedEventArgs)e).Peers);
                });
        }
    }
}
