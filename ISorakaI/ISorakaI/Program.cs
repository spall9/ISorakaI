namespace ISorakaI
{
    using System;
    using EloBuddy;
    using EloBuddy.SDK.Events;
    using color = System.Drawing;

    class Program
    {
        private static string ver = "6.18.0";

        static void Main()
        {
            Loading.OnLoadingComplete += LoadingComplete;
        }

        private static void LoadingComplete(EventArgs args)
        {
            if (Player.Instance.ChampionName == "Soraka")
            {
                new General().Load();
                Chat.Print("Soraka by BluePrince loaded || " + ver);
            }
        }
    }
}