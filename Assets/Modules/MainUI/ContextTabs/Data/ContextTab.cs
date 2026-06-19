namespace Modules.MainUI.ContextTabs.Data
{
    /// <summary>
    ///     The selectable tabs of the bottom panel's context sub-panel (Огляд / Будівлі / Дії).
    ///     <see cref="Unknown" /> is the sentinel so default(ContextTab) is never a valid tab — systems iterate
    ///     only the real tabs and fail-loud on Unknown.
    /// </summary>
    public enum ContextTab
    {
        Unknown = 0,
        Overview = 1,
        Buildings = 2,
        Actions = 3,
    }
}
