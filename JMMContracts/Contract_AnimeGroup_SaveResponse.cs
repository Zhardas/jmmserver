﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JMMContracts
{
	public class Contract_AnimeGroup_SaveResponse
	{
		public string ErrorMessage { get; set; }
		public Contract_AnimeGroup AnimeGroup { get; set; }
	}
}
