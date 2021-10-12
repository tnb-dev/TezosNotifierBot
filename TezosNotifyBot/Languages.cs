namespace TezosNotifyBot
{
    public class Languages
    {
        public static readonly Language ru = new Language("🇷🇺", "ru", "Русский");
        public static readonly Language en = new Language("🇺🇸", "en", "English");

        public static Language Get(string code)
        {
            return code switch
            {
                "ru" => ru,
                _ => en
            };
        }
        
        public static Language Next(string code)
        {
            return code switch
            {
                "en" => ru,
                "ru" => en,
                _ => en
            };
        }
    }

    public class Language
    {
        public string Icon { get; }
        public string Code { get; }
        public string Name { get; }

        public Language(string icon, string code, string name)
        {
            Icon = icon;
            Code = code;
            Name = name;
        }
    }
}