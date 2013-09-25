namespace IniUtil
{
    public enum DuplicatedKeyNameMode
    {
        /// <summary>
        /// Ignore and continue to process the current INI file.
        /// </summary>
        Ignore,
        /// <summary>
        /// Abort to process the current INI file, and throw InvalidDataException.
        /// </summary>
        Abort,
        /// <summary>
        /// Allow and concatenate values separated by semicolons (;).
        /// </summary>
        Allow
    }
}
