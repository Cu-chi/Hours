﻿using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using IWshRuntimeLibrary;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

// Started : 22.12.20

namespace Hours
{
    public partial class HoursForm : Form
    {
        public HoursForm()
        {
            InitializeComponent();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public static string TotalSecondes;
        public static string TotalMinutes;
        public static string TotalHeures;
        public static string TotalJours;
        public static string FirstLauch;
        public static string NowDate;
        public static Counter x;
        public static bool StartingByUser = false;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // Permet de move en cliquant sur le panel gris du form
        private void TopPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private Timer timer1;

        // Timer qui execute la fonction Timer1_Tick à chaque timer1.Interval (1sec)
        public void InitTimer()
        {
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new EventHandler(Timer1_Tick);
            timer1.Interval = 1000;
            timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e) { MainHours(); }
        // Bouton pour minimiser
        private void MinimizeButton_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        // Bouton pour fermer Hours
        private void CloseButton_Click(object sender, EventArgs e)
        {
            DialogResult CloseMSgBox = MessageBox.Show("Souhaitez-vous vraiment fermer Hours ?\nLe temps ne sera alors plus compté.", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (CloseMSgBox == DialogResult.Yes) Close();
        }


        public class Counter
        {
            public long Time { get; set; }
            public string Date { get; set; }
            public string NowDate { get; set; }
            public bool Minimize { get; set; }
        }

        private void MainHours()
        {
            // Check si le processus Hours est déjà en cours d'éxecution,
            // pour chaque processus, si l'id du processus est différent du current
            // Fermer le processus qui est différent

            Process[] HoursProc = Process.GetProcessesByName("Hours");
            foreach(Process phours in HoursProc)
            {
                if(phours.Id != Process.GetCurrentProcess().Id)
                {
                    Close();
                }
                else
                {
                    StartingByUser = true;
                }
            }

            string ShortCutBoot = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string ShortCutPrograms = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

            if (!System.IO.File.Exists($@"{ShortCutBoot}\Hours.lnk"))
            {
                WshShellClass wsh = new WshShellClass();
                IWshShortcut shortcut = wsh.CreateShortcut($@"{ShortCutBoot}\Hours.lnk") as IWshShortcut;
                shortcut.TargetPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                shortcut.WindowStyle = 1;
                shortcut.WorkingDirectory = ShortCutBoot;
                shortcut.Save();
            }
            if (!System.IO.File.Exists($@"{ShortCutPrograms}\Hours.lnk"))
            {
                WshShellClass wsh = new WshShellClass();
                IWshShortcut shortcut = wsh.CreateShortcut($@"{ShortCutPrograms}\Hours.lnk") as IWshShortcut;
                shortcut.TargetPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                shortcut.WindowStyle = 1;
                shortcut.WorkingDirectory = ShortCutPrograms;
                shortcut.Save();
            }

            DateTime now = DateTime.Now;
            string DateNow = now.ToLocalTime().ToString();
            string DataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Hours\data.json";
            string HoursPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Hours";

            if (!System.IO.File.Exists(DataPath))
            {
                timer1.Stop();
                MessageBox.Show("Première ouverture d'Hours ?\nA chaque démarrage, il s'allumera tout seul", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Counter time = new Counter
                {
                    Time = 1,
                    Date = DateNow,
                    NowDate = DateNow,
                    Minimize = false,
                };

                Directory.CreateDirectory($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Hours");
                System.IO.File.WriteAllText(DataPath, JsonConvert.SerializeObject(time));

                using (StreamWriter file = System.IO.File.CreateText(DataPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, time);
                }
                timer1.Start();
                return;
            }

            string json = System.IO.File.ReadAllText($@"{DataPath}");
            Counter x;

            try
            {
                x = JsonConvert.DeserializeObject<Counter>(json);
                var obj = JToken.Parse(json);
            }
            catch
            {
                timer1.Stop();
                LabelTotalTime.Text = "Les données sont erronées...";
                DialogResult CantJson = MessageBox.Show($"La fichier de data est érroné, souhaitez-vous appliquer une backup ?", "Erreur :(", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (CantJson == DialogResult.Yes)
                {
                    try
                    {
                        string jsonbu = System.IO.File.ReadAllText($@"{HoursPath}\backups\data-backup.json");
                        var obj = JToken.Parse(jsonbu);
                        System.IO.File.Delete($@"{HoursPath}\data.json");
                        System.IO.File.Move($@"{HoursPath}\backups\data-backup.json", $@"{HoursPath}\data.json");
                        MessageBox.Show($"Une backup à été appliqué ! (data-backup)", "Réglé !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch
                    {
                        try
                        {
                            string jsonbu_old = System.IO.File.ReadAllText($@"{HoursPath}\backups\data-backup-old.json");
                            var obj = JToken.Parse(jsonbu_old);
                            System.IO.File.Delete($@"{HoursPath}\data.json");
                            System.IO.File.Move($@"{HoursPath}\backups\data-backup-old.json", $@"{HoursPath}\data.json");
                            MessageBox.Show($"Une backup à été appliqué ! (data-backup-old)", "Réglé !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch
                        {
                            MessageBox.Show($"Aucune backup correcte n'a été trouvé, une nouvelle session sera donc débuté (vous reprendrez donc à 0sec).\nN'hésitez pas à me contacter.", "Attention !", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            System.IO.File.Delete($@"{HoursPath}\data.json");
                        }
                    }
                    timer1.Start();
                }
                else
                {
                    MessageBox.Show($"Hours va donc se fermer", "Fermeture due au fichié érroné", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                }
                return;
            }

            if (x.Minimize == true)
                ButtonStartMinimized.Checked = true;
                if(StartingByUser == false)
                    Hide();

            if (x.Time <= 9223372036854775806)
            {
                Counter timeAdder = new Counter
                {
                    Time = x.Time + 1,
                    Date = x.Date,
                    NowDate = DateNow,
                    Minimize = x.Minimize,
                };

                System.IO.File.WriteAllText(DataPath, JsonConvert.SerializeObject(timeAdder));
                using (StreamWriter file = System.IO.File.CreateText(DataPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, timeAdder);
                }

                BackUpFile();
            }
            else
            {
                LabelTotalTime.Text = $"Vous n'êtes pas humain, vous avez\nmodifié votre temps...";
                return;
            }

            double minutes = (double)x.Time / 60;
            double hours = minutes / 60;
            double days = hours / 24;

            if(checkBox1.Checked == false)
            {
                LabelSeconds.Text = $"{x.Time} secondes";
                LabelMinutes.Text = $"{Math.Round((double)minutes, 2)} minutes";
                LabelHours.Text = $"{Math.Round((double)hours, 3)} heures";
                LabelDays.Text = $"{Math.Round((double)days, 3)} jours";
                LabelTotalTime.Text = $"Vous avez passé {Math.Round((double)hours, 5)} heures \navec votre pc d'allumé.";
                LabelFirstOpeningDate.Text = $"{x.Date}";
                LabelDateNow.Text = $"{x.NowDate}";
            }
            else
            {
                LabelSeconds.Text = $"{x.Time} secondes";
                LabelMinutes.Text = $"{Math.Round((double)minutes, 0)} minutes";
                LabelHours.Text = $"{Math.Round((double)hours, 0)} heures";
                LabelDays.Text = $"{Math.Round((double)days, 0)} jours";
                LabelTotalTime.Text = $"Vous avez passé {Math.Round((double)hours, 0)} heures \navec votre pc d'allumé.";
                LabelFirstOpeningDate.Text = $"{x.Date}";
                LabelDateNow.Text = $"{x.NowDate}";
            }
            TotalSecondes = x.Time.ToString();
            TotalMinutes = Math.Round((double)minutes, 0).ToString();
            TotalHeures = Math.Round((double)hours, 0).ToString();
            TotalJours = Math.Round((double)days, 0).ToString();
            FirstLauch = x.Date;
            NowDate = x.NowDate;
        }

        private void HoursForm_Load(object sender, EventArgs e)
        {
            InitTimer();
        }

        private void HideButton_Click(object sender, EventArgs e)
        {
            DialogResult CloseMSgBox = MessageBox.Show("Souhaitez-vous vraiment mettre en arrière plan Hours ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (CloseMSgBox == DialogResult.Yes) Hide();
        }

        private void ButtonShare_Click(object sender, EventArgs e)
        {
            Clipboard.SetText($"⏲ === Hours Score === ⏲\nDepuis que j'ai lancé Hours le {FirstLauch} et la date à laquelle j'ai enregistré mon score le {NowDate}, j'ai passé ~{TotalHeures} heures sur mon pc soit :\n     {TotalSecondes} secondes\n     ~{TotalMinutes} minutes\n     ~{TotalJours} jours\nTélécharge toi aussi Hours ici : https://github.com/Cu-chi !");
        }

        private void ButtonReset_Click(object sender, EventArgs e)
        {
            DialogResult ResetMsgBox = MessageBox.Show($"Vous êtes sur le point de réinitialiser le temps enregistré.\nConfirmez-vous ?\n\nA noter qu'une sauvegarde sera effectuée, vous pourrez la trouver ici :\n{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Hours\\save\\", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ResetMsgBox == DialogResult.Yes)
            {
                DateTime now = DateTime.Now;
                string DataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Hours";
                string DateNow = now.ToLocalTime().ToString();
                if(!Directory.Exists(DataPath + @"\save")) Directory.CreateDirectory(DataPath + @"\save");
                if (!System.IO.File.Exists(DataPath + $@"\save\data-save-{DateNow.Replace(":", ".")}.json")) System.IO.File.Copy(DataPath + @"\data.json", DataPath + $@"\save\data-save-{DateNow.Replace(":", ".")}.json");
                else
                {
                    bool AlreadyExist = true;
                    sbyte i = 0;
                    string filename = $"data-save-{DateNow.Replace(":", ".")}";
                    string filepath = "";
                    while (AlreadyExist)
                    {
                        i += 1;
                        filepath = $@"{DataPath}\save\{filename}-{i}.json";
                        if (!System.IO.File.Exists(filepath))
                        {
                            AlreadyExist = false;
                        }
                    }
                    System.IO.File.Copy(DataPath + @"\data.json", filepath);
                }
                System.IO.File.Delete(DataPath + @"\data.json");
            }
        }

        private void ButtonHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Lors du première démarage, un raccourci à été créé afin qu'Hours soit " +
                "démarré à chaque lancement de votre ordinateur." +
                "\nUn autre raccourci à été créé afin que vous puissiez trouver Hours en cherchant dans " +
                "la barre de recherche Windows." +
                "\n\nLorsque vous démarré votre ordinateur, Hours se lance donc tout seul est se met " +
                "en tâche de fond. Pour réafficher l'interface, il vous suffira de chercher Hours, et " +
                "de l'ouvrir, cela stoppera l'autre Hours en tâche de fond et vous affichera par conséquent" +
                "l'interface avec toutes vos données.", "Aide", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BackUpFile()
        {
            string DataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Hours";
            if (!Directory.Exists($@"{DataPath}\backups")) Directory.CreateDirectory($@"{DataPath}\backups");

            if (System.IO.File.Exists($@"{DataPath}\backups\data-backup-old.json"))
                System.IO.File.Delete($@"{DataPath}\backups\data-backup-old.json");

            if (System.IO.File.Exists($@"{DataPath}\backups\data-backup.json"))
                System.IO.File.Move($@"{DataPath}\backups\data-backup.json", $@"{DataPath}\backups\data-backup-old.json");

            System.IO.File.Copy($@"{DataPath}\data.json", $@"{DataPath}\backups\data-backup.json");
        }

        private void ButtonStartMinimized_CheckedChanged(object sender, EventArgs e)
        {
            Counter timeAdder;
            string DataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Hours\data.json";
            string json = System.IO.File.ReadAllText(DataPath);
            Counter x = JsonConvert.DeserializeObject<Counter>(json);
            if (ButtonStartMinimized.Checked == true)
            {
                timeAdder = new Counter
                {
                    Time = x.Time,
                    Date = x.Date,
                    NowDate = x.NowDate,
                    Minimize = true,
                };
            }
            else
            {
                timeAdder = new Counter
                {
                    Time = x.Time,
                    Date = x.Date,
                    NowDate = x.NowDate,
                    Minimize = false,
                };
            }

            System.IO.File.WriteAllText(DataPath, JsonConvert.SerializeObject(timeAdder));
            using (StreamWriter file = System.IO.File.CreateText(DataPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, timeAdder);
            }
        }
    }
}