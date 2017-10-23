using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BossTimer
{
    /// <summary>
    /// Interaction logic for MainWnd.xaml
    /// </summary>
    public partial class MainWnd : Window
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Beep(uint dwFreq, uint dwDuration);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bInvert);

        private BossModel[] m_models;
        private SpeechSynthesizer m_speech;
        private IntPtr m_hwnd;
        private string m_title;

        public MainWnd()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_models = new BossModel[]
            {
                new BossModel
                {
                    name = "克價卡",
                    url = "http://bd.youxidudu.com/tools/clock/clock_boss_time.php?cid=200",
                    timeText1 = TimeA1,
                    timeText2 = TimeA2,
                    remText = RemA,
                    span1 = TimeSpan.FromHours(8),
                    span2 = TimeSpan.FromHours(12),
                },
                new BossModel
                {
                    name = "庫屯",
                    url = "http://bd.youxidudu.com/tools/clock/clock_boss_time.php?cid=250",
                    timeText1 = TimeB1,
                    timeText2 = TimeB2,
                    remText = RemB,
                    span1 = TimeSpan.FromHours(9),
                    span2 = TimeSpan.FromHours(13),
                },
                new BossModel
                {
                    name = "卡嵐達",
                    url = "http://bd.youxidudu.com/tools/clock/clock_boss_time.php?cid=251",
                    timeText1 = TimeC1,
                    timeText2 = TimeC2,
                    remText = RemC,
                    span1 = TimeSpan.FromHours(14),
                    span2 = TimeSpan.FromHours(18),
                },
                new BossModel
                {
                    name = "羅斐勒",
                    url = "http://bd.youxidudu.com/tools/clock/clock_boss_time.php?cid=252",
                    timeText1 = TimeD1,
                    timeText2 = TimeD2,
                    remText = RemD,
                    span1 = TimeSpan.FromHours(8),
                    span2 = TimeSpan.FromHours(12),
                }
            };

            m_speech = new SpeechSynthesizer();
            m_hwnd = new WindowInteropHelper(this).Handle;
            m_title = Title;

            for (int i = 0; i < m_models.Length; i++)
            {
                BossModel model = m_models[i];

                model.running = true;
                model.thread = new Thread(new ParameterizedThreadStart(Worker));
                model.thread.Start(model);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            for (int i = 0; i < m_models.Length; i++)
            {
                BossModel model = m_models[i];

                model.running = false;
                if (model.thread != null) model.thread.Join();
                model.thread = null;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            Title = m_title;
        }

        private void Worker(object param)
        {
            BossModel model;
            DateTime now, last, roar;
            TimeSpan span1, span2, rem;
            bool action;

            model = (BossModel)param;
            roar = last = DateTime.Now;
            span1 = TimeSpan.FromSeconds(180);
            span2 = TimeSpan.FromSeconds(10);

            UpdateModel(model);
            Dispatcher.BeginInvoke(new Action(delegate
            {
                UpdateUI(model);
            }));

            while (model.running)
            {
                now = DateTime.Now;
                action = false;

                if (now < model.time2)
                {
                    rem = now - last;

                    if (rem > span1)
                    {
                        last = now;
                        action = true;
                    }

                    model.rem = span1 - rem;
                }
                else/* if (now < model.time3)*/
                {
                    rem = now - last;

                    if (rem > span2)
                    {
                        last = now;
                        action = true;
                    }

                    model.rem = span2 - rem;
                }

                if (action)
                {
                    UpdateModel(model);

                    if (model.time1 > roar)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            PlayVoice(model.name);
                        }));

                        roar = model.time1;
                    }

                    GC.Collect();
                }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    UpdateUI(model);
                }));

                Thread.Sleep(100);
            }
        }

        private void UpdateModel(BossModel model)
        {
            HttpWebRequest request;
            String text;
            BossJson json;
            DateTime now;
            double max;

            request = (HttpWebRequest)WebRequest.Create(model.url);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    text = reader.ReadToEnd();
                }
            }

            json = JsonConvert.DeserializeObject<BossJson>(text);

            // guess year
            now = DateTime.Now;
            max = double.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                DateTime t = DateTime.Parse(string.Format("{0}-{1} {2}", now.Year + i - 1, json.pre_day, json.pre_hm));
                double d = Math.Abs((t - now).TotalSeconds);

                if (d < max)
                {
                    max = d;
                    model.time1 = t;
                }
            }

            model.time2 = model.time1 + model.span1;
            model.time3 = model.time1 + model.span2;
        }

        private void UpdateUI(BossModel model)
        {
            model.timeText1.Text = model.time1.ToString(@"HH\:mm");
            model.timeText2.Text = model.time2.ToString(@"HH\:mm") + " - " +
                model.time3.ToString(@"HH\:mm");
            model.remText.Text = model.rem.ToString(@"mm\:ss");
        }

        private void PlayVoice(String text)
        {
            Beep(1000, 1000);
            m_speech.Speak(text);

            Title = m_title + ": " + text;
            FlashWindow(m_hwnd, true);
        }
    }

    class BossJson
    {
        public string boss_name { get; set; }
        public string pre_day { get; set; }
        public string pre_hm { get; set; }
    };

    class BossModel
    {
        public string name { get; set; }
        public string url { get; set; }
        public TextBlock timeText1 { get; set; }
        public TextBlock timeText2 { get; set; }
        public TextBlock remText { get; set; }
        public DateTime time1;
        public DateTime time2;
        public DateTime time3;
        public TimeSpan span1;
        public TimeSpan span2;
        public TimeSpan rem;
        public Thread thread;
        public volatile bool running;
    }
}
