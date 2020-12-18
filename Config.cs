using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Interfaces;


namespace PlayerReconnect
{
	public sealed class Config : IConfig
	{
		public bool IsEnabled { get; set; } = true;

		[Description("The time the player has to reconnect before being registered as leaving")]
		public float ReconnectTime { get; set; } = 30;
	}
}
