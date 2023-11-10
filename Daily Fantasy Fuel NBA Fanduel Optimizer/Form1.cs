using System.Net.Http;
using HtmlAgilityPack; // This specifies that you're using HtmlAgilityPack
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks; // Make sure you have this for async Task
using System; // For the Exception class
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Daily_Fantasy_Fuel_NBA_Fanduel_Optimizer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // If you want to load the data when the form loads, call the method here.
            await DisplayDataInGridView();
        }
        public async Task<DataTable> GetTableFromWebAsync(string url)
        {
            // Initialize the HttpClient
            using (HttpClient http = new HttpClient())
            {
                // Make sure the URL is absolute
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    throw new ArgumentException("The URL must be an absolute URI.", nameof(url));
                }

                // Get the html content from the website
                var response = await http.GetAsync(url);
                var pageContents = await response.Content.ReadAsStringAsync();

                // Load the HTML into the HtmlDocument (specify HtmlAgilityPack.HtmlDocument)
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(pageContents);

                // Initialize the DataTable
                DataTable dt = new DataTable("NBA");

                // Locate the table you want by an identifier if possible, otherwise assume first table
                var table = doc.DocumentNode.SelectSingleNode("//table");

                // Check if the table is not null
                if (table != null)
                {
                    // Process table headers
                    var headers = table.SelectNodes("thead/tr/th").Skip(5).Select(th => th.InnerText.Trim());
                    foreach (var header in headers)
                    {
                        dt.Columns.Add(header); // Create columns according to headers, skipping the first 5
                    }

                    // Process rows, skip the first row
                    var rows = table.SelectNodes("tbody/tr").Skip(1);
                    foreach (var row in rows)
                    {
                        // Select cells from each row, skipping the first cell
                        var cells = row.SelectNodes("td").Skip(1).Select(td => td.InnerText.Trim()).ToArray();
                        dt.Rows.Add(cells); // Add row data to DataTable
                    }
                }

                return dt;
            }


        }
        private async Task DisplayDataInGridView()
        {
            string url = "https://www.dailyfantasyfuel.com/nba/projections/fanduel";
            try
            {
                DataTable table = await GetTableFromWebAsync(url);
                this.Invoke(new Action(() =>
                {
                    dataGridView1.DataSource = table; // This must be done on the UI thread
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }
        public class Player
        {
            public List<string> Positions { get; set; } // List to hold multiple positions
            public string POS { get; set; }
            public string Name { get; set; }
            public double Salary { get; set; }
            public int Rest { get; set; }
            public string Start { get; set; }
            public string Team { get; set; }
            public string Opp { get; set; }
            public int DvP { get; set; }
            public double FDProjectedPoints { get; set; }
            public double ValueProjected { get; set; }
            public double FPMin { get; set; }
            public double FPAvg { get; set; }
            public double FPMax { get; set; }
            public double OverUnder { get; set; }
            public double TeamPoints { get; set; }
        }



        // Assuming this is somewhere in your form code
        private void DisplayFinalTeam(List<Player> finalTeam)
        {
            // Clear existing items if you have a control to display the team
            listViewFinalTeam.Items.Clear();

            // Ensure the ListView has the correct columns
            listViewFinalTeam.Columns.Clear();
            listViewFinalTeam.Columns.Add("Position", -2, HorizontalAlignment.Left);
            listViewFinalTeam.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listViewFinalTeam.Columns.Add("Salary", -2, HorizontalAlignment.Right);
            listViewFinalTeam.Columns.Add("Fantasy Points", -2, HorizontalAlignment.Right);


            // Variables to keep track of totals
            double totalFantasyPoints = 0;
            int totalSalary = 0;

            foreach (var player in finalTeam)
            {
                ListViewItem item = new ListViewItem(string.Join("/", player.Positions));// Position as the first column
                item.SubItems.Add(player.Name); // Name as the second column
                item.SubItems.Add($"${player.Salary * 1000:N0}");  // Salary as the third column, formatted as currency
                item.SubItems.Add(player.FDProjectedPoints.ToString("N2")); // Fantasy Points as the fourth column

                listViewFinalTeam.Items.Add(item); // Add the player to your ListView

                // Add to totals
                totalFantasyPoints += player.FDProjectedPoints;
                totalSalary += (int)(player.Salary * 1000);
            }

            // Add the totals row
            ListViewItem totalsItem = new ListViewItem("Total");
            totalsItem.SubItems.Add(""); // Blank for the Name column
            totalsItem.SubItems.Add($"${totalSalary}"); // Total Salary formatted as currency
            totalsItem.SubItems.Add(totalFantasyPoints.ToString("N2")); // Total Fantasy Points

            listViewFinalTeam.Items.Add(totalsItem); // Add the totals to your ListView

            // Now that all items are added, resize the columns.
            ResizeListViewColumns();

            // Refresh the ListView to show the new items
            listViewFinalTeam.Refresh();


        }

        private void ResizeListViewColumns()
        {
            listViewFinalTeam.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewFinalTeam.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // The Name column should fill the remaining space
            int totalWidthOfOtherColumns = 0;
            for (int i = 0; i < listViewFinalTeam.Columns.Count; i++)
            {
                // Exclude the Name column index, if it's not 1, adjust the index here
                if (i != 1)
                {
                    totalWidthOfOtherColumns += listViewFinalTeam.Columns[i].Width;
                }
            }
            // Set the Name column to fill the rest of the ListView width
            int nameColumnIndex = 1; // Adjust if the Name column index is different
            listViewFinalTeam.Columns[nameColumnIndex].Width = listViewFinalTeam.Width - totalWidthOfOtherColumns - SystemInformation.VerticalScrollBarWidth;
        }


        private async Task<List<Player>> GetAllPlayerDataAsync()
        {
            DataTable dataTable = await GetTableFromWebAsync("https://www.dailyfantasyfuel.com/nba/projections/fanduel");
            List<Player> players = new List<Player>();

            foreach (DataRow row in dataTable.Rows)
            {
                Player player = new Player
                {
                    Positions = row["POS"].ToString().Split('/').ToList(), // Assuming positions are separated by a '/'
                    Name = row["NAME"].ToString(),
                    Salary = double.TryParse(row["SALARY"].ToString().Replace("$", "").Replace("k", "000"), out double salary) ? salary : 0,
                    Rest = int.TryParse(row["REST"].ToString(), out int rest) ? rest : 0,
                    Start = row["START"].ToString(),
                    Team = row["TEAM"].ToString(),
                    Opp = row["OPP"].ToString(),
                    DvP = int.TryParse(row["DvP"].ToString(), out int dvp) ? dvp : 0,
                    FDProjectedPoints = double.TryParse(row["FD FP PROJECTED"].ToString(), out double fdPts) ? fdPts : 0,
                    ValueProjected = double.TryParse(row["VALUE PROJECTED"].ToString(), out double valueProjected) ? valueProjected : 0,
                    FPMin = double.TryParse(row["FP MIN"].ToString(), out double fpMin) ? fpMin : 0,
                    FPAvg = double.TryParse(row["FP AVG"].ToString(), out double fpAvg) ? fpAvg : 0,
                    FPMax = double.TryParse(row["FP MAX"].ToString(), out double fpMax) ? fpMax : 0,
                    OverUnder = double.TryParse(row["O/U"].ToString(), out double overUnder) ? overUnder : 0,
                    TeamPoints = double.TryParse(row["TM PTS"].ToString(), out double teamPoints) ? teamPoints : 0
                };

                players.Add(player);
            }

            return players;
        }
        private async void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Define your position requirements
                Dictionary<string, int> positionRequirements = new Dictionary<string, int>
        {
            {"PG", 2},
            {"SG", 2},
            {"PF", 2},
            {"SF", 2},
            {"C", 1}
        };

                List<Player> allPlayers = await GetAllPlayerDataAsync();
                if (allPlayers == null || !allPlayers.Any())
                {
                    MessageBox.Show("No players were fetched.");
                    return;
                }

                // Now pass the position requirements dictionary to the method
                List<Player> optimalTeam = KnapsackOptimizeTeam(allPlayers, 60000, positionRequirements);

                if (optimalTeam == null || !optimalTeam.Any())
                {
                    MessageBox.Show("No optimal team could be formed.");
                    return;
                }

                DisplayFinalTeam(optimalTeam);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        public const int TotalPlayersToSelect = 9;

        public List<Player> KnapsackOptimizeTeam(List<Player> players, double salaryCap, Dictionary<string, int> positionRequirements)
        {
            // Initialize lists to hold position-specific optimal players and a set to track selected player names
            Dictionary<string, List<Player>> positionPlayers = new Dictionary<string, List<Player>>();
            HashSet<string> selectedPlayerNames = new HashSet<string>();

            foreach (var position in positionRequirements.Keys)
            {
                positionPlayers[position] = new List<Player>();
            }

            // Now, we should consider each player for all positions they are eligible for
            foreach (var player in players)
            {
                foreach (var position in player.Positions)
                {
                    if (positionRequirements.ContainsKey(position))
                    {
                        positionPlayers[position].Add(player);
                    }
                }
            }

            // Combine all position players into one list while ensuring we respect the position constraints
            List<Player> finalTeam = new List<Player>();
            double totalSalary = 0; // Track the total salary of the selected team

            foreach (var position in positionRequirements.Keys)
            {
                List<Player> eligiblePlayers = positionPlayers[position]
                    .Where(p => !selectedPlayerNames.Contains(p.Name)) // Filter out already selected players
                    .OrderByDescending(p => AdjustedProjectedPoints(p) / p.Salary)
                    .ToList();

                List<Player> selectedForPosition = KnapsackForPosition(eligiblePlayers, salaryCap - totalSalary, positionRequirements[position]);

                // Add the selected players to the final team and mark them as selected
                foreach (var player in selectedForPosition)
                {
                    finalTeam.Add(player);
                    selectedPlayerNames.Add(player.Name); // Keep track of selected players by name
                    totalSalary += player.Salary; // Update the total salary
                }
            }

            // Check if the total salary exceeds the cap
            if (totalSalary > salaryCap)
            {
                // Handle the situation by removing the highest-priced players until the team fits within the cap
                while (totalSalary > salaryCap)
                {
                    // Identify the player with the highest salary
                    Player playerToRemove = finalTeam.OrderByDescending(p => p.Salary).FirstOrDefault();

                    if (playerToRemove != null)
                    {
                        finalTeam.Remove(playerToRemove);
                        selectedPlayerNames.Remove(playerToRemove.Name); // Remove from the selected player names
                        totalSalary -= playerToRemove.Salary; // Subtract the removed player's salary from the total
                    }
                    else
                    {
                        // If no player can be removed without going below the cap, break out of the loop
                        break;
                    }
                }
            }

            // Final check to make sure we are within the salary cap
            if (totalSalary > salaryCap)
            {
                throw new InvalidOperationException("Unable to form a valid team under the salary cap.");
            }

            return finalTeam;
        }


        private List<Player> KnapsackForPosition(List<Player> players, double salaryCap, int positionCount)
        {
            // Sort the players by the value they provide divided by their salary
            var sortedPlayers = players.OrderByDescending(p => AdjustedProjectedPoints(p) / p.Salary).ToList();

            List<Player> selectedPlayers = new List<Player>();
            double currentSalary = 0;

            foreach (var player in sortedPlayers)
            {
                // If adding this player doesn't exceed the salary cap and we still need players for the position
                if (currentSalary + player.Salary <= salaryCap && selectedPlayers.Count < positionCount)
                {
                    selectedPlayers.Add(player);
                    currentSalary += player.Salary;
                }
            }

            // If we couldn't find enough players for the position within the salary cap, this needs to be handled
            if (selectedPlayers.Count < positionCount)
            {
                // Handle the situation, maybe by selecting cheaper players or changing the algorithm
            }

            return selectedPlayers;
        }

        // This is a simplified version. You need to define how exactly you want to weigh these metrics.
        public double AdjustedProjectedPoints(Player player)
        {
            // Start with base projected points
            double adjustedPoints = player.FDProjectedPoints;

            // Adjust based on Defense versus Position (DvP)
            // In this system, a higher DvP ranking means a better matchup for the player.
            if (player.DvP >= 15) // If the player is up against one of the worst defenses for their position
            {
                adjustedPoints *= 1.1; // Increase points by 10% for easy matchups
            }
            else if (player.DvP <= 14) // If the player is up against one of the best defenses for their position
            {
                adjustedPoints *= 0.9; // Decrease points by 10% for tough matchups
            }
            // Adjust based on Over/Under (O/U)
            // Let's assume a higher O/U means a higher-scoring game, which is better for fantasy points.
            if (player.OverUnder > 220) // If it's expected to be a high-scoring game
            {
                adjustedPoints *= 1.05; // Increase points by 5%
            }

            // Adjust based on the player's team projected points
            // Let's assume more team points means more opportunities for the player.
            if (player.TeamPoints > 110) // If the player's team is expected to score a lot
            {
                adjustedPoints *= 1.05; // Increase points by 5%
            }

            return adjustedPoints;
        }



    }
}
