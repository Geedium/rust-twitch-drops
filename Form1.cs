using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using System.Collections.ObjectModel;
using System.Linq;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public List<Streamer> streamers = new List<Streamer>();
        private IWebDriver webDriver;
        private IWebDriver streamDriver;
        public int streamerIndex = -1;

        private enum Task
        {
            None,
            Playing
        }

        private Task task;

        public Form1()
        {
            InitializeComponent();

            FirefoxProfileManager manager = new FirefoxProfileManager();
            FirefoxProfile profile = manager.GetProfile("Selenium");

            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            FirefoxDriverService service2 = FirefoxDriverService.CreateDefaultService();
            service2.HideCommandPromptWindow = true;

            FirefoxOptions options = new FirefoxOptions()
            {
                Profile = profile
            };

            options.AddArgument("--log-level=0");
            options.SetPreference("media.volume_scale", "0.0");
            // options.AddArgument("--headless");

            webDriver = new FirefoxDriver(service, options);
            streamDriver = new FirefoxDriver(service2, options);
        }

        public void RefreshList()
        {
            listBox1.Items.Clear();
            streamers.ForEach(e => listBox1.Items.Add(e.identifier));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AddToQueue(textBox1.Text, sender, e);
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Stop();
                timer2.Stop();
                startToolStripMenuItem.Text = "Start";
            }
            else
            {
                timer1.Start();
                timer2.Start();
                timer1_Tick(sender, e);
                startToolStripMenuItem.Text = "Stop";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            webDriver.Url = "https://twitch.facepunch.com/";
            webDriver.Navigate();

            ReadOnlyCollection<IWebElement> webElements = webDriver.FindElements(By.XPath("//a[@class='drop'] | //a[@class='drop is-live']"));
            listBox2.Items.Clear();

            foreach (IWebElement webElement in webElements)
            {
                string href = webElement.GetAttribute("href");

                if (href.Substring(0, 21) == "https://www.twitch.tv")
                {
                    string identifier = webElement.GetAttribute("href").Substring(22);

                    for (int i = 0; i < streamers.Count; i++)
                    {
                        if (streamers[i].identifier == identifier)
                        {
                            streamers[i].twitch = href;

                            IWebElement statusElement = webElement.FindElement(By.CssSelector("div.online-status"));

                            streamers[i].Status = statusElement.GetAttribute("innerHTML").Trim();

                            if (task == Task.Playing && streamers[i].identifier == streamers[streamerIndex].identifier && !streamers[i].IsStreaming)
                            {
                                streamers[streamerIndex].elapsed.Stop();
                                streamers.RemoveAt(streamerIndex);
                                streamerIndex = -1;
                                task = Task.None;

                                RefreshList();
                                timer1_Tick(sender, e);
                                return;
                            }

                            if (task == Task.None && streamers[i].IsStreaming)
                            {
                                streamDriver.Url = streamers[i].twitch;
                                streamers[i].elapsed = new System.Diagnostics.Stopwatch();
                                streamers[i].elapsed.Start();
                                streamDriver.Navigate();
                                task = Task.Playing;
                                streamerIndex = i;
                            }
                        }
                    }

                    listBox2.Items.Add(identifier);
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            streamDriver.Quit();
            streamDriver.Dispose();

            webDriver.Quit();
            webDriver.Dispose();
        }

        private bool IsElementPresent(By by)
        {
            try
            {
                streamDriver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                if (streamers[streamerIndex].IsStreaming && task == Task.Playing)
                {
                    TimeSpan ts = streamers[streamerIndex].elapsed.Elapsed;

                    int hours = Convert.ToInt32(numericUpDown1.Value) * 3600;
                    int minutes = Convert.ToInt32(numericUpDown2.Value) * 60;
                    double seconds = hours + minutes;

                    if (ts.TotalSeconds >= seconds)
                    {
                        streamers[streamerIndex].elapsed.Stop();
                        streamers.RemoveAt(streamerIndex);
                        streamerIndex = -1;
                        task = Task.None;

                        RefreshList();
                        timer1_Tick(sender, e);
                    }

                    // data-a-target="player-overlay-mature-accept"
                    if (IsElementPresent(By.XPath("//button[@data-a-target='player-overlay-mature-accept']")))
                    {
                        streamDriver.FindElement(By.XPath("//button[@data-a-target='player-overlay-mature-accept']")).Click();
                    }

                    string elapsedTime = String.Format("{0}:{1}:{2}",
                                ts.Hours, ts.Minutes, ts.Seconds);

                    toolStripStatusLabel1.Text = elapsedTime + " (" + streamerIndex + ")" + ", " + (task == Task.Playing ? "Playing" : "Idling");
                }
            }
            catch (Exception)
            {
                toolStripStatusLabel1.Text = "00:00:00";
            }
        }

        public void AddToQueue(string text, object sender, EventArgs e)
        {
            bool contains = listBox1.Items.Cast<string>().Any(x => x == text);
            bool exists = listBox2.Items.Cast<string>().Any(x => x == text);

            if (!contains && exists)
            {
                streamers.Add(new Streamer
                {
                    identifier = text
                });

                RefreshList();
                timer1_Tick(sender, e);
            }
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            string text = listBox2.SelectedItem.ToString();
            AddToQueue(text, sender, e);
        }
    }
}
