namespace DevSimAPI.Models.DTOs
{
    /// <summary>
    /// DTO Model used for AI_Service to generate issues and inject back into GH Issues
    /// </summary>
    public class RepoInfo
    {
        public string RepoName { get; set; }
        public string Description { get; set; }
    }
}
