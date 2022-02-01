using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace CIPReport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        SqlConnection connectionToRuntime = new SqlConnection(@"Data Source=LEBSERV1; Initial Catalog=Runtime; Integrated Security=True");
        MainTableRow[] mainTableRows;
       ObservableCollection<SelectableObject> selectableObjects;
        ObservableCollection<SelectableObject> selectableModules;

        public class SelectableObject
        {
            public bool IsSelected { get; set; }
            public string ObjectData { get; set; }

           

            public SelectableObject(string objectData)
            {
                ObjectData = objectData;
                IsSelected = false;
               
            }

            public SelectableObject(string objectData, bool isSelected)
            {
                IsSelected = isSelected;
                ObjectData = objectData;
                
            }

        }

 

        public class MainTableRow
        {
           
            public string Module { get; set; }

            public DateTime DT { get; set; }

            public string DT_string { get; set; }

            public DateTime Ending { get; set; }

            public string Ending_string { get; set; }

            public string Obj { get; set; }

            public string Prev_Delta { get; set; }

            public string Prog { get; set; }

            public string Finish { get; set; }

            public string cont{ get; set; }

            public string Oper { get; set; }

            public PhaseTableRow[] PhaseTable { get; set; }

            public bool filled; 

            public MainTableRow() 
            { 
                Module = ""; DT = new DateTime(); DT_string = "";  Ending = new DateTime(); Ending_string = ""; Obj = ""; Prev_Delta = ""; Prog = "";
                Finish = ""; cont = ""; Oper = "";  filled = false; 
              
              
            }
        }

        public class PhaseTableRow
        {
            public DateTime DT_start { get; set; }

            public string DT_start_string { get; set; }

            public DateTime DT_finish { get; set; }

            public string DT_finish_string { get; set; }

            public string Cont { get; set; }

            public string Phase { get; set; }

            public double TempAVG { get; set; }

            public double TempSP { get; set; }

            public double ConcAVG { get; set; }

            public double ConcSP { get; set; }

            public double FTAVG { get; set; }

            public double FTSP { get; set; }

            public DataTable StepTable { get; set; }

            public PhaseTableRow()
            {
                DT_start = new DateTime(); DT_start_string = "";
                DT_finish = new DateTime(); DT_finish_string = "";
                Cont = ""; Phase = ""; 
                TempAVG = new double(); TempSP = new double();
                ConcAVG = new double(); ConcSP = new double();
                FTAVG = new double(); FTSP = new double();
                StepTable = new DataTable();
            }
        }

        int findRow (string module, string start, string stop )
        {

         
            for (int i =0; i<mainTableRows.Length; i++)
            {
                string temp = mainTableRows[i].DT.ToString();
                if (mainTableRows[i].Module.Contains(module) && mainTableRows[i].DT_string.Contains(start) && mainTableRows[i].Ending_string.Contains(stop))
                {
                    return i;
                }
                else if (mainTableRows[mainTableRows.Length-1-i].Module.Contains(module) && mainTableRows[mainTableRows.Length - 1 - i].DT_string.Contains(start) && mainTableRows[mainTableRows.Length - 1 - i].Ending_string.Contains(stop))
                {
                    return mainTableRows.Length - 1 - i;
                }
                
            }
            return -1;
        }
     
        int findStepRow (DataTable dt)
        {
            for (int i = 0; i<dt.Rows.Count; i++)
            {
                string phase = (string)dt.Rows[i][3];
                if (phase.Contains("поласкивание") || phase.Contains("Циркуляция") || phase.Contains("Стерилизация") || phase.Contains("Дезинфекция"))
                {
                    return i;
                }
            }
            return -1;
        }

        

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {


            connectionToRuntime.Open();
            
            
        }

        void ShowHideDetails(object sender, RoutedEventArgs e)
        {
            StartWindow.Cursor = Cursors.Wait;
            Button button = (Button)sender;

            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility =
                    row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    button.Content =
                    row.DetailsVisibility == Visibility.Visible ? "-" : "+";
                   
                    break;
                }

            try
            {
                foreach (var SelItem in mainTable.SelectedItems)
                {

                    TextBlock module = (TextBlock)this.mainTable.Columns[4].GetCellContent(SelItem);
                    TextBlock start = (TextBlock)this.mainTable.Columns[1].GetCellContent(SelItem);
                    TextBlock stop = (TextBlock)this.mainTable.Columns[2].GetCellContent(SelItem);

                    int noteIndex = findRow(module.Text, start.Text, stop.Text);
                    if (noteIndex == -1)
                    {
                        throw new Exception("При поиске не было найдено значение. Сообщите разработчику.");
                    }
                    else if (!mainTableRows[noteIndex].filled)
                    {

                        string procPhaseTable = "PhaseTable";
                        string procStepTable = "StepTable";

                        DataTable dtPhase = new DataTable();


                        SqlCommand createCommand = new SqlCommand(procPhaseTable, connectionToRuntime);
                        createCommand.CommandType = CommandType.StoredProcedure;

                        createCommand.Parameters.Add(new SqlParameter { ParameterName = "@Module", Value = mainTableRows[noteIndex].Module });
                        createCommand.Parameters.Add(new SqlParameter { ParameterName = "@StartDate", Value = mainTableRows[noteIndex].DT.ToString("o") });
                        createCommand.Parameters.Add(new SqlParameter { ParameterName = "@StopDate", Value = mainTableRows[noteIndex].Ending.ToString("o") });
                        createCommand.ExecuteNonQuery();

                        SqlDataAdapter dataAdp = new SqlDataAdapter(createCommand);
                        dataAdp.Fill(dtPhase);

                        mainTableRows[noteIndex].PhaseTable = new PhaseTableRow[dtPhase.Rows.Count];

                        createCommand.CommandText = procStepTable;
                        createCommand.Parameters.Add(new SqlParameter { ParameterName = "@StopDateNext" });
                        createCommand.Parameters.Add(new SqlParameter { ParameterName = "@Phase" });

                        for (int i = 0; i < dtPhase.Rows.Count; i++)
                        {
                            mainTableRows[noteIndex].PhaseTable[i] = new PhaseTableRow();

                            mainTableRows[noteIndex].PhaseTable[i].DT_start = (DateTime)dtPhase.Rows[i][1];
                            mainTableRows[noteIndex].PhaseTable[i].DT_start_string = dtPhase.Rows[i][1].ToString();

                            mainTableRows[noteIndex].PhaseTable[i].DT_finish = (DateTime)dtPhase.Rows[i][2];
                            mainTableRows[noteIndex].PhaseTable[i].DT_finish_string = dtPhase.Rows[i][2].ToString();

                            mainTableRows[noteIndex].PhaseTable[i].Phase = dtPhase.Rows[i][3].ToString();

                            mainTableRows[noteIndex].PhaseTable[i].Cont = dtPhase.Rows[i][4].ToString();

                            DateTime dateTime = new DateTime();
                            if (i != dtPhase.Rows.Count - 1)
                            {
                                dateTime = (DateTime)dtPhase.Rows[i + 1][2];
                            }
                            else dateTime = mainTableRows[noteIndex].PhaseTable[i].DT_finish;

                            procStepTable = "EXEC StepTable @Module = '" + mainTableRows[noteIndex].Module +
                          "', @StartDate = '" + mainTableRows[noteIndex].PhaseTable[i].DT_start.ToString("o") +
                          "', @StopDate = '" + mainTableRows[noteIndex].PhaseTable[i].DT_finish.ToString("o") +
                          "', @StopDateNext = '"+ dateTime.ToString("o") +
                          "', @Phase = '"+ mainTableRows[noteIndex].PhaseTable[i].Phase+"'";

                            SqlCommand stepProcedure = new SqlCommand(procStepTable, connectionToRuntime);
                            


                            //stepProcedure.CommandType = CommandType.StoredProcedure;

                            //stepProcedure.Parameters.Add(new SqlParameter { ParameterName = "@Module", Value = mainTableRows[noteIndex].Module });
                            //stepProcedure.Parameters.Add(new SqlParameter { ParameterName = "@StartDate", Value = mainTableRows[noteIndex].PhaseTable[i].DT_start.ToString("o") });
                            //stepProcedure.Parameters.Add(new SqlParameter { ParameterName = "@StopDate", Value = mainTableRows[noteIndex].PhaseTable[i].DT_finish.ToString("o") });

                            //createCommand.Parameters[1].Value = mainTableRows[noteIndex].PhaseTable[i].DT_start.ToString("o");
                            //createCommand.Parameters[2].Value = mainTableRows[noteIndex].PhaseTable[i].DT_finish.ToString("o");
                            //createCommand.Parameters[4].Value = mainTableRows[noteIndex].PhaseTable[i].Phase;

                            //if (i != dtPhase.Rows.Count - 1)
                            //{
                            //    DateTime dateTime = (DateTime)dtPhase.Rows[i + 1][2];
                            //    stepProcedure.Parameters.Add(new SqlParameter { ParameterName = "@StopDateNext",  Value = dateTime.ToString("o") });
                            //    //createCommand.Parameters[3].Value = dateTime.ToString("o"); 
                            //}
                            //else stepProcedure.Parameters.Add(new SqlParameter { ParameterName = "@StopDateNext", Value = mainTableRows[noteIndex].PhaseTable[i].DT_finish.ToString("o") }); //createCommand.Parameters[3].Value = mainTableRows[noteIndex].PhaseTable[i].DT_finish.ToString("o");

                            //stepProcedure.Parameters.Add(new SqlParameter { ParameterName = "@Phase", Value = mainTableRows[noteIndex].PhaseTable[i].Phase });

                            stepProcedure.ExecuteNonQuery();

                           

                            dataAdp = new SqlDataAdapter(stepProcedure);
                            dataAdp.Fill(mainTableRows[noteIndex].PhaseTable[i].StepTable);

                            int stepInd = findStepRow(mainTableRows[noteIndex].PhaseTable[i].StepTable);

                            if (stepInd != -1)
                            {
                                mainTableRows[noteIndex].PhaseTable[i].ConcSP = (double)mainTableRows[noteIndex].PhaseTable[i].StepTable.Rows[stepInd][9];
                                mainTableRows[noteIndex].PhaseTable[i].ConcAVG = (double)mainTableRows[noteIndex].PhaseTable[i].StepTable.Rows[stepInd][8];

                                mainTableRows[noteIndex].PhaseTable[i].TempSP = (double)mainTableRows[noteIndex].PhaseTable[i].StepTable.Rows[stepInd][5];
                                mainTableRows[noteIndex].PhaseTable[i].TempAVG = (double)mainTableRows[noteIndex].PhaseTable[i].StepTable.Rows[stepInd][4];

                                mainTableRows[noteIndex].PhaseTable[i].FTSP = (double)mainTableRows[noteIndex].PhaseTable[i].StepTable.Rows[stepInd][13];
                                mainTableRows[noteIndex].PhaseTable[i].FTAVG = (double)mainTableRows[noteIndex].PhaseTable[i].StepTable.Rows[stepInd][12];
                            }

                        }
                        mainTableRows[noteIndex].filled = true;
                        
                    }
                    
                }
            }

            catch (Exception ex) { MessageBox.Show(ex.Message); }
            finally { StartWindow.Cursor = Cursors.Arrow; }

        }

        private void mainWindow_Closed(object sender, EventArgs e)
        {
            connectionToRuntime.Close();
        }



        private void ShowHideDetailsPhase(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility =
                    row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    button.Content =
                    row.DetailsVisibility == Visibility.Visible ? "-" : "+";

                    break;
                }
        }

        private void button_Find_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartWindow.Cursor = Cursors.Wait;
                if(!datePickerStart.SelectedDate.HasValue) throw new Exception("Введите дату начала поиска");
                if (!datePickerStop.SelectedDate.HasValue) throw new Exception("Введите дату конца поиска");
                Convert.ToInt32(texBox_Finder.Text);

                List<string> objNames = new List<string>();
                List<string> modNames = new List<string>();

                selectableObjects = new ObservableCollection<SelectableObject>();
                selectableModules = new ObservableCollection<SelectableObject>();

                string cmd = "SET DATEFORMAT DMY EXEC FullReport @StartDateF = '" + datePickerStart.SelectedDate.Value.ToString("G") +
                "', @EndDateF = '" + datePickerStop.SelectedDate.Value.ToString("G") + "', @AddDaysF = '" + texBox_Finder.Text + "'";
                SqlCommand createCommand = new SqlCommand(cmd, connectionToRuntime);


                SqlDataAdapter dataAdp = new SqlDataAdapter(createCommand);
                DataTable dt = new DataTable("CIPReport");
                dataAdp.Fill(dt);

                mainTableRows = new MainTableRow[dt.Rows.Count];

              

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    mainTableRows[i] = new MainTableRow();
                    mainTableRows[i].Module = new string(dt.Rows[i][0].ToString());

                    if (!modNames.Contains(mainTableRows[i].Module))
                    {
                        modNames.Add(mainTableRows[i].Module);
                    }

                    mainTableRows[i].DT = (DateTime)dt.Rows[i][1];
                    mainTableRows[i].DT_string = new string(dt.Rows[i][1].ToString());
                    mainTableRows[i].Ending = (DateTime)dt.Rows[i][2];
                    mainTableRows[i].Ending_string = new string(dt.Rows[i][2].ToString());
                    mainTableRows[i].Obj = new string(dt.Rows[i][3].ToString());

                   
                    if (!objNames.Contains(mainTableRows[i].Obj))
                    {
                        objNames.Add(mainTableRows[i].Obj);
                    }
                   
                    mainTableRows[i].Prev_Delta = new string(dt.Rows[i][4].ToString());
                    mainTableRows[i].Prog = new string(dt.Rows[i][5].ToString());
                    mainTableRows[i].Finish = new string(dt.Rows[i][6].ToString());
                    mainTableRows[i].cont = new string(dt.Rows[i][7].ToString());
                    mainTableRows[i].Oper = new string(dt.Rows[i][8].ToString());
                }

                selectableObjects.Add(new SelectableObject("Выделить все"));
                selectableModules.Add(new SelectableObject("Выделить все", true));

                objNames.Sort();
                modNames.Sort();
                foreach (string s in objNames)
                {
                    selectableObjects.Add(new SelectableObject(s));
                }
                foreach (string s in modNames)
                {
                    selectableModules.Add(new SelectableObject(s, true));
                }

                comboBox_Module.ItemsSource = selectableModules;
                comboBox_Objects.ItemsSource = selectableObjects;

                comboBox_Objects.SelectedIndex = 0;
                comboBox_Module.SelectedIndex = 0;

                

                mainTable.ItemsSource = mainTableRows;
                StartWindow.Cursor = Cursors.Arrow;
                comboBox_Module.Visibility = Visibility.Visible;
                comboBox_Objects.Visibility = Visibility.Visible;
                button_FilterModule.Visibility = Visibility.Visible;
                button_Filter.Visibility = Visibility.Visible;
            }
            catch (Exception ex) 
            {
                if (ex.Message.Contains("Input string")) MessageBox.Show("Введите целочисленое значение в поле поиск в глубину");
                else MessageBox.Show(ex.Message);
                StartWindow.Cursor = Cursors.Arrow;
            }
        }

        private void Check_Checked(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i<selectableObjects.Count; i++)
            {
                selectableObjects[i].IsSelected = true;
            }
        }

        private void texBox_Finder_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !(Char.IsDigit(e.Text, 0));
        }

        private void button_Filter_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<MainTableRow> filteredTable = new ObservableCollection<MainTableRow>();
            for (int i = 0; i<mainTableRows.Length; i++)
            {
                foreach (SelectableObject so in selectableObjects)
                {
                    if (so.IsSelected && mainTableRows[i].Obj.Contains(so.ObjectData))
                    {
                        filteredTable.Add(mainTableRows[i]);
                    }
                }
            }
            mainTable.ItemsSource = filteredTable;
        }

        private void comboBox_Objects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            comboBox.SelectedIndex = 0;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //CheckBox checkBox = (CheckBox)sender;
            //StackPanel stack =  (StackPanel) checkBox.Parent;
            //TextBlock textBlock = (TextBlock)stack.Children[1];
            //if (textBlock.Text.Contains("Выделить все"))
            //{
            //    for (int i=1; i<selectableObjects.Count;i++)
            //    {
            //        selectableObjects[i].IsSelected = true;
            //    }
            //}
            //comboBox_Objects.Items.Refresh();
            
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //CheckBox checkBox = (CheckBox)sender;
            //StackPanel stack = (StackPanel)checkBox.Parent;
            //TextBlock textBlock = (TextBlock)stack.Children[1];
            //if (textBlock.Text.Contains("Выделить все"))
            //{
            //    for (int i = 1; i < selectableObjects.Count; i++)
            //    {
            //        selectableObjects[i].IsSelected = false;
            //    }
            //}
            //comboBox_Objects.Items.Refresh();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            StackPanel stack = (StackPanel)checkBox.Parent;
            TextBlock textBlock = (TextBlock)stack.Children[1];
            if (textBlock.Text.Contains("Выделить все"))
            {
                if ((bool)checkBox.IsChecked)
                {
                    for (int i = 1; i < selectableObjects.Count; i++)
                    {
                        selectableObjects[i].IsSelected = true;
                    }
                }
                else if (!(bool)checkBox.IsChecked)
                {
                    for (int i = 1; i < selectableObjects.Count; i++)
                    {
                        selectableObjects[i].IsSelected = false;
                    }
                }
            }
            else if (selectableObjects[0].IsSelected) selectableObjects[0].IsSelected = false;
            comboBox_Objects.Items.Refresh();
        }

        private void CheckBoxModule_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            StackPanel stack = (StackPanel)checkBox.Parent;
            TextBlock textBlock = (TextBlock)stack.Children[1];
            if (textBlock.Text.Contains("Выделить все"))
            {
                if ((bool)checkBox.IsChecked)
                {
                    for (int i = 1; i < selectableModules.Count; i++)
                    {
                        selectableModules[i].IsSelected = true;
                    }
                }
                else if (!(bool)checkBox.IsChecked)
                {
                    for (int i = 1; i < selectableModules.Count; i++)
                    {
                        selectableModules[i].IsSelected = false;
                    }
                }
            }
            else if (selectableModules[0].IsSelected) selectableModules[0].IsSelected = false;
            comboBox_Module.Items.Refresh();
        }

        private void comboBox_Module_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            comboBox.SelectedIndex = 0;
        }

        private void button_FilterModule_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<MainTableRow> filteredTable = new ObservableCollection<MainTableRow>();
            selectableObjects.Clear();
            List<string> objNames = new List<string>();

            for (int i = 0; i < mainTableRows.Length; i++)
            {
                foreach (SelectableObject so in selectableModules)
                {
                    if (so.IsSelected && mainTableRows[i].Module.Contains(so.ObjectData))
                    {
                        filteredTable.Add(mainTableRows[i]);

                        if (!objNames.Contains(mainTableRows[i].Obj))
                        {
                            objNames.Add(mainTableRows[i].Obj);
                        }
                    }
                }
            }
            selectableObjects.Add(new SelectableObject("Выделить все"));

            objNames.Sort();

            foreach(string s in objNames)
            {
                selectableObjects.Add(new SelectableObject(s));
            }

            comboBox_Objects.Items.Refresh();
            mainTable.ItemsSource = filteredTable;
        }
    }
}
