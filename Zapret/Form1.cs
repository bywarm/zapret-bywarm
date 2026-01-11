using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zapret_ByWarm
{
    public partial class Main : Form
    {
        // Control declarations - a bit messy
        private ComboBox strategiesComboBox; private CheckedListBox servicesCheckedListBox; private Button runButton;
        private Button closeZapretButton; private Button editListsButton; private Button editIpsetButton;
        private Button closeButton; private Button testButton;
        private Label strategyDescriptionLabel; private Label selectedServicesLabel; private Label statusLabel;
        private ComboBox languageComboBox; private Label titleLabel; private Label strategyLabel; private Label servicesLabel;
        private CheckBox enableGameFilterCheckBox; private CheckBox developerModeCheckBox;
        private TextBox batchPreviewTextBox; private TextBox logsTextBox;
        private TabControl mainTabControl; private TabPage mainTabPage; private TabPage logsTabPage;
        private Label gameFilterLabel; private Label developerLabel; private Label logsTabLabel; private Label commandLabel;
        private Button clearLogsButton; private Label languageLabel;

        // Language support
        private class Translation
        {
            public Dictionary<string, string> English = new Dictionary<string, string>();
            public Dictionary<string, string> Russian = new Dictionary<string, string>();
        }
        private Translation translations = new Translation();
        private string currentLanguage = "en";
        private bool updatingUI = false;

        // Strategy data
        private class StrategyInfo
        {
            public string Name { get; set; }
            public string NameRu { get; set; }
            public string Type { get; set; }
            public string TypeRu { get; set; }
            public string Description { get; set; }
            public string DescriptionRu { get; set; }
            public string Template { get; set; }
            public bool UsesLists { get; set; } = true;
            public bool UsesIpset { get; set; } = true;
        }

        // Service data
        private class ServiceInfo
        {
            public string Name { get; set; }
            public string NameRu { get; set; }
            public string Description { get; set; }
            public string DescriptionRu { get; set; }
            public string[] Domains { get; set; }
            public string[] TcpPorts { get; set; }
            public string[] UdpPorts { get; set; }
        }

        private List<StrategyInfo> strategies; private List<ServiceInfo> services;
        private List<string> selectedServices = new List<string>();
        private Dictionary<string, ServiceInfo> serviceMap = new Dictionary<string, ServiceInfo>();
        private string appDirectory; private string listsDirectory; private string binDirectory;
        private Process zapretProcess; private bool initializing = true; private StringBuilder logs = new StringBuilder();

        public Main()
        {
            InitializeTranslations();
            appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            listsDirectory = Path.Combine(appDirectory, "lists");
            binDirectory = Path.Combine(appDirectory, "bin");
            InitializeData();

            // Initialize form first
            this.Text = "Zapret Bywarm"; this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(25, 25, 25); this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9);

            SetupUI(); CheckDirectories(); UpdateSelectedServices(); UpdateListGeneral();
            UpdateCommandPreview();

            initializing = false;

            // Add Load event handler
            this.Load += Main_Load;
        }

        private void InitializeTranslations()
        {
            // English translations
            translations.English["title"] = "ZAPRET bywarm BETA";
            translations.English["strategy"] = "Select Strategy:";
            translations.English["services"] = "Select Services:"; translations.English["run"] = "▶ RUN ZAPRET";
            translations.English["stop"] = "⏹️ STOP ZAPRET"; translations.English["editLists"] = "📝 EDIT LISTS";
            translations.English["editIpset"] = "📊 EDIT IPSET"; translations.English["exit"] = "✕ EXIT";
            translations.English["test"] = "🔧 TEST"; translations.English["statusReady"] = "Ready. Select options and click RUN";
            translations.English["language"] = "Language:"; translations.English["allSelected"] = "All services selected";
            translations.English["noSelected"] = "No services selected\n\nCheck services to enable";
            translations.English["gameFilter"] = "Enable Game Filter"; translations.English["developerMode"] = "Developer Mode";
            translations.English["tabMain"] = "Main"; translations.English["tabLogs"] = "Logs & Commands";
            translations.English["commandPreview"] = "Generated Command:"; translations.English["logs"] = "Logs:";
            translations.English["clearLogs"] = "Clear Logs";

            // Russian translations
            translations.Russian["title"] = "ZAPRET bywarm"; translations.Russian["strategy"] = "Выберите стратегию:";
            translations.Russian["services"] = "Выберите сервисы:"; translations.Russian["run"] = "▶ ЗАПУСТИТЬ ZAPRET";
            translations.Russian["stop"] = "⏹️ ОСТАНОВИТЬ ZAPRET"; translations.Russian["editLists"] = "📝 РЕДАКТИРОВАТЬ СПИСКИ";
            translations.Russian["editIpset"] = "📊 РЕДАКТИРОВАТЬ IPSET"; translations.Russian["exit"] = "✕ ВЫХОД";
            translations.Russian["test"] = "🔧 ТЕСТ"; translations.Russian["statusReady"] = "Готово. Выберите опции и нажмите RUN";
            translations.Russian["language"] = "Язык:"; translations.Russian["allSelected"] = "Все сервисы выбраны";
            translations.Russian["noSelected"] = "Сервисы не выбраны\n\nОтметьте сервисы для включения";
            translations.Russian["gameFilter"] = "Включить Game Filter"; translations.Russian["developerMode"] = "Режим разработчика";
            translations.Russian["tabMain"] = "Главная"; translations.Russian["tabLogs"] = "Логи и команды";
            translations.Russian["commandPreview"] = "Сгенерированный конфиг"; translations.Russian["logs"] = "Логи:";
            translations.Russian["clearLogs"] = "Очистить логи";
        }

        private void Main_Load(object sender, EventArgs e) { Strategies_SelectedIndexChanged(null, null); }

        private void CheckDirectories()
        {
            // Create directories if they don't exist
            if (!Directory.Exists(listsDirectory)) Directory.CreateDirectory(listsDirectory);
            if (!Directory.Exists(binDirectory)) Directory.CreateDirectory(binDirectory);

            // Create default list files if they don't exist
            CreateDefaultListFiles();
        }

        private void CreateDefaultListFiles()
        {
            // Create list-general.txt with default domains
            string listGeneralPath = Path.Combine(listsDirectory, "list-general.txt");
            if (!File.Exists(listGeneralPath))
            {
                string defaultDomains = @"cloudflare-ech.com
encryptedsni.com
cloudflareaccess.com
cloudflareapps.com
cloudflarebolt.com
cloudflareclient.com
cloudflareinsights.com
cloudflareok.com
cloudflarepartners.com
cloudflareportal.com
cloudflarepreview.com
cloudflareresolve.com
cloudflaressl.com
cloudflarestatus.com
cloudflarestorage.com
cloudflarestream.com
cloudflaretest.com";

                File.WriteAllText(listGeneralPath, defaultDomains, new UTF8Encoding(false));
            }

            // Create other list files if they don't exist
            string[] listFiles = { "list-exclude.txt", "ipset-all.txt", "ipset-exclude.txt" };
            foreach (var file in listFiles)
            {
                string filePath = Path.Combine(listsDirectory, file);
                if (!File.Exists(filePath)) File.WriteAllText(filePath, "# Empty list\n", new UTF8Encoding(false));
            }
        }

        private void InitializeData()
        {
            // Initialize strategies
            strategies = new List<StrategyInfo>
            {
                new StrategyInfo
                {
                    Name = "Strategy #1 - MAIN", NameRu = "Стратегия #1 - MAIN", Type = "Flowseal",
                    TypeRu = "Flowseal", Description = "", DescriptionRu = "", UsesLists = true,
                    UsesIpset = true,
                    Template = @"--wf-tcp={TCP_PORTS},{GAME_FILTER} --wf-udp=443,19294-19344,50000-50100,{GAME_FILTER} ^
--filter-udp=443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-fake-discord=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-fake-stun=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-repeats=6 --new ^
--filter-l3=ipv4 --filter-tcp=443,2053,2083,2087,2096,8443,{GAME_FILTER} --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=syndata,multidisorder --new ^
--filter-udp=443 --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-udp={GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-autottl=2 --dpi-desync-repeats=14 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-cutoff=n3"
                },
                new StrategyInfo
                {
                    Name = "Strategy #2 - MAIN ALT", NameRu = "Стратегия #2 - MAIN ALT", Type = "Flowseal",
                    TypeRu = "Flowseal", Description = "", DescriptionRu = "", UsesLists = true,
                    UsesIpset = true,
                    Template = @"--wf-tcp={TCP_PORTS},{GAME_FILTER} --wf-udp=443,19294-19344,50000-50100,{GAME_FILTER} ^
--filter-udp=443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-fake-discord=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-fake-stun=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-repeats=6 --new ^
--filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=""{BIN}tls_clienthello_4pda_to.bin"" --dpi-desync-fake-tls-mod=none --new ^
--filter-tcp=443 --ip-id=zero --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=""{BIN}tls_clienthello_www_google_com.bin"" --new ^
--filter-tcp=80,443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=""{BIN}tls_clienthello_4pda_to.bin"" --dpi-desync-fake-tls-mod=none --new ^
--filter-udp=443 --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-tcp=80,443,{GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=^! --dpi-desync-fake-tls-mod=rnd,sni=www.google.com --dpi-desync-fake-tls=""{BIN}tls_clienthello_4pda_to.bin"" --dpi-desync-fake-tls-mod=none --new ^
--filter-udp={GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-autottl=2 --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-cutoff=n2"
                },
                new StrategyInfo
                {
                    Name = "Strategy #3 - Fake Multisplit", NameRu = "Стратегия #3 - Fake Multisplit",
                    Type = "Flowseal", TypeRu = "Flowseal", Description = "", DescriptionRu = "",
                    UsesLists = true, UsesIpset = true,
                    Template = @"--wf-tcp={TCP_PORTS},{GAME_FILTER} --wf-udp=443,19294-19344,50000-50100,{GAME_FILTER} ^
--filter-udp=443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=11 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-fake-discord=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-fake-stun=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-repeats=6 --new ^
--filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=fake,multisplit --dpi-desync-split-seqovl=654 --dpi-desync-split-pos=1 --dpi-desync-fooling=ts --dpi-desync-repeats=8 --dpi-desync-split-seqovl-pattern=""{BIN}tls_clienthello_max_ru.bin"" --dpi-desync-fake-tls=""{BIN}tls_clienthello_max_ru.bin"" --new ^
--filter-tcp=443 --ip-id=zero --dpi-desync=fake,multisplit --dpi-desync-split-seqovl=681 --dpi-desync-split-pos=1 --dpi-desync-fooling=ts --dpi-desync-repeats=8 --dpi-desync-split-seqovl-pattern=""{BIN}tls_clienthello_www_google_com.bin"" --dpi-desync-fake-tls=""{BIN}tls_clienthello_www_google_com.bin"" --new ^
--filter-tcp=80,443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake,multisplit --dpi-desync-split-seqovl=654 --dpi-desync-split-pos=1 --dpi-desync-fooling=ts --dpi-desync-repeats=8 --dpi-desync-split-seqovl-pattern=""{BIN}tls_clienthello_max_ru.bin"" --dpi-desync-fake-tls=""{BIN}tls_clienthello_max_ru.bin"" --new ^
--filter-udp=443 --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=11 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-tcp=80,443,{GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake,multisplit --dpi-desync-split-seqovl=654 --dpi-desync-split-pos=1 --dpi-desync-fooling=ts --dpi-desync-repeats=8 --dpi-desync-split-seqovl-pattern=""{BIN}tls_clienthello_max_ru.bin"" --dpi-desync-fake-tls=""{BIN}tls_clienthello_max_ru.bin"" --new ^
--filter-udp={GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-autottl=2 --dpi-desync-repeats=10 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-cutoff=n2"
                },
                new StrategyInfo
                {
                    Name = "Strategy #4 - Host Fake Split", NameRu = "Стратегия #4 - Host Fake Split",
                    Type = "Flowseal", TypeRu = "Flowseal", Description = "", DescriptionRu = "",
                    UsesLists = true, UsesIpset = true,
                    Template = @"--wf-tcp={TCP_PORTS},{GAME_FILTER} --wf-udp=443,19294-19344,50000-50100,{GAME_FILTER} ^
--filter-udp=443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-fake-discord=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-fake-stun=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-repeats=6 --new ^
--filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=hostfakesplit --dpi-desync-repeats=4 --dpi-desync-fooling=ts --dpi-desync-hostfakesplit-mod=host=ozon.ru --new ^
--filter-tcp=443 --ip-id=zero --dpi-desync=hostfakesplit --dpi-desync-repeats=4 --dpi-desync-fooling=ts --dpi-desync-hostfakesplit-mod=host=www.google.com --new ^
--filter-tcp=80,443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=hostfakesplit --dpi-desync-repeats=4 --dpi-desync-fooling=ts,md5sig --dpi-desync-hostfakesplit-mod=host=ozon.ru --new ^
--filter-udp=443 --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-tcp=80,443,{GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=hostfakesplit --dpi-desync-repeats=4 --dpi-desync-fooling=ts --dpi-desync-hostfakesplit-mod=host=ozon.ru --new ^
--filter-udp={GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-autottl=2 --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-cutoff=n2"
                },
                new StrategyInfo
                {
                    Name = "Strategy #5 - Simple Fake", NameRu = "Стратегия #5 - Simple Fake",
                    Type = "Simple Fake", TypeRu = "Simple Fake", Description = "", DescriptionRu = "",
                    UsesLists = true, UsesIpset = true,
                    Template = @"--wf-tcp={TCP_PORTS},{GAME_FILTER} --wf-udp=443,19294-19344,50000-50100,{GAME_FILTER} ^
--filter-udp=443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-udp=19294-19344,50000-50100 --filter-l7=discord,stun --dpi-desync=fake --dpi-desync-fake-discord=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-fake-stun=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-repeats=6 --new ^
--filter-tcp=2053,2083,2087,2096,8443 --hostlist-domains=discord.media --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=""{BIN}tls_clienthello_www_google_com.bin"" --new ^
--filter-tcp=443 --ip-id=zero --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=""{BIN}tls_clienthello_www_google_com.bin"" --new ^
--filter-tcp=80,443 --hostlist=""{LISTS}list-general.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=""{BIN}tls_clienthello_www_google_com.bin"" --new ^
--filter-udp=443 --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fake-quic=""{BIN}quic_initial_www_google_com.bin"" --new ^
--filter-tcp=80,443,{GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --hostlist-exclude=""{LISTS}list-exclude.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=ts --dpi-desync-fake-tls=""{BIN}tls_clienthello_www_google_com.bin"" --new ^
--filter-udp={GAME_FILTER} --ipset=""{LISTS}ipset-all.txt"" --ipset-exclude=""{LISTS}ipset-exclude.txt"" --dpi-desync=fake --dpi-desync-autottl=2 --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp=""{BIN}quic_initial_www_google_com.bin"" --dpi-desync-cutoff=n2"
                }
            };

            // Initialize services
            services = new List<ServiceInfo>
            {
                new ServiceInfo
                {
                    Name = "Youtube", NameRu = "YouTube", Description = "Video streaming platform",
                    DescriptionRu = "Видео стриминговый сервис", Domains = new string[] {
                        "youtube.com", "youtu.be", "googlevideo.com", "ytimg.com",
                        "youtube-nocookie.com", "youtube.googleapis.com"
                    }, TcpPorts = new string[] { "80", "443", "2053", "2083", "2087", "2096", "8443" },
                    UdpPorts = new string[] { "443" }
                },
                new ServiceInfo
                {
                    Name = "Discord", NameRu = "Discord", Description = "Communication platform",
                    DescriptionRu = "Платформа для общения", Domains = new string[] {
                        "discord.com", "discord.gg", "discord.media", "discordapp.com",
                        "discordapp.net", "discordcdn.com", "discordstatus.com",
                        "discord.gift", "discord.gifts", "discord.new", "discord.store"
                    }, TcpPorts = new string[] { "80", "443", "2053", "2083", "2087", "2096", "8443" },
                    UdpPorts = new string[] { "443", "19294-19344", "50000-50100" }
                },
                new ServiceInfo
                {
                    Name = "Roblox", NameRu = "Roblox", Description = "Gaming platform",
                    DescriptionRu = "Игровая платформа", Domains = new string[] {
                        "roblox.com", "www.roblox.com", "rbxcdn.com", "rbxlabs.com",
                        "robloxapp.com", "robloxgames.com", "robloxlabs.com",
                        "rblx.com", "rbx.com", "roblox.qq.com", "roblox.cn"
                    }, TcpPorts = new string[] { "80", "443" }, UdpPorts = new string[] { "443" }
                },
                new ServiceInfo
                {
                    Name = "Cloudflare", NameRu = "Cloudflare", Description = "CDN and security services",
                    DescriptionRu = "CDN и сервисы безопасности", Domains = new string[] {
                        "cloudflare.com", "cloudflare.net", "cloudflaressl.com",
                        "cloudflareaccess.com", "cloudflareapps.com",
                        "cloudflarebolt.com", "cloudflareclient.com"
                    }, TcpPorts = new string[] { "80", "443" }, UdpPorts = new string[] { "443" }
                },
                new ServiceInfo
                {
                    Name = "Whatsapp & Telegram", NameRu = "WhatsApp & Telegram",
                    Description = "Messaging applications", DescriptionRu = "Мессенджеры",
                    Domains = new string[] {
                        "whatsapp.com", "telegram.org", "t.me", "web.telegram.org",
                        "telegram.me", "telegram.dog", "telegram.xyz"
                    }, TcpPorts = new string[] { "80", "443", "5222", "5228", "4244" },
                    UdpPorts = new string[] { "443" }
                },
                new ServiceInfo
                {
                    Name = "Other Services", NameRu = "Другие сервисы",
                    Description = "Speedtest, X, Rutracker, etc.", DescriptionRu = "Speedtest, X, Rutracker и другие",
                    Domains = new string[] {
                        "twitter.com", "x.com", "rutracker.org", "speedtest.net",
                        "netflix.com", "twitch.tv", "steamcommunity.com"
                    }, TcpPorts = new string[] { "80", "443", "8080" }, UdpPorts = new string[] { "443" }
                }
            };

            // Create service map for quick lookup
            foreach (var service in services) serviceMap[service.Name] = service;
        }

        private void SetupUI()
        {
            // Create tab control
            mainTabControl = new TabControl
            {
                Location = new Point(10, 10),
                Size = new Size(860, 640),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                ItemSize = new Size(100, 25)
            };

            // Create main tab
            mainTabPage = new TabPage("Main"); mainTabPage.BackColor = Color.FromArgb(25, 25, 25);
            mainTabPage.ForeColor = Color.White;

            // Create logs tab
            logsTabPage = new TabPage("Logs & Commands"); logsTabPage.BackColor = Color.FromArgb(25, 25, 25);
            logsTabPage.ForeColor = Color.White;

            mainTabControl.TabPages.Add(mainTabPage); mainTabControl.TabPages.Add(logsTabPage);

            // Title Label
            titleLabel = new Label
            {
                Name = "titleLabel",
                Text = "ZAPRET BYWARM",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 255),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Language selection
            languageLabel = new Label
            {
                Name = "languageLabel",
                Text = "Language:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(600, 25)
            };

            languageComboBox = new ComboBox
            {
                Location = new Point(660, 20),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            languageComboBox.Items.AddRange(new string[] { "English", "Русский" }); languageComboBox.SelectedIndex = 0;

            // Strategy Selection
            strategyLabel = new Label
            {
                Name = "strategyLabel",
                Text = "Select Strategy:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 70)
            };

            strategiesComboBox = new ComboBox
            {
                Location = new Point(20, 95),
                Size = new Size(350, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // Populate strategies
            foreach (var strategy in strategies) strategiesComboBox.Items.Add(strategy.Name);
            strategiesComboBox.SelectedIndex = 0;

            // Strategy Description Label
            strategyDescriptionLabel = new Label
            {
                Location = new Point(380, 95),
                Size = new Size(400, 80),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Services Selection
            servicesLabel = new Label
            {
                Name = "servicesLabel",
                Text = "Select Services:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 140)
            };

            servicesCheckedListBox = new CheckedListBox
            {
                Location = new Point(20, 165),
                Size = new Size(350, 180),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                CheckOnClick = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Populate services
            foreach (var service in services) servicesCheckedListBox.Items.Add(service.Name, true);

            // Selected Services Label
            selectedServicesLabel = new Label
            {
                Location = new Point(380, 165),
                Size = new Size(400, 180),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGreen,
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "All services selected"
            };

            // Game Filter Checkbox
            enableGameFilterCheckBox = new CheckBox
            {
                Name = "enableGameFilterCheckBox",
                Text = "Enable Game Filter (UDP 1024-65535)",
                Location = new Point(20, 350),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Checked = true
            };

            // Developer Mode Checkbox
            developerModeCheckBox = new CheckBox
            {
                Name = "developerModeCheckBox",
                Text = "Developer Mode",
                Location = new Point(380, 350),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Checked = false
            };

            // Buttons
            runButton = new Button
            {
                Name = "runButton",
                Text = "▶ RUN ZAPRET",
                Location = new Point(20, 380),
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            runButton.FlatAppearance.BorderSize = 0; runButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 150, 0);

            closeZapretButton = new Button
            {
                Name = "closeZapretButton",
                Text = "⏹️ STOP ZAPRET",
                Location = new Point(210, 380),
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(120, 60, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeZapretButton.FlatAppearance.BorderSize = 0; closeZapretButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 80, 0);

            editListsButton = new Button
            {
                Name = "editListsButton",
                Text = "📝 EDIT LISTS",
                Location = new Point(400, 380),
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 80, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            editListsButton.FlatAppearance.BorderSize = 0; editListsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 100, 150);

            editIpsetButton = new Button
            {
                Name = "editIpsetButton",
                Text = "📊 EDIT IPSET",
                Location = new Point(590, 380),
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(80, 0, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            editIpsetButton.FlatAppearance.BorderSize = 0; editIpsetButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(100, 0, 150);

            // Test Button
            testButton = new Button
            {
                Name = "testButton",
                Text = "🔧 TEST",
                Location = new Point(20, 430),
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(120, 80, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            testButton.FlatAppearance.BorderSize = 0; testButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 100, 0);

            closeButton = new Button
            {
                Name = "closeButton",
                Text = "✕ EXIT",
                Location = new Point(210, 430),
                Size = new Size(180, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(120, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0; closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 0, 0);

            // Status Label
            statusLabel = new Label
            {
                Location = new Point(20, 480),
                Size = new Size(760, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Yellow,
                Text = "Ready. Select options and click RUN"
            };

            // Add controls to main tab
            mainTabPage.Controls.AddRange(new Control[] {
                titleLabel, languageLabel, languageComboBox, strategyLabel, strategiesComboBox,
                strategyDescriptionLabel, servicesLabel, servicesCheckedListBox, selectedServicesLabel,
                enableGameFilterCheckBox, developerModeCheckBox, runButton, closeZapretButton,
                editListsButton, editIpsetButton, testButton, closeButton, statusLabel
            });

            // Setup logs tab
            SetupLogsTab();

            // Add tab control to form
            this.Controls.Add(mainTabControl);

            // Wire up events
            strategiesComboBox.SelectedIndexChanged += Strategies_SelectedIndexChanged;
            servicesCheckedListBox.ItemCheck += Services_ItemCheck;
            runButton.Click += RunButton_Click;
            closeZapretButton.Click += CloseZapretButton_Click;
            editListsButton.Click += EditListsButton_Click;
            editIpsetButton.Click += EditIpsetButton_Click;
            testButton.Click += TestButton_Click;
            closeButton.Click += Close_Click;
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            enableGameFilterCheckBox.CheckedChanged += GameFilter_CheckedChanged;
            developerModeCheckBox.CheckedChanged += DeveloperMode_CheckedChanged;
        }

        private void SetupLogsTab()
        {
            // Batch Preview Label
            commandLabel = new Label
            {
                Text = "Generated Command:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            batchPreviewTextBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(820, 200),
                Multiline = true,
                Font = new Font("Consolas", 8),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.LightGray,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                WordWrap = true
            };

            // Logs Label
            logsTabLabel = new Label
            {
                Text = "Logs:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 260)
            };

            logsTextBox = new TextBox
            {
                Location = new Point(20, 285),
                Size = new Size(820, 300),
                Multiline = true,
                Font = new Font("Consolas", 8),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.LightGray,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                WordWrap = true
            };

            // Clear Logs Button
            clearLogsButton = new Button
            {
                Text = "Clear Logs",
                Location = new Point(750, 255),
                Size = new Size(90, 25),
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            clearLogsButton.FlatAppearance.BorderSize = 0; clearLogsButton.Click += (s, e) => {
                logs.Clear(); logsTextBox.Clear();
            };

            logsTabPage.Controls.AddRange(new Control[] {
                commandLabel, batchPreviewTextBox, logsTabLabel, logsTextBox, clearLogsButton
            });
        }

        private void UpdateUI()
        {
            if (updatingUI) return;
            updatingUI = true;

            try
            {
                var dict = currentLanguage == "ru" ? translations.Russian : translations.English;

                // Update controls with translations
                titleLabel.Text = dict["title"]; strategyLabel.Text = dict["strategy"];
                servicesLabel.Text = dict["services"]; runButton.Text = dict["run"];
                closeZapretButton.Text = dict["stop"]; editListsButton.Text = dict["editLists"];
                editIpsetButton.Text = dict["editIpset"]; testButton.Text = dict["test"];
                closeButton.Text = dict["exit"]; statusLabel.Text = dict["statusReady"];
                enableGameFilterCheckBox.Text = dict["gameFilter"]; developerModeCheckBox.Text = dict["developerMode"];
                languageLabel.Text = dict["language"];

                // Update tab titles
                mainTabPage.Text = dict["tabMain"]; logsTabPage.Text = dict["tabLogs"];
                commandLabel.Text = dict["commandPreview"]; logsTabLabel.Text = dict["logs"];
                clearLogsButton.Text = dict["clearLogs"];

                // Update selected services label
                UpdateSelectedServices();

                // Update strategies combo box
                int selectedIndex = strategiesComboBox.SelectedIndex;
                strategiesComboBox.Items.Clear();
                foreach (var strategy in strategies)
                    strategiesComboBox.Items.Add(currentLanguage == "ru" ? strategy.NameRu : strategy.Name);
                if (selectedIndex >= 0 && selectedIndex < strategiesComboBox.Items.Count)
                    strategiesComboBox.SelectedIndex = selectedIndex;

                // Update services checked list box
                List<bool> checkedStates = new List<bool>();
                for (int i = 0; i < servicesCheckedListBox.Items.Count; i++)
                    checkedStates.Add(servicesCheckedListBox.GetItemChecked(i));

                servicesCheckedListBox.Items.Clear();
                foreach (var service in services)
                    servicesCheckedListBox.Items.Add(currentLanguage == "ru" ? service.NameRu : service.Name);

                for (int i = 0; i < Math.Min(checkedStates.Count, servicesCheckedListBox.Items.Count); i++)
                    servicesCheckedListBox.SetItemChecked(i, checkedStates[i]);

                // Update strategy description
                Strategies_SelectedIndexChanged(null, null);

                // Update command preview
                UpdateCommandPreview();
            }
            finally
            {
                updatingUI = false;
            }
        }

        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentLanguage = languageComboBox.SelectedIndex == 1 ? "ru" : "en";
            UpdateUI(); AddLog($"Language changed to {currentLanguage}");
        }

        private void GameFilter_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCommandPreview(); AddLog($"Game filter {(enableGameFilterCheckBox.Checked ? "enabled" : "disabled")}");
        }

        private void DeveloperMode_CheckedChanged(object sender, EventArgs e)
        {
            if (developerModeCheckBox.Checked) { mainTabControl.SelectedTab = logsTabPage; AddLog("Developer mode enabled"); }
            else mainTabControl.SelectedTab = mainTabPage;
            UpdateCommandPreview();
        }

        private void Strategies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (strategiesComboBox.SelectedIndex >= 0 && strategiesComboBox.SelectedIndex < strategies.Count)
            {
                var strategy = strategies[strategiesComboBox.SelectedIndex];

                string name = currentLanguage == "ru" ? strategy.NameRu : strategy.Name;
                string type = currentLanguage == "ru" ? strategy.TypeRu : strategy.Type;
                string description = currentLanguage == "ru" ? strategy.DescriptionRu : strategy.Description;

                string listInfo = strategy.UsesLists ?
                    (currentLanguage == "ru" ? "Использует списки: ДА" : "Uses lists: YES") :
                    (currentLanguage == "ru" ? "Использует списки: НЕТ" : "Uses lists: NO");
                string ipsetInfo = strategy.UsesIpset ?
                    (currentLanguage == "ru" ? "Использует ipset: ДА" : "Uses ipset: YES") :
                    (currentLanguage == "ru" ? "Использует ipset: НЕТ" : "Uses ipset: NO");

                strategyDescriptionLabel.Text = $"{name}\n{type}\n{listInfo}\n{ipsetInfo}\n\n{description}";

                AddLog($"Strategy changed to: {name}");
                UpdateCommandPreview();
            }
        }

        private void Services_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (initializing) return;

            this.BeginInvoke((MethodInvoker)delegate
            {
                UpdateSelectedServices(); UpdateListGeneral(); UpdateCommandPreview();
            });
        }

        private void UpdateSelectedServices()
        {
            selectedServices.Clear(); StringBuilder serviceInfo = new StringBuilder();
            var dict = currentLanguage == "ru" ? translations.Russian : translations.English;

            if (currentLanguage == "ru") serviceInfo.AppendLine("Выбранные сервисы:\n");
            else serviceInfo.AppendLine("Selected Services:\n");

            for (int i = 0; i < servicesCheckedListBox.Items.Count; i++)
            {
                if (servicesCheckedListBox.GetItemChecked(i))
                {
                    string serviceDisplayName = servicesCheckedListBox.Items[i].ToString();

                    // Find the service by display name
                    foreach (var service in services)
                    {
                        string serviceName = currentLanguage == "ru" ? service.NameRu : service.Name;
                        if (serviceName == serviceDisplayName)
                        {
                            selectedServices.Add(service.Name); serviceInfo.AppendLine($"✓ {serviceName}");
                            break;
                        }
                    }
                }
            }

            if (selectedServices.Count > 0) selectedServicesLabel.Text = serviceInfo.ToString();
            else selectedServicesLabel.Text = dict["noSelected"];
        }

        private void UpdateListGeneral()
        {
            try
            {
                string listGeneralPath = Path.Combine(listsDirectory, "list-general.txt");
                HashSet<string> allDomains = new HashSet<string>();

                // Add base Cloudflare domains
                string baseDomains = @"cloudflare-ech.com
encryptedsni.com
cloudflareaccess.com
cloudflareapps.com
cloudflarebolt.com
cloudflareclient.com
cloudflareinsights.com
cloudflareok.com
cloudflarepartners.com
cloudflareportal.com
cloudflarepreview.com
cloudflareresolve.com
cloudflaressl.com
cloudflarestatus.com
cloudflarestorage.com
cloudflarestream.com
cloudflaretest.com";

                foreach (var domain in baseDomains.Split('\n')) allDomains.Add(domain.Trim());

                // Add domains from selected services
                foreach (var serviceName in selectedServices)
                {
                    if (serviceMap.ContainsKey(serviceName))
                    {
                        var service = serviceMap[serviceName];
                        foreach (var domain in service.Domains) allDomains.Add(domain.Trim());
                    }
                }

                // Write to file
                File.WriteAllText(listGeneralPath, string.Join(Environment.NewLine, allDomains), new UTF8Encoding(false));

                // Update status
                if (currentLanguage == "ru")
                    statusLabel.Text = $"Списки обновлены: {selectedServices.Count} сервисов, {allDomains.Count} доменов";
                else statusLabel.Text = $"Lists updated: {selectedServices.Count} services, {allDomains.Count} domains";

                AddLog($"list-general.txt updated with {allDomains.Count} domains");
            }
            catch (Exception ex)
            {
                if (currentLanguage == "ru") statusLabel.Text = $"Ошибка обновления списков: {ex.Message}";
                else statusLabel.Text = $"Error updating lists: {ex.Message}";
                AddLog($"Error updating list-general.txt: {ex.Message}");
            }
        }

        private string GenerateWinwsCommand()
        {
            if (selectedServices.Count == 0)
            {
                if (currentLanguage == "ru") return "Пожалуйста, выберите хотя бы один сервис.";
                else return "Please select at least one service.";
            }

            var selectedStrategy = strategies[strategiesComboBox.SelectedIndex];

            // Fixed TCP ports from strategies and service.bat
            string tcpPortsStr = "80,443,2053,2083,2087,2096,8443";
            string gameFilter = enableGameFilterCheckBox.Checked ? "1024-65535" : "12";

            // Get the template and replace placeholders
            string command = selectedStrategy.Template
                .Replace("{TCP_PORTS}", tcpPortsStr)
                .Replace("{GAME_FILTER}", gameFilter)
                .Replace("{BIN}", "%BIN%")
                .Replace("{LISTS}", "%LISTS%");

            return command;
        }

        private void UpdateCommandPreview()
        {
            string command = GenerateWinwsCommand();
            string preview = $"start \"zapret\" /min \"%BIN%winws.exe\" ^\r\n{command.Replace("\n", " ^\r\n")}";

            if (developerModeCheckBox.Checked)
            {
                batchPreviewTextBox.Text = preview; batchPreviewTextBox.Visible = true;
            }
            else
            {
                batchPreviewTextBox.Text = string.Empty;
            }
        }

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";

            logs.AppendLine(logEntry);

            if (developerModeCheckBox.Checked && logsTextBox != null)
            {
                logsTextBox.AppendText(logEntry + Environment.NewLine); logsTextBox.ScrollToCaret();
            }
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            if (selectedServices.Count == 0)
            {
                string message = currentLanguage == "ru" ?
                    "Пожалуйста, выберите хотя бы один сервис для тестирования." :
                    "Please select at least one service to test.";

                MessageBox.Show(message,
                    currentLanguage == "ru" ? "Сервисы не выбраны" : "No Services Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Disable test button during testing
            testButton.Enabled = false;
            testButton.Text = currentLanguage == "ru" ? "⏳ ТЕСТИРОВАНИЕ..." : "⏳ TESTING...";

            statusLabel.ForeColor = Color.Yellow;
            if (currentLanguage == "ru") statusLabel.Text = "Запуск тестов стратегий и сервисов...";
            else statusLabel.Text = "Running strategy and service tests...";

            AddLog("=== Starting Strategy & Service Tests ===");

            try
            {
                // Test 1: Generate and validate command
                AddLog("Test 1: Command generation");
                string command = GenerateWinwsCommand();
                if (command.Contains("Пожалуйста") || command.Contains("Please select")) throw new Exception("No services selected");
                AddLog("✓ Command generated successfully");

                // Test 2: Check list-general.txt
                AddLog("Test 2: Checking list-general.txt");
                string listGeneralPath = Path.Combine(listsDirectory, "list-general.txt");
                if (!File.Exists(listGeneralPath)) throw new Exception("list-general.txt not found");
                int domainCount = File.ReadAllLines(listGeneralPath).Count(l => !string.IsNullOrWhiteSpace(l));
                AddLog($"✓ list-general.txt exists with {domainCount} domains");

                // Test 3: Ping test for selected services (skip telegram domains)
                AddLog("Test 3: Service connectivity test");

                int successfulPings = 0; int totalTests = 0;

                foreach (var serviceName in selectedServices)
                {
                    if (serviceMap.ContainsKey(serviceName))
                    {
                        var service = serviceMap[serviceName];
                        string displayName = currentLanguage == "ru" ? service.NameRu : service.Name;

                        // Skip pinging for WhatsApp & Telegram service
                        if (service.Name == "Whatsapp & Telegram")
                        {
                            AddLog($"  Skipping ping test for {displayName} (contains Telegram)"); continue;
                        }

                        // Test a few domains from each service
                        int domainsToTest = Math.Min(3, service.Domains.Length);
                        for (int i = 0; i < domainsToTest; i++)
                        {
                            totalTests++; string domain = service.Domains[i];

                            AddLog($"  Testing {displayName}: {domain}...");

                            bool pingSuccess = await PingHostAsync(domain);
                            if (pingSuccess) { successfulPings++; AddLog($"  ✓ {domain} is reachable"); }
                            else AddLog($"  ✗ {domain} is not reachable");

                            // Small delay between pings
                            await Task.Delay(500);
                        }
                    }
                }

                // Test 4: Check bin directory
                AddLog("Test 4: Checking bin directory");
                if (!Directory.Exists(binDirectory)) throw new Exception("bin directory not found");
                AddLog("✓ bin directory exists");

                // Test 5: Check winws.exe
                string winwsPath = Path.Combine(binDirectory, "winws.exe");
                if (!File.Exists(winwsPath)) AddLog("⚠ winws.exe not found (required for running)");
                else AddLog("✓ winws.exe found");

                // Summary
                AddLog("=== Test Summary ===");
                AddLog($"Total tests: {totalTests}"); AddLog($"Successful pings: {successfulPings}/{totalTests}");

                if (successfulPings > 0)
                {
                    statusLabel.ForeColor = Color.LightGreen;
                    if (currentLanguage == "ru") statusLabel.Text = $"Тесты пройдены: {successfulPings}/{totalTests} ping успешны";
                    else statusLabel.Text = $"Tests passed: {successfulPings}/{totalTests} pings successful";

                    AddLog("✓ All tests completed successfully");
                }
                else if (totalTests > 0)
                {
                    statusLabel.ForeColor = Color.Orange;
                    if (currentLanguage == "ru") statusLabel.Text = "Тесты завершены, но ping не удался (возможно, блокировка)";
                    else statusLabel.Text = "Tests completed but ping failed (possible blocking)";

                    AddLog("⚠ Tests completed with ping issues");
                }
                else
                {
                    statusLabel.ForeColor = Color.Yellow;
                    if (currentLanguage == "ru") statusLabel.Text = "Тесты завершены (ping тесты пропущены)";
                    else statusLabel.Text = "Tests completed (ping tests skipped)";

                    AddLog("✓ Tests completed (ping tests were skipped)");
                }
            }
            catch (Exception ex)
            {
                statusLabel.ForeColor = Color.Red;
                if (currentLanguage == "ru") statusLabel.Text = $"Ошибка тестирования: {ex.Message}";
                else statusLabel.Text = $"Test error: {ex.Message}";

                AddLog($"✗ Test failed: {ex.Message}");
            }
            finally
            {
                // Re-enable test button
                testButton.Enabled = true; testButton.Text = currentLanguage == "ru" ? "🔧 ТЕСТ" : "🔧 TEST";
            }
        }

        private async Task<bool> PingHostAsync(string hostName)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(hostName, 2000); // 2 second timeout
                    return reply.Status == IPStatus.Success;
                }
            }
            catch { return false; }
        }

        private void RunZapret()
        {
            try
            {
                // Check if winws.exe exists in bin directory
                string winwsPath = Path.Combine(binDirectory, "winws.exe");
                if (!File.Exists(winwsPath))
                {
                    string message = currentLanguage == "ru" ?
                        $"winws.exe не найден в директории bin!\n\nПожалуйста, поместите winws.exe в:\n{binDirectory}" :
                        $"winws.exe not found in bin directory!\n\nPlease place winws.exe in:\n{binDirectory}";

                    MessageBox.Show(message,
                        currentLanguage == "ru" ? "Ошибка" : "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Generate batch script WITHOUT BOM
                StringBuilder batchScript = new StringBuilder();
                batchScript.AppendLine("@echo off"); batchScript.AppendLine("chcp 65001 > nul");
                batchScript.AppendLine(":: Generated by Zapret-bywarm"); batchScript.AppendLine();
                batchScript.AppendLine("cd /d \"%~dp0\""); batchScript.AppendLine("set \"BIN=%~dp0bin\\\"");
                batchScript.AppendLine("set \"LISTS=%~dp0lists\\\""); batchScript.AppendLine("cd /d %BIN%"); batchScript.AppendLine();

                string command = GenerateWinwsCommand();

                // Always start minimized
                batchScript.AppendLine($"start \"zapret\" /min \"%BIN%winws.exe\" ^");
                batchScript.Append(command.Replace("\n", " ^\n")); batchScript.AppendLine(); batchScript.AppendLine();

                // Escape ampersands in echo commands
                string servicesString = string.Join(", ", selectedServices).Replace("&", "^&");
                batchScript.AppendLine("echo Zapret is running...");
                batchScript.AppendLine($"echo Strategy: {strategies[strategiesComboBox.SelectedIndex].Name.Replace("&", "^&")}");
                batchScript.AppendLine($"echo Services: {servicesString}");
                batchScript.AppendLine("echo Game Filter: " + (enableGameFilterCheckBox.Checked ? "Enabled" : "Disabled"));
                batchScript.AppendLine("pause");

                // Save batch file to app directory WITHOUT BOM
                string batchFile = Path.Combine(appDirectory, "zapret_run.bat");
                File.WriteAllText(batchFile, batchScript.ToString(), new UTF8Encoding(false));

                AddLog($"Batch file created: {batchFile}"); AddLog($"Strategy: {strategies[strategiesComboBox.SelectedIndex].Name}");
                AddLog($"Services: {string.Join(", ", selectedServices)}");
                AddLog($"Game Filter: {(enableGameFilterCheckBox.Checked ? "Enabled" : "Disabled")}");

                // Run the batch file
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchFile,
                    WorkingDirectory = appDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                zapretProcess = Process.Start(psi); AddLog("Zapret process started");

                statusLabel.ForeColor = Color.LightGreen;
                if (currentLanguage == "ru") statusLabel.Text = $"Zapret запущен с стратегией {strategies[strategiesComboBox.SelectedIndex].Name}";
                else statusLabel.Text = $"Zapret started with {strategies[strategiesComboBox.SelectedIndex].Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(currentLanguage == "ru" ? $"Ошибка: {ex.Message}" : $"Error: {ex.Message}",
                    currentLanguage == "ru" ? "Ошибка выполнения" : "Execution Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.ForeColor = Color.Red;
                if (currentLanguage == "ru") statusLabel.Text = "Ошибка: " + ex.Message;
                else statusLabel.Text = "Error: " + ex.Message;

                AddLog($"Error running Zapret: {ex.Message}");
            }
        }

        private void CloseZapretProcess()
        {
            try
            {
                AddLog("Stopping Zapret...");

                // Kill winws.exe process
                Process[] processes = Process.GetProcessesByName("winws");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        process.Kill(); process.WaitForExit(); AddLog($"Stopped winws.exe (PID: {process.Id})");
                    }
                    statusLabel.ForeColor = Color.Yellow;
                    if (currentLanguage == "ru") statusLabel.Text = "Zapret остановлен";
                    else statusLabel.Text = "Zapret stopped";

                    AddLog("Zapret stopped successfully");
                }
                else
                {
                    statusLabel.ForeColor = Color.Gray;
                    if (currentLanguage == "ru") statusLabel.Text = "Zapret не запущен";
                    else statusLabel.Text = "Zapret is not running";

                    AddLog("Zapret is not running");
                }

                // Also kill any batch processes
                processes = Process.GetProcessesByName("cmd");
                foreach (Process process in processes)
                {
                    if (process.MainWindowTitle.Contains("zapret"))
                    {
                        process.Kill(); AddLog($"Stopped cmd process (PID: {process.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(currentLanguage == "ru" ? $"Ошибка остановки Zapret: {ex.Message}" : $"Error stopping Zapret: {ex.Message}",
                    currentLanguage == "ru" ? "Ошибка" : "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddLog($"Error stopping Zapret: {ex.Message}");
            }
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            if (selectedServices.Count == 0)
            {
                string message = currentLanguage == "ru" ?
                    "Пожалуйста, выберите хотя бы один сервис для запуска Zapret." :
                    "Please select at least one service to run Zapret.";

                MessageBox.Show(message,
                    currentLanguage == "ru" ? "Сервисы не выбраны" : "No Services Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string strategyName = currentLanguage == "ru" ?
                strategies[strategiesComboBox.SelectedIndex].NameRu :
                strategies[strategiesComboBox.SelectedIndex].Name;

            string gameFilterStatus = enableGameFilterCheckBox.Checked ?
                (currentLanguage == "ru" ? "Включен" : "Enabled") :
                (currentLanguage == "ru" ? "Выключен" : "Disabled");

            var result = MessageBox.Show(
                currentLanguage == "ru" ?
                $"Запустить Zapret со следующими настройками?\n\n" +
                $"Стратегия: {strategyName}\n" +
                $"Сервисы: {string.Join(", ", selectedServices)}\n" +
                $"Игровой фильтр: {gameFilterStatus}" :
                $"Run Zapret with the following settings?\n\n" +
                $"Strategy: {strategyName}\n" +
                $"Services: {string.Join(", ", selectedServices)}\n" +
                $"Game Filter: {gameFilterStatus}",
                currentLanguage == "ru" ? "Подтверждение запуска" : "Confirm Run",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes) RunZapret();
        }

        private void CloseZapretButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                currentLanguage == "ru" ?
                "Вы уверены, что хотите остановить Zapret?" :
                "Are you sure you want to stop Zapret?",
                currentLanguage == "ru" ? "Остановить Zapret" : "Stop Zapret",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes) CloseZapretProcess();
        }

        private void EditListsButton_Click(object sender, EventArgs e)
        {
            try
            {
                string listFilePath = Path.Combine(listsDirectory, "list-general.txt");
                if (File.Exists(listFilePath))
                {
                    Process.Start("notepad.exe", listFilePath);
                    if (currentLanguage == "ru") statusLabel.Text = "Редактирование list-general.txt...";
                    else statusLabel.Text = "Editing list-general.txt...";

                    AddLog("Opening list-general.txt for editing");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(currentLanguage == "ru" ? $"Ошибка открытия файла: {ex.Message}" : $"Error opening file: {ex.Message}",
                    currentLanguage == "ru" ? "Ошибка" : "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddLog($"Error opening list-general.txt: {ex.Message}");
            }
        }

        private void EditIpsetButton_Click(object sender, EventArgs e)
        {
            try
            {
                string ipsetFilePath = Path.Combine(listsDirectory, "ipset-all.txt");
                if (File.Exists(ipsetFilePath))
                {
                    Process.Start("notepad.exe", ipsetFilePath);
                    if (currentLanguage == "ru") statusLabel.Text = "Редактирование ipset-all.txt...";
                    else statusLabel.Text = "Editing ipset-all.txt...";

                    AddLog("Opening ipset-all.txt for editing");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(currentLanguage == "ru" ? $"Ошибка открытия файла: {ex.Message}" : $"Error opening file: {ex.Message}",
                    currentLanguage == "ru" ? "Ошибка" : "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddLog($"Error opening ipset-all.txt: {ex.Message}");
            }
        }

        private void Close_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                currentLanguage == "ru" ?
                "Вы уверены, что хотите выйти из Zapret-bywarm?" +
                "Это не остановит Zapret, если он запущен." :
                "Are you sure you want to exit Zapret Bywarm?\n\n" +
                "This will not stop Zapret if it's running.",
                currentLanguage == "ru" ? "Подтверждение выхода" : "Exit Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                AddLog("Application closing..."); Application.Exit();
            }
        }
    }
}