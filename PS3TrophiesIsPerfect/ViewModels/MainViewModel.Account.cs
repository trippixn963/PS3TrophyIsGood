using System.Threading.Tasks;
using System.Windows.Input;
using PS3TrophiesIsPerfect.Dialogs;
using PS3TrophiesIsPerfect.Infra;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.ViewModels
{
    // The linked PSNProfiles account (username + avatar), shown in the toolbar chip and comparison header.
    public sealed partial class MainViewModel
    {
        private string _myPsnUser = "";
        public string MyPsnUser
        {
            get => _myPsnUser;
            set
            {
                Set(ref _myPsnUser, value);
                Raise(nameof(HasMyUser));
                Raise(nameof(NoMyUser));
                Raise(nameof(MyUserDisplay));
                Raise(nameof(AccountChipText));
                Raise(nameof(StatusAccount));
            }
        }
        public bool HasMyUser => !string.IsNullOrWhiteSpace(_myPsnUser);
        public bool NoMyUser => !HasMyUser;
        public string MyUserDisplay => HasMyUser ? _myPsnUser : "You";
        public string AccountChipText => HasMyUser ? _myPsnUser : "Link PSN account";

        private string _myAvatarUrl = "";
        public string MyAvatarUrl
        {
            get => _myAvatarUrl;
            set => Set(ref _myAvatarUrl, value);
        }

        // ---- Overall PSN standing (level + total trophies across all platforms), for the profile chip ----
        private int _psnLevel;
        public int PsnLevel
        {
            get => _psnLevel;
            set
            {
                Set(ref _psnLevel, value);
                Raise(nameof(PsnLevelText));
            }
        }
        public string PsnLevelText => "Lv " + _psnLevel;

        private int _psnPlat;
        public int PsnPlat
        {
            get => _psnPlat;
            set => Set(ref _psnPlat, value);
        }
        private int _psnGold;
        public int PsnGold
        {
            get => _psnGold;
            set => Set(ref _psnGold, value);
        }
        private int _psnSilver;
        public int PsnSilver
        {
            get => _psnSilver;
            set => Set(ref _psnSilver, value);
        }
        private int _psnBronze;
        public int PsnBronze
        {
            get => _psnBronze;
            set => Set(ref _psnBronze, value);
        }

        private bool _hasPsnSummary;
        public bool HasPsnSummary
        {
            get => _hasPsnSummary;
            set => Set(ref _hasPsnSummary, value);
        }

        // Clicking the profile chip refreshes the stats; if no account is set yet, it sets one instead.
        private ICommand _profileClickCommand;
        public ICommand ProfileClickCommand =>
            _profileClickCommand
            ?? (
                _profileClickCommand = new RelayCommand(
                    async () =>
                    {
                        if (!HasMyUser)
                            await SetMyUserAsync();
                        else
                            await LoadPsnSummaryAsync();
                    },
                    () => !IsBusy
                )
            );

        /// <summary>Loads the account's overall trophy level + totals (passive — no prompt if not linked).</summary>
        public async Task LoadPsnSummaryAsync()
        {
            if (!Psn.HasCredentials)
                return;
            try
            {
                var s = await Task.Run(() => Psn.GetTrophySummary());
                PsnLevel = s.Level;
                PsnPlat = s.Platinum;
                PsnGold = s.Gold;
                PsnSilver = s.Silver;
                PsnBronze = s.Bronze;
                HasPsnSummary = s.Level > 0 || s.Total > 0;
            }
            catch
            { /* passive display — leave the stats hidden if it fails */
            }
        }

        private async Task SetMyUserAsync()
        {
            string user = await Modern.PromptText(
                "Your PSNProfiles account",
                "PSNProfiles username",
                "Your avatar and name will show on the comparison.",
                "e.g. LATKYA",
                "Save",
                MyPsnUser
            );
            if (string.IsNullOrWhiteSpace(user))
                return;
            user = user.Trim();

            string avatar = null;
            IsBusy = true;
            BusyText = "Fetching your PSNProfiles avatar…";
            try
            {
                avatar = await Task.Run(() =>
                    PsnProfilesScraper.FetchAvatar("https://psnprofiles.com/" + user)
                );
            }
            catch
            { /* leave avatar null */
            }
            IsBusy = false;

            MyPsnUser = user;
            MyAvatarUrl = avatar ?? "";
            Settings.MyPsnUser = MyPsnUser;
            Settings.MyAvatarUrl = MyAvatarUrl;
            Settings.Save();
        }
    }
}
