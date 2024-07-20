namespace Hi3Helper
{
    public sealed partial class Locale
    {
        #region FileCleanupPage
        public sealed partial class LocalizationParams
        {
            public LangFileCleanupPage _FileCleanupPage { get; set; } = LangFallback?._FileCleanupPage;
            public sealed class LangFileCleanupPage
            {
                public string Title { get; set; } = LangFallback?._FileCleanupPage.Title;
                public string TopButtonRescan { get; set; } = LangFallback?._FileCleanupPage.TopButtonRescan;
                public string NoFilesToBeDeletedText { get; set; } = LangFallback?._FileCleanupPage.NoFilesToBeDeletedText;
                public string ListViewFieldFileName { get; set; } = LangFallback?._FileCleanupPage.ListViewFieldFileName;
                public string ListViewFieldFileSize { get; set; } = LangFallback?._FileCleanupPage.ListViewFieldFileSize;
                public string BottomButtonDeleteAllFiles { get; set; } = LangFallback?._FileCleanupPage.BottomButtonDeleteAllFiles;
                public string BottomButtonDeleteSelectedFiles { get; set; } = LangFallback?._FileCleanupPage.BottomButtonDeleteSelectedFiles;
                public string BottomCheckboxFilesSelected { get; set; } = LangFallback?._FileCleanupPage.BottomCheckboxFilesSelected;
                public string BottomCheckboxNoFileSelected { get; set; } = LangFallback?._FileCleanupPage.BottomCheckboxNoFileSelected;
                public string DialogDeletingFileTitle { get; set; } = LangFallback?._FileCleanupPage.DialogDeletingFileTitle;
                public string DialogDeletingFileSubtitle1 { get; set; } = LangFallback?._FileCleanupPage.DialogDeletingFileSubtitle1;
                public string DialogDeletingFileSubtitle2 { get; set; } = LangFallback?._FileCleanupPage.DialogDeletingFileSubtitle2;
                public string DialogDeletingFileSubtitle3 { get; set; } = LangFallback?._FileCleanupPage.DialogDeletingFileSubtitle3;
                public string DialogDeletingFileSubtitle4 { get; set; } = LangFallback?._FileCleanupPage.DialogDeletingFileSubtitle4;
                public string DialogDeletingFileSubtitle5 { get; set; } = LangFallback?._FileCleanupPage.DialogDeletingFileSubtitle5;
                public string DialogDeleteSuccessTitle { get; set; } = LangFallback?._FileCleanupPage.DialogDeleteSuccessTitle;
                public string DialogDeleteSuccessSubtitle1 { get; set; } = LangFallback?._FileCleanupPage.DialogDeleteSuccessSubtitle1;
                public string DialogDeleteSuccessSubtitle2 { get; set; } = LangFallback?._FileCleanupPage.DialogDeleteSuccessSubtitle2;
            }
        }
        #endregion
    }
}
