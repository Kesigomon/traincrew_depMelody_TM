using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TraincrewDepMelody.Models
{
    /// <summary>
    /// アプリケーション状態
    /// </summary>
    public class ApplicationState : INotifyPropertyChanged
    {
        private ModeType _currentMode;
        private GameStatus _gameStatus;
        private List<string> _occupiedTracks = new List<string>();
        private string _trainNumber = string.Empty;
        private Direction _direction;
        private StationInfo? _currentStation;
        private bool _isAtStation;
        private string _currentAudioFile = string.Empty;
        private bool _isAudioPlaying;

        public ModeType CurrentMode
        {
            get => _currentMode;
            set { _currentMode = value; OnPropertyChanged(); }
        }

        public GameStatus GameStatus
        {
            get => _gameStatus;
            set { _gameStatus = value; OnPropertyChanged(); }
        }

        public List<string> OccupiedTracks
        {
            get => _occupiedTracks;
            set { _occupiedTracks = value; OnPropertyChanged(); }
        }

        public string TrainNumber
        {
            get => _trainNumber;
            set { _trainNumber = value; OnPropertyChanged(); }
        }

        public Direction Direction
        {
            get => _direction;
            set { _direction = value; OnPropertyChanged(); }
        }

        public StationInfo? CurrentStation
        {
            get => _currentStation;
            set { _currentStation = value; OnPropertyChanged(); }
        }

        public bool IsAtStation
        {
            get => _isAtStation;
            set { _isAtStation = value; OnPropertyChanged(); }
        }

        public string CurrentAudioFile
        {
            get => _currentAudioFile;
            set { _currentAudioFile = value; OnPropertyChanged(); }
        }

        public bool IsAudioPlaying
        {
            get => _isAudioPlaying;
            set { _isAudioPlaying = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
