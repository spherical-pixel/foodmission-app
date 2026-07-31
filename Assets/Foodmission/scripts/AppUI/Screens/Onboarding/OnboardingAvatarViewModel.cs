using Unity.AppUI.MVVM;

namespace eu.foodmission.platform
{
    public partial class OnboardingAvatarViewModel : ViewModelBase
    {
        private readonly IAvatarService _avatarService;

        public OnboardingAvatarViewModel(IStoreService storeService, IAvatarService avatarService = null) : base(storeService)
        {
            _avatarService = avatarService;
        }

        public async System.Threading.Tasks.Task SkipAvatarAsync()
        {
            var avatar = _avatarService ?? App.current?.services?.GetService<IAvatarService>();
            if (avatar != null)
            {
                await avatar.SetHasAvatarAsync(false);
            }
        }
    }
}
