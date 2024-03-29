using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using Dapper;
using FreeCMS.Presentation.Formatters;
using FreeCMS.Shared.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace FreeCMS.DataAccess
{
    public class ContentRepository : IContentRepository
    {
        private readonly IConfiguration _config;
        private readonly string connectionstring;

        public ContentRepository(IConfiguration config)
        {
            _config = config;
            connectionstring = @$"Server={_config["Database:DatabaseHost"]};
                                  Database={_config["Database:DatabaseName"]};
                                  User Id={_config["Database:DatabaseUser"]};
                                  Password={_config["Database:DatabasePassword"]};";
        }

        public string PostContent(string contentType, string contentBody, ClaimsPrincipal user)
        {
            SqlConnection dbconnection = new(connectionstring);

            try
            {
                 var jsonString = JsonConvert.DeserializeObject(contentBody);
            }
            catch
            {
                throw new Exception("Request body is not in a valid json format.");      
            }

            if(contentType == null || contentBody == "[]" || contentBody == "{}") 
            {
                throw new Exception("Content can not create with empty json format.");
            }

            dbconnection.Execute(@$"INSERT INTO contents (ContentType, ContentBody, Date)
                                    VALUES('{contentType}', '{contentBody}', {DateTimeOffset.Now.ToUnixTimeSeconds()})");

            return @$"Content '{contentType}' created successfully";
        }

        public ContentUnitDTO_output GetContent(int contentId) 
        {
            SqlConnection dbconnection = new(connectionstring);

            List<ContentUnit> queryContent = dbconnection.Query<ContentUnit>($"SELECT * FROM contents WHERE ContentId = {contentId}").ToList();
            ContentUnitDTO_output contentDTO = new();

            contentDTO.ContentId = queryContent.First().ContentId;
            contentDTO.ContentType = queryContent.First().ContentType;
            contentDTO.ContentBody = JsonConvert.DeserializeObject<Dictionary<string, object>>(queryContent.First().ContentBody);
            contentDTO.ContentOwner = queryContent.First().ContentOwner;
            contentDTO.Date = UnixTimeStampToDateTimeConverter.UnixTimeStampToDateTime(queryContent.First().Date);

            return contentDTO;
        }

        public List<ContentUnitDTO_output> GetContents(string contentType, int offset, int pageSize, string orderField, OrderDirection orderDirection)
        {
            if (offset < 0)
            {
                throw new Exception("ERROR: Offset value can not take negative numbers.");
            }

            SqlConnection dbconnection = new(connectionstring);

            if(orderField == null) 
            {
                orderDirection = OrderDirection.None;
            }

            List<ContentUnit> queryContent = new();
            queryContent = dbconnection.Query<ContentUnit>(@$"SELECT * FROM contents WHERE ContentType = '{contentType}' ORDER BY ContentId").ToList();
            if(queryContent.Count == 0) 
            {
                throw new Exception("ERROR: Content not found.");
            }

            List<ContentUnitDTO_output> contentDTO = new();
            Dictionary<int, object> contentFieldFetchers = new();
            Dictionary<int, object> orderedFieldDict = new();

            for (int i = 0; i < queryContent.Count; i++)
            {
                contentDTO.Add(new ContentUnitDTO_output {
                    ContentId = queryContent[i].ContentId,
                    ContentType = queryContent[i].ContentType,
                    ContentBody = JsonConvert.DeserializeObject<Dictionary<string, object>>(queryContent[i].ContentBody),
                    Date = UnixTimeStampToDateTimeConverter.UnixTimeStampToDateTime(queryContent[i].Date)
                });

                if (orderField != null && contentDTO[i].ContentBody.ContainsKey(orderField))
                {
                    contentFieldFetchers.Add(contentDTO[i].ContentId, contentDTO[i].ContentBody[orderField]);
                }
            }

            //ascending
            if(orderDirection == OrderDirection.Ascending) 
            {
                foreach (KeyValuePair<int, object> field in contentFieldFetchers.OrderBy(key => key.Value))
                {
                    orderedFieldDict.Add(field.Key, field.Value);
                }
            }

            //descending
            if(orderDirection == OrderDirection.Descending) 
            {
                foreach (KeyValuePair<int, object> field in contentFieldFetchers.OrderByDescending(key => key.Value))
                {
                    orderedFieldDict.Add(field.Key, field.Value);
                }
            }

            //none
            if(orderDirection == OrderDirection.None)
            {
                //offset process
                contentDTO.RemoveRange(0, offset);

                //limit process
                if(contentDTO.Count >= pageSize) 
                {
                    contentDTO.RemoveRange(pageSize, contentDTO.Count-pageSize);
                }

                AddContentIdToTheField(contentDTO);

                return contentDTO;
            }

            List<ContentUnitDTO_output> orderedContentDTO = new();

            int index;
            for (int i = 0; i < orderedFieldDict.Count; i++)
            {
                index = contentDTO.FindIndex(c => c.ContentId == orderedFieldDict.ElementAt(i).Key);
                orderedContentDTO.Add(contentDTO[index]);
            }

            //offset process
            orderedContentDTO.RemoveRange(0, offset);

            //limit process
            if (orderedContentDTO.Count >= pageSize)
            {
                orderedContentDTO.RemoveRange(pageSize, orderedContentDTO.Count - pageSize);
            }

            AddContentIdToTheField(orderedContentDTO);

            return orderedContentDTO;
        }

        public string RemoveContent(int contentId)
        {
            SqlConnection dbconnection = new(connectionstring);

            List<ContentUnit> queryContent = dbconnection.Query<ContentUnit>($"SELECT * FROM contents WHERE \"ContentId\" = {contentId}").ToList();

            if (queryContent.Count == 0) 
            {
                throw new Exception("Content doesn't exist.");
            } 
            else 
            {
                dbconnection.Execute($"DELETE FROM contents WHERE \"ContentId\" = {contentId}");
                return "Content deleted successfuly.";
            }
        }

        public string PutContent(int contentId, string newContentBody)
        {
            SqlConnection dbconnection = new(connectionstring);

            try
            {
                 var jsonString = JsonConvert.DeserializeObject(newContentBody);
            }
            catch
            {
                throw new Exception("Request body is not in a valid json format.");      
            }

            List<ContentUnit> queryContent = dbconnection.Query<ContentUnit>($"SELECT * FROM contents WHERE \"ContentId\" = {contentId}").ToList();

            if (queryContent.Count == 0)
            {
                throw new Exception("Content doesn't exist.");
            }
            else
            {
                dbconnection.Execute($"UPDATE contents SET ContentBody = '{newContentBody}' WHERE ContentId = {contentId}");
                return @"Content updated successfuly.";
            }
        }

        private void AddContentIdToTheField(List<ContentUnitDTO_output> contentDTO) 
        {
            //add item id to field
            Dictionary<string, object> valueDict = new();
            Dictionary<string, object> finalDict = new();

            for (int i = 0; i < contentDTO.Count; i++)
            {
                valueDict.Add("content_id", contentDTO[i].ContentId);

                foreach (var item in contentDTO[i].ContentBody)
                {
                    valueDict.Add(item.Key, item.Value);
                }

                contentDTO[i].ContentBody.Clear();

                foreach (var item in valueDict)
                {
                    contentDTO[i].ContentBody.Add(item.Key, item.Value);
                }

                valueDict.Clear();
            }
        }
    }
}