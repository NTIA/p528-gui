﻿using System.Windows;

namespace P528GUI.Windows
{
    public partial class ExportOptionsWindow : Window
    {
        public bool IsRowAlignedData { get; private set; }

        public bool IncludeFreeSpaceLoss { get; private set; }

        public bool IncludeModeOfPropagation { get; private set; }

        public bool ShowMinimum
        {
            set
            {
                cb_FreeSpaceLoss.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                cb_ModeOfPropagation.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public ExportOptionsWindow()
        {
            InitializeComponent();
        }

        private void Btn_Export_Click(object sender, RoutedEventArgs e)
        {
            IsRowAlignedData = rb_RowAlignedData.IsChecked.Value;
            IncludeFreeSpaceLoss = cb_FreeSpaceLoss.IsChecked.Value;
            IncludeModeOfPropagation = cb_ModeOfPropagation.IsChecked.Value;

            this.DialogResult = true;
            this.Close();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
