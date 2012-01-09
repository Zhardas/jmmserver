﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using JMMServer.Entities;
using JMMServer.Repositories;
using JMMServer.Providers.TvDB;
using JMMServer.WebCache;
using JMMServer.Providers.MyAnimeList;

namespace JMMServer.Commands.MAL
{
	[Serializable]
	public class CommandRequest_MALSearchAnime : CommandRequestImplementation, ICommandRequest
	{
		public int AnimeID { get; set; }
		public bool ForceRefresh { get; set; }

		public CommandRequestPriority DefaultPriority 
		{
			get { return CommandRequestPriority.Priority8; }
		}

		public string PrettyDescription
		{
			get
			{
				return string.Format("Searching for anime on MAL: {0}", AnimeID);
			}
		}

		public CommandRequest_MALSearchAnime()
		{
		}

		public CommandRequest_MALSearchAnime(int animeID, bool forced)
		{
			this.AnimeID = animeID;
			this.ForceRefresh = forced;
			this.CommandType = (int)CommandRequestType.MAL_SearchAnime;
			this.Priority = (int)DefaultPriority;

			GenerateCommandID();
		}

		public override void ProcessCommand()
		{
			logger.Info("Processing CommandRequest_MALSearchAnime: {0}", AnimeID);

			try
			{
				// first check if the user wants to use the web cache
				if (ServerSettings.WebCache_MAL_Get)
				{
					try
					{
						CrossRef_AniDB_MALResult crossRef = XMLService.Get_CrossRef_AniDB_MAL(AnimeID);
						if (crossRef != null)
						{
							logger.Trace("Found MAL match on web cache for {0} - id = {1}", AnimeID, crossRef.MALID);
							MALHelper.LinkAniDBMAL(AnimeID, crossRef.MALID, crossRef.MALTitle, true);
							return;
						}
					}
					catch (Exception)
					{
						
					}
				}

				string searchCriteria = "";
				AniDB_AnimeRepository repAnime = new AniDB_AnimeRepository();
				AniDB_Anime anime = repAnime.GetByAnimeID(AnimeID);
				if (anime == null) return;

				searchCriteria = anime.MainTitle;

				// if not wanting to use web cache, or no match found on the web cache go to TvDB directly
				anime malResults = MALHelper.SearchAnimesByTitle(searchCriteria);

				if (malResults.entry.Length == 1)
				{
					logger.Trace("Using MAL search result for search on {0} : {1} ({2})", searchCriteria, malResults.entry[0].id, malResults.entry[0].title);
					MALHelper.LinkAniDBMAL(AnimeID, malResults.entry[0].id, malResults.entry[0].title, false);
				}
				else if (malResults.entry.Length == 0)
					logger.Trace("ZERO MAL search result results for: {0}", searchCriteria);
				else
				{
					// if the title's match exactly and they have the same amount of episodes, we will use it
					foreach (animeEntry res in malResults.entry)
					{
						if (res.title.Equals(anime.MainTitle, StringComparison.InvariantCultureIgnoreCase) && res.episodes == anime.EpisodeCountNormal)
						{
							logger.Trace("Using MAL search result for search on {0} : {1} ({2})", searchCriteria, res.id, res.title);
							MALHelper.LinkAniDBMAL(AnimeID, res.id, res.title, false);
						}
					}
					logger.Trace("Too many MAL search result results for, skipping: {0}", searchCriteria);
				}
			}
			catch (Exception ex)
			{
				logger.Error("Error processing CommandRequest_MALSearchAnime: {0} - {1}", AnimeID, ex.ToString());
				return;
			}
		}

		public override void GenerateCommandID()
		{
			this.CommandID = string.Format("CommandRequest_MALSearchAnime{0}", this.AnimeID);
		}

		public override bool LoadFromDBCommand(CommandRequest cq)
		{
			this.CommandID = cq.CommandID;
			this.CommandRequestID = cq.CommandRequestID;
			this.CommandType = cq.CommandType;
			this.Priority = cq.Priority;
			this.CommandDetails = cq.CommandDetails;
			this.DateTimeUpdated = cq.DateTimeUpdated;

			// read xml to get parameters
			if (this.CommandDetails.Trim().Length > 0)
			{
				XmlDocument docCreator = new XmlDocument();
				docCreator.LoadXml(this.CommandDetails);

				// populate the fields
				this.AnimeID = int.Parse(TryGetProperty(docCreator, "CommandRequest_MALSearchAnime", "AnimeID"));
				this.ForceRefresh = bool.Parse(TryGetProperty(docCreator, "CommandRequest_MALSearchAnime", "ForceRefresh"));
			}

			return true;
		}

		public override CommandRequest ToDatabaseObject()
		{
			GenerateCommandID();

			CommandRequest cq = new CommandRequest();
			cq.CommandID = this.CommandID;
			cq.CommandType = this.CommandType;
			cq.Priority = this.Priority;
			cq.CommandDetails = this.ToXML();
			cq.DateTimeUpdated = DateTime.Now;

			return cq;
		}
	}
}
