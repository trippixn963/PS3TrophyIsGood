using System.Threading.Tasks;
using PS3TrophiesIsPerfect.Dialogs;
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
            set { Set(ref _myPsnUser, value); Raise(nameof(HasMyUser)); Raise(nameof(NoMyUser)); Raise(nameof(MyUserDisplay)); Raise(nameof(AccountChipText)); }
        }
        public bool HasMyUser => !string.IsNullOrWhiteSpace(_myPsnUser);
        public bool NoMyUser => !HasMyUser;
        public string MyUserDisplay => HasMyUser ? _myPsnUser : "You";
        public string AccountChipText => HasMyUser ? _myPsnUser : "Link PSN account";

        private string _myAvatarUrl = "";
        public string MyAvatarUrl { get => _myAvatarUrl; set => Set(ref _myAvatarUrl, value); }

        private async Task SetMyUserAsync()
        {
            string user = await Modern.PromptText("Your PSNProfiles account", "PSNProfiles username",
                "Your avatar and name will show on the comparison.", "e.g. LATKYA", "Save", MyPsnUser);
            if (string.IsNullOrWhiteSpace(user)) return;
            user = user.Trim();

            string avatar = null;
            IsBusy = true;
            BusyText = "Fetching your PSNProfiles avatar…";
            try { avatar = await Task.Run(() => PsnProfilesScraper.FetchAvatar("https://psnprofiles.com/" + user)); }
            catch { /* leave avatar null */ }
            IsBusy = false;

            MyPsnUser = user;
            MyAvatarUrl = avatar ?? "";
            Settings.MyPsnUser = MyPsnUser;
            Settings.MyAvatarUrl = MyAvatarUrl;
            Settings.Save();
        }
    }
}
