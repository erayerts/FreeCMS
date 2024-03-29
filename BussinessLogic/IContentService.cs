using System.Collections.Generic;
using System.Security.Claims;
using FreeCMS.Shared.Entities;

namespace FreeCMS.BussinessLogic
{
    public interface IContentService
    {
        string PostContent(string contentType, string contentBody, ClaimsPrincipal user);
        string RemoveContent(int contentId);
        string PutContent(int contentId, string contentBody);
        ContentUnitDTO_output GetContent(int contentId);
        List<ContentUnitDTO_output> GetContents(string contentType, int offset = 0, int pageSize = int.MaxValue, string orderField = "");
    }
}