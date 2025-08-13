using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DiscordRPC;
using MsBox.Avalonia;
using System.Text.Json;
using PresenceClient.Avalonia.Models;

namespace PresenceClient.Avalonia.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // --- Private Fields ---
        private string _ipAddress = "192.168.0.226";
        private string _clientId = "";
        private bool _ignoreHomeScreen;
        private bool _isVerbose = true;
        private bool _minimizeToTray;
        private string _logOutput = "";
        private bool _isRunning;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _clientTask;
        
        private const int TcpPort = 0xCAFE; // 51966

        // --- Presence Info Fields ---
        private string _currentTitleName = "";
        private string _currentDetails = "";
        private string _currentLargeImageKey = "";
        private string _currentLargeImageText = "";
        private string _currentSmallImageText = "";


        // --- Public Properties (for data binding) ---
        public string IpAddress { get => _ipAddress; set => SetProperty(ref _ipAddress, value); }
        public string ClientId { get => _clientId; set => SetProperty(ref _clientId, value); }
        public bool IgnoreHomeScreen { get => _ignoreHomeScreen; set => SetProperty(ref _ignoreHomeScreen, value); }
        public bool IsVerbose { get => _isVerbose; set => SetProperty(ref _isVerbose, value); }
        public bool MinimizeToTray { get => _minimizeToTray; set => SetProperty(ref _minimizeToTray, value); }
        public string LogOutput { get => _logOutput; set => SetProperty(ref _logOutput, value); }
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(IsNotRunning));
                    StartCommand.RaiseCanExecuteChanged();
                    StopCommand.RaiseCanExecuteChanged();
                }
            }
        }
        public bool IsNotRunning => !IsRunning;
        
        // --- Public Presence Info Properties ---
        public string CurrentTitleName { get => _currentTitleName; set => SetProperty(ref _currentTitleName, value); }
        public string CurrentDetails { get => _currentDetails; set => SetProperty(ref _currentDetails, value); }
        public string CurrentLargeImageKey { get => _currentLargeImageKey; set => SetProperty(ref _currentLargeImageKey, value); }
        public string CurrentLargeImageText { get => _currentLargeImageText; set => SetProperty(ref _currentLargeImageText, value); }
        public string CurrentSmallImageText { get => _currentSmallImageText; set => SetProperty(ref _currentSmallImageText, value); }


        // --- Commands ---
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }

        public MainViewModel()
        {
            StartCommand = new RelayCommand(async () => await StartClient(), () => !IsRunning);
            StopCommand = new RelayCommand(StopClient, () => IsRunning);
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettings());
            
            LoadSettings();
            ClearPresenceInfo();
        }
        
        private void ClearPresenceInfo()
        {
            CurrentTitleName = "N/A";
            CurrentDetails = "N/A";
            CurrentLargeImageKey = "N/A";
            CurrentLargeImageText = "N/A";
            CurrentSmallImageText = "N/A";
        }
        
        private async Task ShowErrorDialog(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", message);
            await box.ShowAsync();
        }
        
        private async Task ShowSuccessDialog(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Success", message);
            await box.ShowAsync();
        }

        private async Task StartClient()
        {
            if (!await ValidateInput()) return;

            IsRunning = true;
            Log("Starting client...");

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _clientTask = Task.Run(() => MainLoop(token), token);
        }

        public void StopClient()
        {
            if (!IsRunning || _cancellationTokenSource == null) return;

            Log("Stopping client...");
            _cancellationTokenSource.Cancel();
            // Removed synchronous Wait to avoid blocking UI thread during shutdown
            IsRunning = false;
            ClearPresenceInfo();
            Log("Client stopped.");
        }

        private async Task MainLoop(CancellationToken token)
        {
            using var rpc = new DiscordRpcClient(ClientId);
            rpc.Initialize();

            string lastProgramName = "";
            DateTime? startTimer = null;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Log($"Attempting to connect to {IpAddress}:{TcpPort}...");
                    using var client = new TcpClient();
                    await client.ConnectAsync(IpAddress, TcpPort, token);
                    Log("Successfully connected.");
                    
                    var stream = client.GetStream();
                    client.ReceiveTimeout = 5000;

                    while (!token.IsCancellationRequested && client.Connected)
                    {
                        var buffer = new byte[628];
                        int bytesRead = await stream.ReadAsync(buffer, token);

                        if (bytesRead == 0)
                        {
                            Log("Connection closed by remote host. Reconnecting...");
                            break;
                        }

                        var title = new TitlePacket(buffer);
                        
                        if (title.Name == lastProgramName)
                        {
                            await Task.Delay(1000, token);
                            continue;
                        }

                        Log($"Received Title: {title.Name} (PID: {title.PID:X16})");
                        startTimer = DateTime.UtcNow;
                        lastProgramName = title.Name;

                        if (IgnoreHomeScreen && title.Name == "Home Menu")
                        {
                            Log("Ignoring Home Menu, clearing presence.");
                            rpc.ClearPresence();
                            ClearPresenceInfo();
                        }
                        else
                        {
                            UpdatePresence(rpc, title, startTimer.Value);
                        }
                        await Task.Delay(1000, token);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log($"An error occurred: {ex.Message}. Retrying in 5 seconds...");
                    rpc.ClearPresence();
                    ClearPresenceInfo();
                    await Task.Delay(5000, token);
                }
            }

            rpc.ClearPresence();
            rpc.Deinitialize();
            Log("Main loop terminated.");
        }

        private void UpdatePresence(DiscordRpcClient rpc, TitlePacket title, DateTime startTime)
        {
            string details;
            string largeImageKey;
            string largeImageText = title.Name;
            string smallImageKey = "";
            string smallImageText = "";

            if (title.Name == "Home Menu")
            {
                largeImageKey = "switch";
                details = "Navigating the Home Menu";
                largeImageText = "Home Menu";
                smallImageText = "On the Switch";
                smallImageKey = "switch"; // Assuming you want a small image here too; adjust if not
            }
            else
            {
                smallImageText = "Nintendo Switch";
                largeImageKey = $"0{title.PID:x}";
                details = title.Name;
                smallImageKey = "switch"; // Add this to enable the small image tooltip
            }
            
            if (details.Length > 128) details = details.Substring(0, 128);
            if (largeImageText.Length > 128) largeImageText = largeImageText.Substring(0, 128);
            if (smallImageText.Length > 128) smallImageText = smallImageText.Substring(0, 128);
            if (largeImageKey.Length > 32) largeImageKey = largeImageKey.Substring(0, 32);

            var presence = new RichPresence()
            {
                Details = details,
                Assets = new Assets() 
                { 
                    LargeImageKey = largeImageKey, 
                    LargeImageText = largeImageText, 
                    SmallImageKey = smallImageKey, 
                    SmallImageText = smallImageText 
                },
                Timestamps = new Timestamps(startTime)
            };
            
            rpc.SetPresence(presence);
            
            CurrentTitleName = largeImageText;
            CurrentDetails = details;
            CurrentLargeImageKey = largeImageKey;
            CurrentLargeImageText = largeImageText;
            CurrentSmallImageText = smallImageText;
            
            Log($"Updated presence: {details}");
        }
        
        private async Task SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    IpAddress = this.IpAddress,
                    ClientId = this.ClientId,
                    IgnoreHomeScreen = this.IgnoreHomeScreen,
                    IsVerbose = this.IsVerbose,
                    MinimizeToTray = this.MinimizeToTray
                };

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string settingsDir = Path.Combine(appDataPath, "SwitchPresenceClient");
                Directory.CreateDirectory(settingsDir); // Ensure the directory exists
                string settingsFile = Path.Combine(settingsDir, "settings.json");

                var serializeOptions = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, serializeOptions);
                File.WriteAllText(settingsFile, json);
                
                Log("Settings saved successfully.");
                await ShowSuccessDialog("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Log($"Error saving settings: {ex.Message}");
                await ShowErrorDialog($"Error saving settings: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string settingsFile = Path.Combine(appDataPath, "SwitchPresenceClient", "settings.json");

                if (File.Exists(settingsFile))
                {
                    string json = File.ReadAllText(settingsFile);
                    var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, deserializeOptions);

                    if (settings != null)
                    {
                        IpAddress = settings.IpAddress;
                        ClientId = settings.ClientId;
                        IgnoreHomeScreen = settings.IgnoreHomeScreen;
                        IsVerbose = settings.IsVerbose;
                        MinimizeToTray = settings.MinimizeToTray;
                        Log("Settings loaded successfully.");
                    }
                }
                else
                {
                    Log("No settings file found. Using default values.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading settings: {ex.Message}");
            }
        }

        private async Task<bool> ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                await ShowErrorDialog("Please enter a Discord Client ID.");
                return false;
            }

            var ipRegex = new Regex(@"^(25[0-5]|2[0-4][0-9]|[0-1]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[0-1]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[0-1]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[0-1]?[0-9][0-9]?)$");
            if (!ipRegex.IsMatch(IpAddress))
            {
                await ShowErrorDialog("The entered IP address is not valid.");
                return false;
            }
            return true;
        }

        private void Log(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (IsVerbose)
                {
                    LogOutput += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
                }
            });
        }
    }
}