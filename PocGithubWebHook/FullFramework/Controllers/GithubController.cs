using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FullFramework.Controllers
{
    [Route("api/[controller]")]
    public class GithubController : Controller
    {
        private const string LocalRepositoryPath = @"C:\Workspace\GitHub\TeBeCo\EmptyWebHookGenerator";
        private string fileNameToRandomlyChange = "SomeTextFile";
        private Identity identity = new Identity("TeBeCo", "TeBeCo@gmail.com");

        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        // POST api/values
        [Route("push")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]UserInfo userInfos)
        {
            using (var repository = new Repository(LocalRepositoryPath))
            {
                var masterBranch = repository.Branches["master"];
                var pullOptions = new PullOptions();
                var _signature = new Signature(identity, DateTimeOffset.Now);

                var mergeResult = Commands.Pull(repository, _signature, pullOptions);

                if (mergeResult.Status == MergeStatus.Conflicts)
                    return StatusCode(500, "Conflict");

                using (var fs = System.IO.File.Create(System.IO.Path.Combine(LocalRepositoryPath, fileNameToRandomlyChange)))
                {
                    fs.Position = 0;
                    var guidArray = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
                    await fs.WriteAsync(guidArray, 0, guidArray.Length).ConfigureAwait(false);
                    await fs.FlushAsync();
                    fs.Close();
                }

                try
                {
                    Commands.Stage(repository, @"C:\Workspace\GitHub\TeBeCo\EmptyWebHookGenerator\SomeTextFile.txt");
                    repository.Commit("Random commit generated", _signature, _signature);
                    repository.Network.Push(masterBranch, GetPushOption(userInfos.UserName, userInfos.Password));

                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
                return Json("Pushed");
            }
        }

        private PushOptions GetPushOption(string userName, string password)
        {
            var options = new LibGit2Sharp.PushOptions();
            options.CredentialsProvider = new CredentialsHandler(
                (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials()
                    {
                        Username = userName,
                        Password = password
                    });

            return options;
        }
    }

    public class UserInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
