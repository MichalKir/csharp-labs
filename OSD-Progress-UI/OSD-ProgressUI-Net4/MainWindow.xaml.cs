using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using TSEnvironmentLib;
using System.Management;
using System.Net;
using System.Diagnostics;

namespace OSD_ProgressUI_Net4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();            
        }

        public void PopulateWmiLabels()
        {
            // Initiate WMI and define queries
            string ComputerQuery = "SELECT * FROM Win32_ComputerSystem";
            string BiosQuery = "SELECT * FROM Win32_Bios";
            string NetworkAdapterQuery = "SELECT * FROM Win32_NetworkAdapter"
                + " WHERE NetConnectionID = 'Ethernet' AND NetConnectionStatus = '2'"
                + " OR NetConnectionId = 'Anslutning till lokalt nätverk' AND NetConnectionStatus = '2'";
            // Read WMI
            ManagementObjectSearcher ComputerInfo = new ManagementObjectSearcher(ComputerQuery);
            ManagementObjectSearcher BiosInfo = new ManagementObjectSearcher(BiosQuery);
            ManagementObjectSearcher NetworkAdapterInfo = new ManagementObjectSearcher(NetworkAdapterQuery);
            // From ComputerSystem query
            foreach (ManagementObject oneValue in ComputerInfo.Get())
            {
                Label_Device_ComputerModel.Content = oneValue.Properties["Model"].Value.ToString();
            }
            // From BIOS Query
            foreach (ManagementObject oneValue in BiosInfo.Get())
            {
                Label_Device_SerialNumber.Content = oneValue.Properties["SerialNumber"].Value.ToString();
            }
            // From Network adapter Query
            foreach (ManagementObject oneValue in NetworkAdapterInfo.Get())
            {
                Label_Device_Mac.Content = oneValue.Properties["MACAddress"].Value.ToString();
                Label_Device_NicName.Content = oneValue.Properties["Name"].Value.ToString();
            }
        }

        public void PopulateIpLabel()
        {
            // Ip Address 
            string hostName = Dns.GetHostName();
            IPAddress[] ipaddress = Dns.GetHostAddresses(hostName);
            string ipAddresses = string.Empty;
            List<string> ipResults = new List<string>();
            foreach (IPAddress ip4 in ipaddress.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                // Get real IP-only, ignore Hyper-V default nic IP
                if (ip4.ToString().StartsWith("10.") == true)
                {
                    ipResults.Add(ip4.ToString());
                }
            }

            Label_Device_IP.Content = string.Join(", ", ipResults);
        }
        public void PopulateTsEnvLabels()
        {             
                // Read TS-env
                ITSEnvClass tsEnvVar = new TSEnvClass();
                // Top labels
                Label_TS_Name.Text = tsEnvVar["_SMSTSPackageName"].ToString();
                Label_TS_Role.Text = tsEnvVar["OrgName"].ToString();
                Label_TS_Office.Text = tsEnvVar["ITOfficeVersion"].ToString();
                Label_TS_Windows.Text = tsEnvVar["ITWindowsName"].ToString();
                // Computer info label
                Label_Device_ComputerName.Content = tsEnvVar["OSDComputerName"].ToString();


        }

        // Initiate BackgroundWorker on Windows_Loaded event
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate labels
            PopulateTsEnvLabels();
            PopulateWmiLabels();
            PopulateIpLabel();          
            
            pbCalculationProgress.Value = 0;
    
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync(10000);
        }

        // Execute Background Worker
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int max = (int)e.Argument;
            int result = 0;

            // Connect to TSEnvironment
            ITSEnvClass tsEnvVar = new TSEnvClass();

            // Define variables
            int totalStepNumbers = 0;
            int currentStepNumber = 0;
            int progressPercentage = 0;
            int currentStage = 0;
            bool lastActionSuccess = true;
            string currentStepName = null;
            
            // Update the progress and step name bar until TS is done
            do
            {   
                // Calculate progress %
                totalStepNumbers = Int32.Parse(tsEnvVar["_SMSTSInstructionTableSize"].ToString());
                currentStepNumber = Int32.Parse(tsEnvVar["_SMSTSNextInstructionPointer"].ToString());
                progressPercentage = Convert.ToInt32(((double)currentStepNumber / totalStepNumbers) * 100);
                // Set current step name
                currentStepName = tsEnvVar["_SMSTSCurrentActionName"].ToString();

                // Report progress
                (sender as BackgroundWorker).ReportProgress(progressPercentage);
                // Sleep thread
                System.Threading.Thread.Sleep(1);
                result = progressPercentage;
                // Update Current Step Name Label
                Label_TS_CurrentStep.BeginInvoke(new Action(() => { Label_TS_CurrentStep.Content = currentStepName; }));

                // Update stage
                lastActionSuccess = Convert.ToBoolean((string)tsEnvVar["_SMSTSLastActionSucceeded"].ToString());
                currentStage = Convert.ToInt32((string)tsEnvVar["ITCurrentStage"].ToString());              
                Grid_AKA_Stages.Dispatcher.BeginInvoke(new Action(() => { // THIS SHOULD BE FUNCTION
                    // Stage 1
                    if (currentStage == 1)
                    {
                        if (lastActionSuccess == false)
                        {
                            label_Progress_1.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c0392b"));
                            label_Progress_1.FontSize = 16;
                        }
                    }
                    // Stage 2
                    if (currentStage == 2)
                    {
                        label_Progress_1.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_1.FontSize = 12;
                        label_Progress_2.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f39c12"));
                        label_Progress_2.FontSize = 16;

                        if (lastActionSuccess == false)
                        {
                            label_Progress_2.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c0392b"));
                            label_Progress_2.FontSize = 16;
                        }
                    }
                    // Stage 3
                    if (currentStage == 3)
                    {
                        label_Progress_1.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_1.FontSize = 12;
                        label_Progress_2.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_2.FontSize = 12;
                        label_Progress_3.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f39c12"));
                        label_Progress_3.FontSize = 16;

                        if (lastActionSuccess == false)
                        {
                            label_Progress_3.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c0392b"));
                            label_Progress_3.FontSize = 16;
                        }
                    }
                    // Stage 4
                    if (currentStage == 4)
                    {
                        label_Progress_1.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_1.FontSize = 12;
                        label_Progress_2.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_2.FontSize = 12;
                        label_Progress_3.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_3.FontSize = 12;
                        label_Progress_4.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f39c12"));
                        label_Progress_4.FontSize = 16;

                        if (lastActionSuccess == false)
                        {
                            label_Progress_4.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c0392b"));
                            label_Progress_4.FontSize = 16;
                        }
                    }
                    // Stage 5
                    if (currentStage == 5)
                    {
                        label_Progress_1.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_1.FontSize = 12;
                        label_Progress_2.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_2.FontSize = 12;
                        label_Progress_3.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_3.FontSize = 12;
                        label_Progress_4.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_4.FontSize = 12;
                        label_Progress_5.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f39c12"));
                        label_Progress_5.FontSize = 16;

                        if (lastActionSuccess == false)
                        {
                            label_Progress_5.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c0392b"));
                            label_Progress_5.FontSize = 16;
                        }
                    }
                    // Stage 6
                    if (currentStage == 6)
                    {
                        label_Progress_1.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_1.FontSize = 12;
                        label_Progress_2.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_2.FontSize = 12;
                        label_Progress_3.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_3.FontSize = 12;
                        label_Progress_4.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_4.FontSize = 12;
                        label_Progress_5.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_5.FontSize = 12;
                        label_Progress_6.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f39c12"));
                        label_Progress_6.FontSize = 16;

                        if (lastActionSuccess == false)
                        {
                            label_Progress_6.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c0392b"));
                            label_Progress_6.FontSize = 16;
                        }
                    }
                    // Stage 7
                    if (currentStage == 7)
                    {
                        label_Progress_1.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_1.FontSize = 12;
                        label_Progress_2.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_2.FontSize = 12;
                        label_Progress_3.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_3.FontSize = 12;
                        label_Progress_4.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_4.FontSize = 12;
                        label_Progress_5.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_5.FontSize = 12;
                        label_Progress_6.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#27ae60"));
                        label_Progress_6.FontSize = 12;
                        label_Progress_7.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f39c12"));
                        label_Progress_7.FontSize = 16;

                        if (lastActionSuccess == false)
                        {
                            label_Progress_7.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c0392b"));
                            label_Progress_7.FontSize = 16;
                        }
                    }                                  
                }));

            } while (progressPercentage <= 100);
            e.Result = result;
          
            
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {            
            Label_ProgressUI_Percent.Content = e.ProgressPercentage.ToString() + " %";
            pbCalculationProgress.Value = e.ProgressPercentage;
        }

        private void Button_Password_Click(object sender, RoutedEventArgs e)
        {
            Flyout_Password.IsOpen = true;
        }

        private void Button_Flyout_Password_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox_Tools.Password == "PasswordLab")
            {
                Button_PowerShell.Visibility = Visibility.Visible;
                Button_PowerShell.IsEnabled = true;
                Button_CMTrace.Visibility = Visibility.Visible;
                Button_CMTrace.IsEnabled = true;
                Seperator_Tools.Visibility = Visibility.Visible;
                Flyout_Password.IsOpen = false;
            }
        }

        private void Button_PowerShell_Click(object sender, RoutedEventArgs e)
        {
            Process process = new Process();
            process.StartInfo.FileName = "Powershell.exe";
            process.Start();
            ;
        }

        private void Button_CMTrace_Click(object sender, RoutedEventArgs e)
        {
            Process process = new Process();
            process.StartInfo.FileName = "CMTrace.exe";
            process.Start();
;        }
    }
}
