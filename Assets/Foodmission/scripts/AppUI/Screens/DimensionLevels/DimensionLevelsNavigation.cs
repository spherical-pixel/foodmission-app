using Unity.AppUI.Navigation;

namespace eu.foodmission.platform
{
    public enum DimensionLevelsMode
    {
        /// <summary>After the onboarding survey: shows the proposal, then continues the onboarding.</summary>
        Proposal,
        /// <summary>From Edit Profile or a locked-content dialog: edits the levels, then goes back.</summary>
        Edit
    }

    public static class DimensionLevelsNavigation
    {
        public const string ModeArgument = "mode";
        public const string ProposalValue = "proposal";
        public const string EditValue = "edit";

        public static Argument[] EditArguments() => new[] { new Argument(ModeArgument, EditValue) };

        public static Argument[] ProposalArguments(bool fromHome) => new[]
        {
            new Argument(ModeArgument, ProposalValue),
            new Argument("fromHome", fromHome ? "true" : "false")
        };
    }
}
