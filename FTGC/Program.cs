using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTGC
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

		public static bool TryParseJson<T>(this string obj, out T result)
		{
			try
			{
				// Validate missing fields of object
				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.MissingMemberHandling = MissingMemberHandling.Error;

				result = JsonConvert.DeserializeObject<T>(obj, settings);
				return true;
			}
			catch (Exception)
			{
				result = default(T);
				return false;
			}
		}

        
    }
}
