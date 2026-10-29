using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace KeyManagementSystem
{
    public partial class Form1 : Form
    {
        private KeyManager manager = new KeyManager();

        private TextBox txtIssueBadge, txtIssueRoom, txtIssueNote;
        private TextBox txtReturnRoom, txtReturnNote;
        private DataGridView dgvKeys, dgvJournal;

        public Form1()
        {
            InitializeComponent();
            SetupCustomUI();
            RefreshData();
        }

        private void SetupCustomUI()
        {
            this.Text = "АИС учета ключей на вахте (КПП / Учебный корпус)";
            this.Size = new Size(860, 520);
            this.StartPosition = FormStartPosition.CenterScreen;

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };

            TabPage tabIssue = new TabPage("Выдача ключа");
            Label lbl1 = new Label { Text = "Номер пропуска:", Location = new Point(20, 25), AutoSize = true };
            txtIssueBadge = new TextBox { Location = new Point(200, 22), Width = 220 };

            Label lbl2 = new Label { Text = "Номер аудитории:", Location = new Point(20, 65), AutoSize = true };
            txtIssueRoom = new TextBox { Location = new Point(200, 62), Width = 220 };

            Label lbl3 = new Label { Text = "Примечание:", Location = new Point(20, 105), AutoSize = true };
            txtIssueNote = new TextBox { Location = new Point(200, 102), Width = 220, Text = "Без замечаний" };

            Button btnIssue = new Button
            {
                Text = "Проверить доступ и Выдать ключ",
                Location = new Point(20, 150),
                Size = new Size(400, 40),
                BackColor = Color.LightGreen
            };
            btnIssue.Click += BtnIssue_Click;

            Label lblKeysTitle = new Label { Text = "Состояние ключницы:", Location = new Point(450, 20), AutoSize = true };
            dgvKeys = new DataGridView
            {
                Location = new Point(450, 45),
                Size = new Size(360, 380),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            tabIssue.Controls.AddRange(new Control[] { lbl1, txtIssueBadge, lbl2, txtIssueRoom, lbl3, txtIssueNote, btnIssue, lblKeysTitle, dgvKeys });

            TabPage tabReturn = new TabPage("Прием ключа");
            Label lblRetRoom = new Label { Text = "Номер сдаваемой аудитории:", Location = new Point(20, 30), AutoSize = true };
            txtReturnRoom = new TextBox { Location = new Point(250, 27), Width = 180 };

            Label lblRetNote = new Label { Text = "Фиксация замечания:", Location = new Point(20, 75), AutoSize = true };
            txtReturnNote = new TextBox { Location = new Point(250, 72), Width = 250, Text = "Сдан в порядке" };

            Button btnReturn = new Button
            {
                Text = "Принять ключ в ключницу",
                Location = new Point(20, 125),
                Size = new Size(410, 40),
                BackColor = Color.LightSkyBlue
            };
            btnReturn.Click += BtnReturn_Click;

            tabReturn.Controls.AddRange(new Control[] { lblRetRoom, txtReturnRoom, lblRetNote, txtReturnNote, btnReturn });

            TabPage tabJournal = new TabPage("Журнал учета");
            Button btnRefresh = new Button { Text = "Обновить журнал", Dock = DockStyle.Top, Height = 35 };
            btnRefresh.Click += (s, e) => RefreshData();

            dgvJournal = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            tabJournal.Controls.Add(dgvJournal);
            tabJournal.Controls.Add(btnRefresh);

            tabControl.TabPages.AddRange(new TabPage[] { tabIssue, tabReturn, tabJournal });
            this.Controls.Add(tabControl);
        }

        private void BtnIssue_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtIssueBadge.Text.Trim(), out int badgeID))
            {
                MessageBox.Show("Введите числовой номер пропуска (например, 1001)!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string room = txtIssueRoom.Text.Trim();
            if (string.IsNullOrEmpty(room))
            {
                MessageBox.Show("Введите номер аудитории!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (manager.verifyAccess(badgeID, room, out string msg))
                {
                    manager.issueKey(badgeID, room, txtIssueNote.Text.Trim());
                    MessageBox.Show("Успешно! " + msg + "\nКлюч от аудитории " + room + " выдан.", "Выдача ключа", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshData();
                }
                else
                {
                    MessageBox.Show(msg, "Отказ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReturn_Click(object sender, EventArgs e)
        {
            string room = txtReturnRoom.Text.Trim();
            if (string.IsNullOrEmpty(room))
            {
                MessageBox.Show("Укажите номер аудитории!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                manager.returnKey(room, txtReturnNote.Text.Trim());
                MessageBox.Show("Ключ от аудитории " + room + " принят и возвращен в ключницу!", "Прием ключа", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshData()
        {
            try
            {
                dgvKeys.DataSource = manager.getKeysStatus();
                dgvJournal.DataSource = manager.getLogReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить базу KeySystem.accdb:\n" + ex.Message);
            }
        }
    }

    public class Key
    {
        public int keyNumber { get; set; }
        public string roomNumber { get; set; }
        public string status { get; set; }
        public void setStatus(string newStatus) { this.status = newStatus; }
    }

    public class Employee
    {
        public int badgeID { get; set; }
        public string fullName { get; set; }
        public string department { get; set; }
    }

    public class KeyLog
    {
        public int logID { get; set; }
        public DateTime issueTime { get; set; }
        public string returnTime { get; set; }
        public string note { get; set; }
    }

    public class KeyManager
    {
        private string connString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\ivank\OneDrive\Desktop\МДК Корепанов\KeySystem.accdb;Persist Security Info=False;";

        public bool verifyAccess(int badgeID, string roomNumber, out string message)
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();

                string empQuery = $"SELECT COUNT(*) FROM Employees WHERE CStr(badgeID) = '{badgeID}'";
                using (OleDbCommand cmd = new OleDbCommand(empQuery, conn))
                {
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        message = "Отказ СКУД: Сотрудник с таким пропуском не найден!";
                        return false;
                    }
                }

                string keyQuery = $"SELECT status FROM Keys WHERE CStr(roomNumber) = '{roomNumber}'";
                using (OleDbCommand cmd = new OleDbCommand(keyQuery, conn))
                {
                    object result = cmd.ExecuteScalar();

                    if (result == null)
                    {
                        message = "Ошибка: Аудитория не найдена в базе!";
                        return false;
                    }
                    if (!result.ToString().Trim().Equals("свободен", StringComparison.OrdinalIgnoreCase))
                    {
                        message = "Ключ от этой аудитории уже выдан!";
                        return false;
                    }
                }

                message = "Доступ разрешен. Ключ свободен.";
                return true;
            }
        }

        public void issueKey(int badgeID, string roomNumber, string note)
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();

                string updateKey = $"UPDATE Keys SET status = 'Выдан' WHERE CStr(roomNumber) = '{roomNumber}'";
                using (OleDbCommand cmd = new OleDbCommand(updateKey, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string cleanNote = string.IsNullOrWhiteSpace(note) ? "Без замечаний" : note.Replace("'", "");
                try
                {
                    string insertLog = $"INSERT INTO KeyLogs (badgeID, roomNumber, issueTime, [note]) VALUES ({badgeID}, '{roomNumber}', Now(), '{cleanNote}')";
                    using (OleDbCommand cmd = new OleDbCommand(insertLog, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    string insertLogAlt = $"INSERT INTO KeyLogs (badgeID, roomNumber, issueTime, [note]) VALUES ('{badgeID}', {roomNumber}, Now(), '{cleanNote}')";
                    using (OleDbCommand cmd = new OleDbCommand(insertLogAlt, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void returnKey(string roomNumber, string note)
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();

                string updateKey = $"UPDATE Keys SET status = 'свободен' WHERE CStr(roomNumber) = '{roomNumber}'";
                using (OleDbCommand cmd = new OleDbCommand(updateKey, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string cleanNote = string.IsNullOrWhiteSpace(note) ? "Сдан без замечаний" : note.Replace("'", "");
                string updateLog = $"UPDATE KeyLogs SET returnTime = Now(), [note] = '{cleanNote}' WHERE CStr(roomNumber) = '{roomNumber}' AND returnTime IS NULL";
                using (OleDbCommand cmd = new OleDbCommand(updateLog, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable getLogReport()
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();
                string query = @"SELECT KeyLogs.logID AS [ID], 
                                        KeyLogs.badgeID AS [Пропуск], 
                                        Employees.fullName AS [ФИО Сотрудника], 
                                        KeyLogs.roomNumber AS [Аудитория], 
                                        KeyLogs.issueTime AS [Время выдачи], 
                                        KeyLogs.returnTime AS [Время возврата], 
                                        KeyLogs.note AS [Замечание]
                                 FROM KeyLogs LEFT JOIN Employees ON CStr(KeyLogs.badgeID) = CStr(Employees.badgeID)
                                 ORDER BY KeyLogs.logID DESC";
                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        public DataTable getKeysStatus()
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();
                string query = "SELECT keyNumber AS [№ Ключа], roomNumber AS [Аудитория], status AS [Статус] FROM Keys";
                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }
    }
}