// Project:         TravelOptions mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2021 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

namespace DaggerfallWorkshop.Game.Guilds
{
    public class MagesGuildTO : MagesGuild
    {
        public override bool CanAccessService(GuildServices service)
        {
            if (service == GuildServices.Teleport)
                return true;
            else
                return base.CanAccessService(service);
        }
    }
}
