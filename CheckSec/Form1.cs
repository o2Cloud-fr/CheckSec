using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CheckSec
{
    public partial class Form1 : Form
    {
        private Button btnCheckStatus;
        private Button btnUpdateSignatures;
        private Button btnQuickScan;
        private TextBox txtOutput;
        private ProgressBar progressBar;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
            InitializeTrayIcon();
        }

        private void InitializeUI()
        {
            this.Text = "Vérification de la Sécurité Système";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            btnCheckStatus = new Button();
            btnCheckStatus.Text = "Vérifier l'état de Windows Defender";
            btnCheckStatus.Size = new Size(250, 30);
            btnCheckStatus.Location = new Point(75, 30);
            btnCheckStatus.Click += BtnCheckStatus_Click;

            btnUpdateSignatures = new Button();
            btnUpdateSignatures.Text = "Mettre à jour les signatures";
            btnUpdateSignatures.Size = new Size(250, 30);
            btnUpdateSignatures.Location = new Point(75, 80);
            btnUpdateSignatures.Click += BtnUpdateSignatures_Click;

            btnQuickScan = new Button();
            btnQuickScan.Text = "Lancer une analyse rapide";
            btnQuickScan.Size = new Size(250, 30);
            btnQuickScan.Location = new Point(75, 130);
            btnQuickScan.Click += BtnQuickScan_Click;

            txtOutput = new TextBox();
            txtOutput.Multiline = true;
            txtOutput.ScrollBars = ScrollBars.Vertical;
            txtOutput.Size = new Size(450, 100);
            txtOutput.Location = new Point(25, 230);
            txtOutput.ReadOnly = true;

            progressBar = new ProgressBar();
            progressBar.Size = new Size(450, 20);
            progressBar.Location = new Point(25, 340);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;

            this.Controls.Add(btnCheckStatus);
            this.Controls.Add(btnUpdateSignatures);
            this.Controls.Add(btnQuickScan);
            this.Controls.Add(txtOutput);
            this.Controls.Add(progressBar);
        }

        private void InitializeTrayIcon()
        {
            // Créer le menu contextuel
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Ouvrir", null, OnOpen);
            trayMenu.Items.Add("Quitter", null, OnExit);

            // Créer l'icône dans la barre des tâches avec un logo personnalisé
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Vérification de la Sécurité Système";

            // Chargez l'icône personnalisée depuis un fichier
            // Utiliser le répertoire courant (où l'application est exécutée)
            string currentDirectory = Environment.CurrentDirectory;

            // Charger l'icône à partir du chemin complet (chemin du répertoire + le fichier icon.ico)
            string iconPath = Path.Combine(currentDirectory, "icon.ico");

            // Charger l'icône depuis le fichier
            trayIcon.Icon = new Icon(iconPath);


            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            // Masquer la fenêtre au démarrage
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;  // Ne pas afficher dans la barre des tâches
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;  // Ré-afficher dans la barre des tâches
        }

        private void OnOpen(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;  // Ré-afficher dans la barre des tâches
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false; // Masquer l'icône
            Application.Exit(); // Fermer l'application
        }

        private async void BtnCheckStatus_Click(object sender, EventArgs e)
        {
            btnCheckStatus.Enabled = false; // Désactive le bouton
            await Task.Run(() => GetWindowsDefenderStatus());
            btnCheckStatus.Enabled = true; // Réactive le bouton
        }

        private async void BtnUpdateSignatures_Click(object sender, EventArgs e)
        {
            btnUpdateSignatures.Enabled = false; // Désactive le bouton
            await Task.Run(() => UpdateWindowsDefenderSignatures());
            btnUpdateSignatures.Enabled = true; // Réactive le bouton
        }

        private async void BtnQuickScan_Click(object sender, EventArgs e)
        {
            btnQuickScan.Enabled = false; // Désactive le bouton
            await Task.Run(() => StartQuickScan());
            btnQuickScan.Enabled = true; // Réactive le bouton
        }

        private void GetWindowsDefenderStatus()
        {
            Invoke(new Action(() => txtOutput.AppendText("=== État de Windows Defender ===\r\n")));
            UpdateProgressBar(20);

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "Get-MpComputerStatus";
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Invoke(new Action(() =>
                        {
                            txtOutput.AppendText("Antivirus activé : " + GetPropertyValue(result, "AntivirusEnabled") + "\r\n");
                            txtOutput.AppendText("Version du moteur de l'antivirus : " + GetPropertyValue(result, "AMEngineVersion") + "\r\n");
                            txtOutput.AppendText("Version des signatures de virus : " + GetPropertyValue(result, "AntivirusSignatureVersion") + "\r\n");
                            txtOutput.AppendText("Dernière mise à jour des signatures antivirus : " + GetPropertyValue(result, "AntivirusSignatureLastUpdated") + "\r\n");
                            txtOutput.AppendText("Dernière mise à jour des signatures antispyware : " + GetPropertyValue(result, "AntispywareSignatureLastUpdated") + "\r\n");
                            txtOutput.AppendText("Début de la dernière analyse rapide : " + GetPropertyValue(result, "QuickScanStartTime") + "\r\n");
                            txtOutput.AppendText("Version des signatures de l'analyse rapide : " + GetPropertyValue(result, "QuickScanSignatureVersion") + "\r\n");
                            txtOutput.AppendText("Fin de la dernière analyse rapide : " + GetPropertyValue(result, "QuickScanEndTime") + "\r\n");
                            txtOutput.AppendText("Protection en temps réel : " + GetPropertyValue(result, "RealTimeProtectionEnabled") + "\r\n");
                        }));
                    }
                }

                UpdateProgressBar(100);
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => txtOutput.AppendText($"Erreur lors de la récupération de l'état de Windows Defender : {ex.Message}\r\n")));
            }

            Invoke(new Action(() => txtOutput.AppendText("================================\r\n")));
        }

        private string GetPropertyValue(string data, string propertyName)
        {
            string pattern = $"{propertyName}\\s+:\\s+([\\S]+)";
            var match = System.Text.RegularExpressions.Regex.Match(data, pattern);
            return match.Success ? match.Groups[1].Value : "Non disponible";
        }

        private void UpdateWindowsDefenderSignatures()
        {
            Invoke(new Action(() => txtOutput.AppendText("=== Mise à jour des signatures de Windows Defender ===\r\n")));
            UpdateProgressBar(0);

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "Update-MpSignature -ErrorAction Stop";
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                Process process = Process.Start(psi);
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Invoke(new Action(() => txtOutput.AppendText("Mise à jour des signatures de Windows Defender terminée avec succès.\r\n")));
                }
                else
                {
                    Invoke(new Action(() => txtOutput.AppendText("Échec de la mise à jour des signatures de Windows Defender.\r\n")));
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => txtOutput.AppendText($"Échec de la mise à jour des signatures de Windows Defender. Détails : {ex.Message}\r\n")));
            }

            UpdateProgressBar(100);
            Invoke(new Action(() => txtOutput.AppendText("================================\r\n")));
        }

        private void StartQuickScan()
        {
            Invoke(new Action(() => txtOutput.AppendText("=== Lancement d'une analyse rapide ===\r\n")));
            UpdateProgressBar(0);

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "Start-MpScan -ScanType QuickScan -ErrorAction Stop";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true; // Capturer les erreurs
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.Verb = "runas";  // Demander les privilèges administratifs

                Process process = Process.Start(psi);
                using (StreamReader reader = process.StandardError)
                {
                    string error = reader.ReadToEnd();
                    if (error.Contains("Une analyse est déjà en cours sur cet appareil"))
                    {
                        Invoke(new Action(() =>
                        {
                            txtOutput.AppendText("Une analyse est déjà en cours.\r\n");
                            btnQuickScan.Enabled = false; // Désactiver le bouton si une analyse est en cours
                        }));
                    }
                }

                process.WaitForExit();

                // Vérifier si l'analyse a été lancée avec succès
                if (process.ExitCode == 0)
                {
                    Invoke(new Action(() => txtOutput.AppendText("Analyse rapide terminée avec succès.\r\n")));
                }
                else
                {
                    Invoke(new Action(() => txtOutput.AppendText("Échec de l'analyse rapide.\r\n")));
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => txtOutput.AppendText($"Échec de l'analyse rapide. Détails : {ex.Message}\r\n")));
            }

            UpdateProgressBar(100);
            Invoke(new Action(() => txtOutput.AppendText("================================\r\n")));
        }

        private void UpdateProgressBar(int percent)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action<int>(UpdateProgressBar), percent);
            }
            else
            {
                progressBar.Value = percent;
            }
        }
    }
}
