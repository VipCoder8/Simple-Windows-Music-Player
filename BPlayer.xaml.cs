using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.Wave;
namespace WPFProject
{
    /// <summary>
    /// Interaction logic for BPlayer.xaml
    /// </summary>
    public partial class BPlayer : Window
    {
        List<Canvas> musicPanels;
        //White Mode Icons
        BitmapImage playIcon;
        BitmapImage settingsIcon;
        BitmapImage folderIcon;
        BitmapImage pauseIcon;
        BitmapImage editIcon;
        BitmapImage darkModeOffIcon;
        //Dark Mode Icons
        BitmapImage folderDarkModeIcon;
        BitmapImage settingsDarkModeIcon;
        BitmapImage pauseDarkModeIcon;
        BitmapImage playDarkModeIcon;
        BitmapImage darkModeOnIcon;
        AudioFileReader audioFile;
        WaveOutEvent woe;
        volatile bool isCanceledProgressThread = true;
        Thread musicProgressThread;
        int currentMusicPlaying;
        bool isChangingProgress = false;
        bool startedPlaying = false;
        int playlistsY = 560;
        int selectedPlaylist;
        public BPlayer()
        {
            InitializeComponent();
            Closed += (sender, e) =>
            {
                Environment.Exit(0);
            };
            Closing += (sender, e) =>
            {
                Environment.Exit(0);
            };
            //White Theme Icons
            playIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\LightModeIcons\\PlayIcon.png"));
            pauseIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\LightModeIcons\\PauseIcon.png"));
            editIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\LightModeIcons\\EditIcon.png"));
            darkModeOffIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\LightModeIcons\\DarkModeOff.png"));
            settingsIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\LightModeIcons\\SettingsIcon.png"));
            folderIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\LightModeIcons\\FolderIcon.png"));
            //Dark Mode Icons
            darkModeOnIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\DarkModeIcons\\DarkModeOn.png"));
            folderDarkModeIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\DarkModeIcons\\FolderDarkMode.png"));
            settingsDarkModeIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\DarkModeIcons\\SettingsIconDarkMode.png"));
            pauseDarkModeIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\DarkModeIcons\\PauseIconDarkMode.png"));
            playDarkModeIcon = new BitmapImage(new Uri("C:\\Users\\user\\source\\repos\\WPFProject\\WPFProject\\assets\\DarkModeIcons\\PlayIconDarkMode.png"));
            
            musicPanels = new List<Canvas>();
            playlists = new List<Canvas>();
            musicProgressThread = new Thread(() => UpdateProgress());
            musicProgressThread.Start();
            CheckMusicFolder();
            for (int i = 0; i < musicPanels.Count; i++)
            {
                int index = i;
                musicPanels[i].Children[0].MouseDown += (sender, e) =>
                {
                    int lastIndex = i;
                    if (((Image)musicPanels[index].Children[0]).Source == playIcon)
                    {
                        for (int j = 0; j < musicPanels.Count; j++)
                        {
                            if (((Image)musicPanels[j].Children[0]).Source == pauseIcon)
                            {
                                if (j != lastIndex)
                                {
                                    ((Image)musicPanels[j].Children[0]).Source = playIcon;
                                    this.GlobalStateButton.Source = playIcon;
                                    this.isCanceledProgressThread = true;
                                    woe.Stop();
                                    //StopMusic();
                                }
                            }
                        }
                        startedPlaying = true;
                        this.ControllingCanvas.Visibility = Visibility.Visible;
                        ((Image)musicPanels[index].Children[0]).Source = pauseIcon;
                        currentMusicPlaying = index;
                        PlayMusic(((TextBlock)musicPanels[index].Children[2]).Text);
                    }
                    else
                    {
                        ((Image)musicPanels[index].Children[0]).Source = playIcon;
                        this.GlobalStateButton.Source = playIcon;
                        this.isCanceledProgressThread = true;
                        woe.Stop();
                        //StopMusic();
                    }
                };
            }
            this.SizeChanged += (sender, e) =>
            {
                if (this.Width - 250 > 0)
                {
                    this.FilesViewer.Width = this.Width - 250;
                    this.MyFilesCanvas.Width = this.Width - 250;
                    this.SearchBox.Width = this.Width - 250;
                }
            };
            this.StateChanged += (sender, e) =>
            {
                if (WindowState == WindowState.Maximized)
                {
                    this.FilesViewer.Width = this.ActualWidth - 250;
                    this.MyFilesCanvas.Width = this.ActualWidth - 250;
                    this.SearchBox.Width = this.ActualWidth - 250;
                }
            };
            this.Files.Click += (sender, e) =>
            {
                this.MyFilesCanvas.Visibility = Visibility.Visible;
                this.FilesViewer.Visibility = Visibility.Visible;
                this.SearchBox.Visibility = Visibility.Visible;
                if (startedPlaying)
                {
                    this.ControllingCanvas.Visibility = Visibility.Visible;
                }
                this.SettingsCanvas.Visibility = Visibility.Collapsed;
            };
            this.Settings.Click += (sender, e) =>
            {
                this.MyFilesCanvas.Visibility = Visibility.Collapsed;
                this.SearchBox.Visibility = Visibility.Collapsed;
                this.FilesViewer.Visibility = Visibility.Collapsed;
                this.ControllingCanvas.Visibility = Visibility.Collapsed;
                this.SettingsCanvas.Visibility = Visibility.Visible;
            };
            this.GlobalStateButton.MouseDown += (sender, e) =>
            {
                if (GlobalStateButton.Source == pauseIcon)
                {
                    ((Image)musicPanels[currentMusicPlaying].Children[0]).Source = playIcon;
                    GlobalStateButton.Source = playIcon;
                    this.isCanceledProgressThread = true;
                    PauseMusic();
                }
                else if (GlobalStateButton.Source == playIcon)
                {
                    ((Image)musicPanels[currentMusicPlaying].Children[0]).Source = pauseIcon;
                    GlobalStateButton.Source = pauseIcon;
                    woe.Play();
                    this.isCanceledProgressThread = false;
                }
            };
            this.SearchBox.TextChanged += (sender, e) =>
            {
                int y = 450;
                for (int i = 0; i < musicPanels.Count; i++)
                {
                    if (SearchBox.Text.Trim().Length > 0)
                    {
                        musicPanels[i].Visibility = Visibility.Hidden;
                        if (((TextBlock)musicPanels[i].Children[1]).Text.Trim().ToLower().Contains(SearchBox.Text.Trim().ToLower()))
                        {
                            Canvas.SetBottom(musicPanels[i], y);
                            y -= 48;
                            musicPanels[i].Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        Canvas.SetBottom(musicPanels[i], y);
                        y -= 48;
                        musicPanels[i].Visibility = Visibility.Visible;
                    }
                }
            };
            this.MusicProgress.PreviewMouseDown += (sender, e) =>
            {
                isChangingProgress = true;
            };
            this.MusicProgress.PreviewMouseUp += (sender, e) =>
            {
                audioFile.Position = (long)this.MusicProgress.Value;
                isChangingProgress = false;
            };
            this.NextButton.MouseDown += (sender, e) =>
            {
                currentMusicPlaying++;
                //StopMusic();
                if (currentMusicPlaying > musicPanels.Count - 1) { currentMusicPlaying = 0; }
                ((Image)musicPanels[currentMusicPlaying].Children[0]).Source = pauseIcon;
                if (currentMusicPlaying - 1 > 0)
                { ((Image)musicPanels[currentMusicPlaying - 1].Children[0]).Source = playIcon;
                } else
                {
                    ((Image)musicPanels[0].Children[0]).Source = playIcon;
                    ((Image)musicPanels[musicPanels.Count - 1].Children[0]).Source = playIcon;
                }
                GlobalStateButton.Source = pauseIcon;
                if (woe != null) { woe.Dispose(); }
                if (audioFile != null)
                {
                    audioFile.Close();
                    audioFile.Dispose();
                }
                audioFile = new AudioFileReader(((TextBlock)musicPanels[currentMusicPlaying].Children[2]).Text);
                this.MusicProgress.Value = 0;
                this.MusicProgress.Maximum = audioFile.Length;
                this.isCanceledProgressThread = false;
                woe = new WaveOutEvent();
                woe.Init(audioFile);
                woe.Play();
            };
            this.PreviousButton.MouseDown += (sender, e) =>
            {
                currentMusicPlaying--;
                //StopMusic();
                if (currentMusicPlaying < 0) { currentMusicPlaying = musicPanels.Count - 1; }
                ((Image)musicPanels[currentMusicPlaying].Children[0]).Source = pauseIcon;
                if (currentMusicPlaying + 1 < musicPanels.Count) { ((Image)musicPanels[currentMusicPlaying + 1].Children[0]).Source = playIcon; }
                else
                {
                    ((Image)musicPanels[0].Children[0]).Source = playIcon;
                }
                GlobalStateButton.Source = pauseIcon;
                if (woe != null) { woe.Dispose(); }
                if (audioFile != null) {
                    audioFile.Close();
                    audioFile.Dispose();
                }
                audioFile = new AudioFileReader(((TextBlock)musicPanels[currentMusicPlaying].Children[2]).Text);
                this.MusicProgress.Value = 0;
                this.MusicProgress.Maximum = audioFile.Length;
                this.isCanceledProgressThread = false;
                woe = new WaveOutEvent();
                woe.Init(audioFile);
                woe.Play();
            };
            this.ThemeModeButton.MouseDown += (sender, e) =>
            {
                if(this.ThemeModeButton.Source == darkModeOffIcon)
                {
                    changeToDarkMode();
                    this.ThemeModeButton.Source = darkModeOnIcon;
                } else
                {
                    changeToWhiteMode();
                    this.ThemeModeButton.Source = darkModeOffIcon;
                }
            };
        }
        public void PlayMusic(string filePath)
        {
            if (woe != null){woe.Stop();woe.Dispose();}
            if(audioFile != null){audioFile.Close();audioFile.Dispose();}
            audioFile = new AudioFileReader(filePath);
            woe = new WaveOutEvent();
            woe.Init(audioFile);
            woe.Play();
            this.MusicProgress.Visibility = Visibility.Visible;
            this.GlobalStateButton.Visibility = Visibility.Visible;
            this.NextButton.Visibility = Visibility.Visible;
            this.PreviousButton.Visibility = Visibility.Visible;
            this.MusicProgress.Value = 0;
            this.MusicProgress.Maximum = audioFile.Length;
            this.GlobalStateButton.Source = pauseIcon;
            this.isCanceledProgressThread = false;
            woe.PlaybackStopped += (sender, e) =>
            {
                if (this.MusicProgress.Value >= this.MusicProgress.Maximum-5)
                {
                    for (int i = 0; i < musicPanels.Count; i++)
                    {
                        ((Image)musicPanels[i].Children[0]).Source = playIcon;
                    }
                }
                this.MusicProgress.Value = 0;
            };
        }
        public void UpdateProgress()
        {
            while (true)
            {
                while (!this.isCanceledProgressThread)
                {
                    if (woe.PlaybackState == PlaybackState.Playing)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (!isChangingProgress)
                            {
                                this.MusicProgress.Value = audioFile.Position;
                            }
                        });
                    }
                    Thread.Sleep(200);
                }
                Thread.Sleep(200);
            }
        }
        public void PauseMusic()
        {
            this.GlobalStateButton.Source = playIcon;
            woe.Pause();
        }
        public void CheckMusicFolder()
        {
            int y = 480;
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string[] filePaths = Directory.GetFiles(path);
            Debug.WriteLine(filePaths.Length);
            if(filePaths.Length<1)
            {
                this.NotFoundMusic.Visibility=Visibility.Visible;
                this.ChooseMusicFileButton.Visibility=Visibility.Visible;
                this.ChooseMusicFolderButton.Visibility=Visibility.Visible;
            } else
            {
                foreach (string filePath in filePaths)
                {
                    if(System.IO.Path.GetFileName(filePath).EndsWith(".mp3")|| System.IO.Path.GetFileName(filePath).EndsWith(".ogg")
                        || System.IO.Path.GetFileName(filePath).EndsWith(".wav"))
                    {
                        Canvas musicsCanvas = new Canvas();
                        TextBlock filePathBlock = new TextBlock();
                        filePathBlock.Text = filePath;
                        filePathBlock.Visibility = Visibility.Hidden;
                        Image stateButton = new Image();
                        stateButton.Width = 32;
                        stateButton.Height = 32;
                        stateButton.Cursor = System.Windows.Input.Cursors.Hand;
                        stateButton.Source = playIcon;
                        TextBlock fileName = new TextBlock();
                        fileName.Text = System.IO.Path.GetFileName(filePath);
                        Canvas.SetLeft(fileName, 49);
                        Canvas.SetTop(stateButton, -6);
                        musicsCanvas.Children.Add(stateButton);
                        musicsCanvas.Children.Add(fileName);
                        musicsCanvas.Children.Add(filePathBlock);
                        Canvas.SetBottom(musicsCanvas, y);
                        Canvas.SetLeft(musicsCanvas, 25);
                        musicsCanvas.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        musicsCanvas.VerticalAlignment = VerticalAlignment.Top;
                        y -= 48;
                        this.MyFilesCanvas.Children.Add(musicsCanvas);
                        musicPanels.Add(musicsCanvas);
                        this.MyFilesCanvas.Height += musicsCanvas.Height;
                        
                    }
                }
            }
        }
        public void changeToDarkMode()
        {
            //Canvases
            this.MainCanvas.Background = new SolidColorBrush(Color.FromRgb(24,24,24));

            //UIs(Buttons, TextEditors, etc.)
            this.SearchBox.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            this.SearchBox.BorderBrush = new SolidColorBrush(Colors.DarkGray);
            this.SearchBox.Foreground= new SolidColorBrush(Colors.White);
            this.Settings.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            ((TextBlock)((Canvas)this.Settings.Content).Children[1]).Foreground = new SolidColorBrush(Colors.White);
            ((Image)((Canvas)this.Settings.Content).Children[0]).Source= settingsDarkModeIcon;
            this.Files.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            ((TextBlock)((Canvas)this.Files.Content).Children[1]).Foreground = new SolidColorBrush(Colors.White);
            ((Image)((Canvas)this.Files.Content).Children[0]).Source = folderDarkModeIcon;
            this.AppThemeText.Foreground = new SolidColorBrush(Colors.White);
        }
        public void changeToWhiteMode()
        {
            //Canvases
            this.MainCanvas.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            //UIs(Buttons, TextEditors, etc.)
            this.SearchBox.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            this.SearchBox.BorderBrush = new SolidColorBrush(Colors.DarkGray);
            this.SearchBox.Foreground = new SolidColorBrush(Colors.Black);
            this.Settings.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            ((TextBlock)((Canvas)this.Settings.Content).Children[1]).Foreground = new SolidColorBrush(Colors.Black);
            ((Image)((Canvas)this.Settings.Content).Children[0]).Source = settingsIcon;
            this.Files.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            ((TextBlock)((Canvas)this.Files.Content).Children[1]).Foreground = new SolidColorBrush(Colors.Black);
            ((Image)((Canvas)this.Files.Content).Children[0]).Source = folderIcon;
            this.AppThemeText.Foreground = new SolidColorBrush(Colors.Black);
        }
    }
}
