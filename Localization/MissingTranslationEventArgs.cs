﻿using System;

namespace Localization;

public class MissingTranslationEventArgs : EventArgs
{
    public MissingTranslationEventArgs(string languageTag, string label, string defaultValue)
    {
        LanguageTag = languageTag;
        Label = label;
        DefaultValue = defaultValue;
    }

    public string LanguageTag { get; set; }

    public string Label { get; set; }

    public string DefaultValue { get; set; }
}