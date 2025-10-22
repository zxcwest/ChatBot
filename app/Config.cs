using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChatBot
{
    class Config
    {
        public string botUserName { get; set; }
        public string chanelTwitch { get; set; }

        public string botOAuth { get; set; }
        public string refreshTokenBot { get; set; }

        public string brodcasterOAuth { get; set; }
        public string refreshTokenBrodcaster { get; set; }

        public string secretKey { get; set; }
        public string clientID { get; set; }

        //Кастомне ссылки
        public string donatSite { get; set; }
        public string telegram {  get; set; }
        public string discord { get; set; }
        //Начало/закреп
        public string nowStream { get; set; }
        //
        //OPEN AI
        public string apiOpenAI { get; set; }
        //SQL
        public string sql {  get; set; }

    }
}
