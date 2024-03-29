﻿using System.Windows;
using System.Windows.Input;

namespace P528GUI.Windows
{
    public partial class PlotTitleWindow : Window
    {
        /// <summary>
        /// User defined plot title
        /// </summary>
        public string PlotTitle { get; private set; }

        public PlotTitleWindow()
        {
            InitializeComponent();
        }

        #region Event Handlers

        private void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            PlotTitle = tb_Title.Text;
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Cancel and close the window
        /// </summary>
        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Trigger an 'Accept' button click if user presses 'Enter' or 'Return'
        /// </summary>
        private void tb_Title_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
                Btn_Accept_Click(sender, null);
        }

        #endregion
    }
}
