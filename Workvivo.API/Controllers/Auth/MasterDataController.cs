namespace Workvivo.API.Controllers.Auth;
public class MasterDataController : ApiControllersBase
{
    private readonly IMasterDataService _masterDataService;
    public MasterDataController(IMasterDataService masterDataService)
    {
        _masterDataService = masterDataService;
    }
    [HttpGet]
    [Route(RouteClass.MasterData.GetUserType)]
    public async Task<IActionResult> GetUserType() =>
        Ok(await _masterDataService.GetUserType());

    [HttpGet]
    [Route(RouteClass.MasterData.GetIcons)]
    public async Task<IActionResult> GetIcons() =>
       Ok(await _masterDataService.GetIcons());


    [HttpGet]
    [Route(RouteClass.MasterData.GetMasterDataByCode)]
    public async Task<IActionResult> GetMasterDataByCode([FromQuery] string code) =>
      Ok(await _masterDataService.GetMasterDataByCode(code));
}
