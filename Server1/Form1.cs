using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Server1
{
    public partial class ServerForm : Form
    {
        TcpListener lyssnare; // TCP-lyssnare för att acceptera inkommande anslutningar
        List<TcpClient> anslutnaKlienter = new List<TcpClient>(); // Lista över anslutna klienter
        int port = 12345; // Portnummer för servern

        public ServerForm()
        {
            InitializeComponent();
        }

        // Händelsehanterare för när formuläret laddas
        private void Form1_Load(object sender, EventArgs e)
        {
         
        }

        // Händelsehanterare för klick på startknappen
        private void btnStarta_Click(object sender, EventArgs e)
        {
            try
            {
                lyssnare = new TcpListener(IPAddress.Any, port); // Skapa en ny TCP-lyssnare på angiven port
                lyssnare.Start(); // Starta lyssnaren
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, Text); // Visa felmeddelande om lyssnaren inte kunde startas
                return;
            }

            btnStarta.Enabled = false; // Inaktivera startknappen efter att servern har startats
            StartaMottagning(); // Börja acceptera inkommande anslutningar
        }

        // Metod för att starta mottagning av anslutningar asynkront
        public async Task StartaMottagning()
        {
            while (true)
            {
                TcpClient klient = await lyssnare.AcceptTcpClientAsync(); // Vänta på en inkommande anslutning
                anslutnaKlienter.Add(klient); // Lägg till den nya klienten i listan över anslutna klienter
                HanteraKlient(klient); // Hantera den nya klientens kommunikation
            }
        }

        // Metod för att hantera en klient asynkront
        private async void HanteraKlient(TcpClient klient)
        {
            byte[] buffert = new byte[1024]; // Buffer för att lagra mottagna data
            int n;

            try
            {
                // Läs data från klienten tills anslutningen stängs
                while ((n = await klient.GetStream().ReadAsync(buffert, 0, buffert.Length)) != 0)
                {
                    string meddelande = Encoding.Unicode.GetString(buffert, 0, n); // Konvertera byte-array till string

                    if (meddelande.StartsWith("FILE:"))
                    {
                        // Hantera filöverföringsmeddelande
                        HandleFileTransfer(klient, meddelande.Substring(5)); // Ta bort "FILE:" prefixet och skicka resten som filnamn
                    }
                    else
                    {
                        // Vanligt textmeddelande, skicka till alla anslutna klienter
                        LoggaMeddelande(meddelande); // Logga meddelandet i serverns inkorg
                        SkickaTillAllaKlienter(meddelande, klient); // Skicka meddelandet till alla andra klienter
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, Text); // Visa felmeddelande om något går fel
            }

            klient.Close(); // Stäng anslutningen när vi är klara
        }

        // Metod för att hantera filöverföring asynkront
        private async void HandleFileTransfer(TcpClient klient, string fileName)
        {
            try
            {
                using (var fileStream = File.Create(fileName)) // Skapa en fil för att spara den mottagna data
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    // Läs data från klienten och skriv den till filen
                    while ((bytesRead = await klient.GetStream().ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }

                LoggaMeddelande($"File received: {fileName}"); // Logga att filen har mottagits
            }
            catch (Exception ex)
            {
                LoggaMeddelande($"Error receiving file: {ex.Message}"); // Logga felmeddelande om något går fel
            }
        }

        // Metod för att skicka meddelanden till alla anslutna klienter utom ursprungsklienten
        private void SkickaTillAllaKlienter(string meddelande, TcpClient ursprungsklient)
        {
            foreach (TcpClient klient in anslutnaKlienter)
            {
                if (klient != ursprungsklient) // Skicka inte tillbaka till ursprungsklienten
                {
                    byte[] data = Encoding.Unicode.GetBytes(meddelande); // Konvertera meddelandet till byte-array
                    klient.GetStream().WriteAsync(data, 0, data.Length); // Skicka meddelandet asynkront
                }
            }
        }

        // Metod för att logga meddelanden i serverns inkorg
        private void LoggaMeddelande(string meddelande)
        {
            if (tbxInkorg.InvokeRequired)
            {
                // Om inkorgens UI-kontroll kräver att uppdateras från en annan tråd
                tbxInkorg.Invoke(new MethodInvoker(delegate { tbxInkorg.AppendText(meddelande + Environment.NewLine); }));
            }
            else
            {
                // Om vi redan är på UI-tråden
                tbxInkorg.AppendText(meddelande + Environment.NewLine);
            }
        }
    }
}
