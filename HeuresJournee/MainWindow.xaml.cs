using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HeuresJournee
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Pointage> Pointages = new ObservableCollection<Pointage>();
        private const string POINTAGES_DATA_HANDLER = "pointages_data";
        private Journee Journee;
        private LiteDatabase db;

        public MainWindow()
        {
            InitializeComponent();
        
            string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\data.db";
            db = new LiteDatabase(path);

            dtpDate.SelectedDateChanged += DtpDate_SelectedDateChanged;
            dtpDate.SelectedDate = DateTime.Now;
        }

        private void DtpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime date = dtpDate.SelectedDate.GetValueOrDefault(DateTime.Now);
            string dateId = string.Format("{0}{1}{2}", date.Day, date.Month, date.Year);
            LoadData(dateId);
        }

        private void LoadData(string dateId)
        {
            Pointages = new ObservableCollection<Pointage>();

            var journees = db.GetCollection<Journee>(POINTAGES_DATA_HANDLER);

            var aujourdhui = journees.FindOne(j => j.DateId.Equals(dateId));

            try
            {
                var heuresManquantes = TimeSpan.Parse(txtHoursNeeded.Text);
                lblHeuresManquantes.Content = string.Format("-{0}:{1:D2}", heuresManquantes.Hours, heuresManquantes.Minutes);
                lblHeuresManquantes.Foreground = Brushes.Red;
                lblWorkTime.Content = "0:00";
            }
            catch { }

            if (aujourdhui == null)
            {
                Journee = new Journee();
                Journee.DateId = dateId;

                journees.Insert(Journee);
            }
            else
            {
                Journee = aujourdhui;
                Pointages = Journee.Pointages ?? new ObservableCollection<Pointage>();
                OrderAndCalculateTimes();
            }

            dgPointages.ItemsSource = Pointages;
        }

        private void OrderAndCalculateTimes()
        {
            if (Pointages.Count > 0)
            {
                var ordered = Pointages.OrderBy(o => o.Temps);
                Pointages = new ObservableCollection<Pointage>(ordered);
                dgPointages.ItemsSource = Pointages;

                if (Pointages.Count % 2 == 0)
                {
                    var entree = Pointages.Where(en => en.TypePointage == "Entrée").ToList();
                    var sortie = Pointages.Where(s => s.TypePointage == "Sortie").ToList();
                    TimeSpan tempsTotal = new TimeSpan();

                    if (entree.Count() == sortie.Count())
                    {
                        for (int i = 0; i < entree.Count(); i++)
                        {
                            TimeSpan difference = sortie[i].Temps - entree[i].Temps;
                            tempsTotal += difference;
                        }

                        lblWorkTime.Content = string.Format("{0}:{1:D2}", tempsTotal.Hours, tempsTotal.Minutes);

                        try
                        {
                            var tempsJournee = TimeSpan.Parse(Properties.Settings.Default.heuresParJour);
                            TimeSpan manquant = tempsTotal - tempsJournee;
                            string manquantSign = null;
                            int manquantHours = manquant.Hours;
                            int manquantMinutes = manquant.Minutes;

                            if(manquant.Hours < 0)
                            {
                                manquantHours = manquant.Hours * -1;
                                manquantSign = "-";
                                lblHeuresManquantes.Foreground = Brushes.Red;
                            }

                            if(manquant.Minutes < 0)
                            {
                                manquantMinutes = manquant.Minutes * -1;
                                manquantSign = "-";
                                lblHeuresManquantes.Foreground = Brushes.Red;
                            }

                            if(manquantSign == null)
                            {
                                manquantSign = "+";
                                lblHeuresManquantes.Foreground = Brushes.Green;
                            }

                            lblHeuresManquantes.Content = string.Format("{0}{1}:{2:D2}", manquantSign, manquantHours, manquantMinutes);
                        }
                        catch {}
                    }
                }

                Journee.Pointages = Pointages;

                var journees = db.GetCollection<Journee>(POINTAGES_DATA_HANDLER);
                journees.Update(Journee);
            }
        }

        private void dgPointages_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            OrderAndCalculateTimes();
        }

        private void dgPointages_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (dgPointages.SelectedItem != null)
            {
                (sender as DataGrid).RowEditEnding -= dgPointages_RowEditEnding;
                (sender as DataGrid).CommitEdit();
                (sender as DataGrid).Items.Refresh();
                (sender as DataGrid).RowEditEnding += dgPointages_RowEditEnding;
            }
            else return;

            OrderAndCalculateTimes();
        }

        private void txtHoursNeeded_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.heuresParJour = txtHoursNeeded.Text;
            Properties.Settings.Default.Save();

            try
            {
                TimeSpan.Parse(txtHoursNeeded.Text);
                OrderAndCalculateTimes();
            }
            catch {}
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Journee.Pointages = Pointages;
            var journees = db.GetCollection<Journee>(POINTAGES_DATA_HANDLER);
            journees.Update(Journee);

            db.Checkpoint();
            db.Dispose();
        }

        private void ManageDataGridDeleteKeyEvent(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                OrderAndCalculateTimes();
            }
        }

        private void dgPointages_KeyUp(object sender, KeyEventArgs e)
        {
            ManageDataGridDeleteKeyEvent(e);
        }

        private void dgPointages_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            ManageDataGridDeleteKeyEvent(e);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).ContextMenu.IsOpen = true;
        }

        private void mnitAPropos_Click(object sender, RoutedEventArgs e)
        {
            new APropos().Show();
        }
    }
}
